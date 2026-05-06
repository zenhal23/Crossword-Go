# Crossword GO! — Unity Prototype Design Document

**Date:** 2026-05-01  
**Unity Version:** 6000.3.8f1 (Unity 6)  
**Pipeline:** URP 2D  
**Platform:** Android (prototype ignores platform-specific code)  
**Architecture:** ScriptableObject-based levels, custom EditorWindow tooling  

---

## 1. Overview

A turn-based competitive crossword battle game. One human player and one bot opponent share the same crossword board. Each turn the active player receives a hand of letters and places them into answer cells. Players score points for correct placements, word completions, and efficiency. The match ends when the board is complete or no moves remain. Highest score wins.

The prototype must demonstrate:
- Playable core mechanic (turn-based letter placement)
- Bot opponent with tunable difficulty
- Level editor, validator, difficulty analyzer, and generator tools
- Bot simulation for headless level testing

---

## 2. Data Model

All level data lives in a single `LevelData` ScriptableObject. Runtime mutable state is kept separately so `LevelData` is never modified during play.

### 2.1 LevelData (ScriptableObject)

```
LevelData
  string          title
  int             gridWidth
  int             gridHeight
  CellData[]      cells              // row-major: index = row * gridWidth + col
  WordSlotData[]  wordSlots
  Difficulty      difficulty         // Easy | Medium | Hard (set by DifficultyAnalyzer or manual override)
  int             turnTimerSeconds   // default 30; configurable per level
```

### 2.2 CellData (serializable struct)

```
CellData
  CellType  cellType      // Black | Clue | Answer
  int       clueSlotId    // index into wordSlots; -1 if not a Clue cell
```

`CellType` enum:
- `Black` — blocked/empty, no interaction
- `Clue` — displays the clue text/image and a directional arrow; never receives a letter
- `Answer` — white cell that receives a single letter from the player's hand

There are no numbered corner labels. The clue cell itself acts as the identifier — the arrow (► or ▼) points to the first answer cell.

### 2.3 WordSlotData (serializable struct)

```
WordSlotData
  int        id
  Direction  direction     // Across | Down
  int        clueRow       // grid position of the Clue cell
  int        clueCol
  int        startRow      // grid position of the first Answer cell
  int        startCol      // = clueCol+1 if Across, = clueRow+1 if Down
  int        length
  string     answer        // any format: proper word, abbreviation, fill-in-the-blank value, etc.
  string     clue          // text clue; empty string if image-only
  Sprite     clueSprite    // optional image clue; null if text-only
```

Answers can be any format: proper words, abbreviations, fill-in-the-blank completions (e.g. answer is "RITER" for clue "TYPEW__R"), multi-word phrases, etc. The answer string contains only the letters that go in the answer cells — no underscores or spaces.

### 2.4 BoardState (runtime class, not persisted)

Holds all mutable game state. Created from `LevelData` at match start.

```
BoardState
  char[]      playerFill      // pending placements for current player turn; '\0' = empty
  char[]      botFill         // pending placements for current bot turn; '\0' = empty
  bool[]      lockedCells     // true once a cell is correctly confirmed and permanently placed
  int[]       cellOwner       // who locked the cell: 0 = unowned, 1 = player, 2 = bot
```

On turn submission: correct pending letters become locked (`lockedCells[i] = true`, `cellOwner[i]` set). Both fill arrays are then cleared for the next turn. A locked cell cannot be targeted by either player again.

### 2.5 PlayerState (runtime class)

```
PlayerState
  int         score
  int         turnsPlayed
  List<char>  currentHand     // letters dealt this turn
  bool        isHuman
```

---

## 3. Core Gameplay

### 3.1 Turn Flow

```
1. TurnManager deals a hand of letters to the active player (LetterPool.DealHand)
2. Active player places letters onto valid answer cells
3. Active player submits turn (or timer expires → auto-submit placed letters)
4. ScoringSystem evaluates the placement, locks correct cells, awards points
5. TurnManager switches active player
6. Repeat until EndCondition is met
```

### 3.2 Letter Hand

- **Turn 1:** player is dealt **5 letters** from the pool.
- **Subsequent turns:** unused letters from the previous turn **carry over**. Player then receives **N new letters** from the pool to replenish, where N = letters correctly placed and locked last turn (max 5). Hand size is always capped at 5.
  - Example: had 5 letters, placed 3 correctly → 2 carry over + 3 new = 5.
  - Example: had 5 letters, placed 0 (passed) → 5 carry over + 0 new = 5.
- Letters are drawn from the pool of correct answers for unfilled cells (shuffled), so the player always receives valid letters — the challenge is figuring out which cell each letter belongs to.
- A **Shuffle** action (limited uses per match, shown in the reference image with count badge) lets the player reorder the tiles in their hand for no penalty.
- A **Pass** action ends the turn immediately; player receives 0 new letters next turn but keeps their current hand.

### 3.3 Letter Placement Rules

- The player may only place letters on unlocked answer cells (not black, not already locked).
- Letters can be rearranged freely before submitting.
- On submission, each placed letter is checked against the correct answer:
  - **Correct:** cell locks, points awarded, cell visually marked as solved.
  - **Incorrect:** **-1 point**, letter is returned to the player's hand, cell reverts to empty.
- This is a "check on submit" model — incorrect letters are allowed temporarily during placement.

### 3.4 Word Completion

A word slot is complete when every cell in it is locked. On completion:
- Visual state changes (e.g. highlight color on all cells in the slot).
- Word completion bonus points awarded to the player who locked the final cell.
- Completed cells cannot be targeted again by either player.

### 3.5 Scoring

| Event | Points |
|---|---|
| Correct letter placed | +1 |
| Incorrect letter placed (returned to hand) | -1 |
| Word completed (any length) | +5 |
| All hand letters placed correctly in one turn | +10 (efficiency bonus) |
| Full board completed | +50 (awarded to both players as a match-end bonus) |

Both scores are visible on screen at all times.

### 3.6 End Condition

The match ends when:
- All answer cells are locked (board complete), **or**
- `LetterPool` is exhausted and neither player can place (stuck state).

Result screen shows final scores, winner, and per-word breakdown.

### 3.7 Turn Timer

- Default: **30 seconds** per turn.
- On expiry, any letters placed so far are auto-submitted.
- Timer is configurable per level in `LevelData` (optional field, defaults to 30s).

---

## 4. Game Systems Architecture

### 4.1 System Map

```
LevelData (asset)
    └─ LevelManager          loads asset, creates BoardState + PlayerStates
         ├─ TurnManager       owns turn loop, timer, active player switching
         │    └─ LetterPool   manages unplaced letter pool, deals hands
         ├─ ScoringSystem     evaluates placements, fires score events
         ├─ GameStateManager  state machine (Idle→Playing→BotTurn→Result)
         ├─ GridView          instantiates and owns CellViews
         │    └─ CellView[]   one per non-black cell; renders letter/state/highlight
         ├─ CluePanel         two scroll lists (Across / Down); highlights active clue
         ├─ LetterHandView    renders the player's current letter tiles
         ├─ ScoreboardView    live scores + turn indicator + timer bar
         ├─ PlayerInput       touch tap to select cell, tap tile to assign letter
         └─ BotController     drives bot turns via BotStrategy
```

### 4.2 Scenes

| Scene | Purpose |
|---|---|
| `Home` | Level select list, tap to start match |
| `Game` | Full battle board + all in-game UI |

### 4.3 GameStateManager States

```
Idle → LoadingLevel → PlayerTurn → BotTurn → Result
```

`PlayerTurn` and `BotTurn` both funnel through the same `TurnManager` — the difference is whether `PlayerInput` or `BotController` drives letter placement.

### 4.4 LetterPool

- On `BoardState` init, builds a pool from all correct letters of all unfilled cells.
- `DealHand(int size)` shuffles pool and returns `size` letters without replacement.
- Tracks remaining count; signals `OnPoolEmpty` when exhausted.

### 4.5 CellView States

| State | Visual |
|---|---|
| Black / empty | Solid background color (no interaction) |
| Clue cell | Beige/cream fill, clue text or image centred, directional arrow (► or ▼) |
| Answer — empty | White fill, no letter |
| Answer — pending placement | Letter shown, yellow/gold tint (placed this turn, not yet submitted) |
| Answer — locked by player | Letter shown, green tint |
| Answer — locked by bot | Letter shown, orange tint |
| Completed word | All answer cells in the slot get a distinct solid accent color |

### 4.6 In-Game UI Elements

| Element | Description |
|---|---|
| Scoreboard | Player score (highlighted, left) vs Opponent score (right), always visible at top |
| Letter hand tray | 5 draggable/tappable letter tiles at bottom of screen |
| Shuffle button | Left of hand tray; limited uses (count badge shown); reorders hand tiles |
| PASS button | Centre below hand tray; ends turn with 0 placements |
| Hint button | Right of hand tray; limited uses (count badge); reveals one correct cell |

---

## 5. Bot System

### 5.1 Architecture

`BotController` holds a `BotStrategy` reference. Strategy is swapped at runtime based on `BotDifficulty` setting.

```
BotController
  BotDifficulty   difficulty     // Easy | Medium | Hard
  BotStrategy     strategy       // assigned from difficulty
  float           thinkDelay     // simulated thinking time in seconds (for UX feel)

BotStrategy (abstract)
  List<(Cell, char)> ChoosePlacements(BoardState, List<char> hand, LevelData)
```

### 5.2 Strategy Implementations

**EasyBotStrategy**
- Randomly selects cells from the hand.
- Places 2–4 letters per turn (leaves some unused).
- Does not consult clues; pure random targeting of empty cells.

**MediumBotStrategy**
- Identifies which hand letters match answer letters at specific positions.
- Places 4–5 letters per turn targeting partial word completions.
- Prioritizes words that are already half-filled.

**HardBotStrategy**
- Builds a full letter-to-cell mapping from remaining answer letters.
- Attempts to maximize word completions per turn.
- Places all 6 letters when possible; targets words with most locked neighbors.

### 5.3 Tunable Parameters

Exposed on `BotController` component:
- `difficulty` — Easy / Medium / Hard (swaps strategy)
- `thinkDelay` — float, seconds before bot submits (visual pacing)
- `handUsageRate` — float 0–1, fraction of hand letters actually placed (overrides per-strategy default for fine tuning)

---

## 6. Level Editor Tool

A custom `EditorWindow` at **Tools → Crossword Level Editor**.

### 6.1 Workflow

```
Step 1 — Grid Setup
  Set width × height (default 13×13).
  Three paint modes (toolbar toggle):
    • Black    — click/drag to mark cells as Black (no content)
    • Clue     — click a cell to mark it as a Clue cell; a direction
                 picker (► / ▼) appears; the immediately adjacent cell
                 in that direction becomes the first Answer cell
    • Answer   — click/drag to mark cells as Answer cells

Step 2 — Slot Auto-Detection
  Button scans the grid: for every Clue cell, trace Answer cells in
  the indicated direction until a Black or Clue cell is hit.
  Each contiguous Answer run becomes a WordSlot.
  Minimum run length: 1 (single-letter answers are allowed).

Step 3 — Clue Entry
  Slot list auto-populates below the grid.
  Each row shows: slot id, direction, answer length, and two fields:
    • Answer   — any string (word, abbreviation, fill-in-the-blank value)
    • Clue     — text field (leave empty if image-only)
    • Image    — Sprite picker (drag a Sprite asset; optional)
  Both text and image can be set simultaneously for combined clues.

Step 4 — Validate
  Runs LevelValidator; shows inline pass/fail per rule.
  Save is blocked if any error-level rule fails.
  Warnings allow save with confirmation.

Step 5 — Analyze Difficulty
  Runs DifficultyAnalyzer; shows score breakdown and computed tier.
  Designer can manually override the tier.

Step 6 — Save
  Creates or overwrites Assets/Levels/<title>.asset.
  Runs validation one more time before writing.
```

### 6.2 Implementation Notes

- Grid is rendered with IMGUI (`GUI.DrawTexture` + `GUILayout`). No Play Mode required.
- The window keeps an in-memory `LevelData` draft until saved.
- Levels can be re-opened for editing by dragging an existing `.asset` into the window.

---

## 7. Level Validation

Static class `LevelValidator` returns `List<ValidationResult>`. Each result has: `RuleId`, `Severity` (Error | Warning), `Message`, and optionally `AffectedSlotIds`.

### 7.1 Rules

| # | Rule | Severity |
|---|---|---|
| 1 | Grid is at least 3×3 | Error |
| 2 | All Answer cells form a single connected region (flood fill ignoring Black/Clue cells) | Error |
| 3 | Every Answer cell belongs to at least one WordSlot | Error |
| 4 | Every Clue cell has exactly one direction set and points to at least one Answer cell | Error |
| 5 | Every slot has a non-empty answer string matching its cell count exactly | Error |
| 6 | Every slot has either a non-empty clue text or a non-null clueSprite (or both) | Error |
| 7 | All intersection cells (Answer cells shared by an Across and a Down slot) have matching letters in both answers | Error |
| 8 | The same answer string appears in more than one slot | Warning |
| 9 | The level has fewer than 3 word slots total | Warning |

**Winnability definition:** A level is winnable if rules 1–7 all pass. Rule 7 (intersection consistency) is the critical guarantee — if all intersections agree, a player who correctly fills every slot will reach a complete, non-contradictory board.

---

## 8. Difficulty System

### 8.1 Score Formula

`DifficultyAnalyzer` computes a normalized score 0–100 from five weighted signals:

| Signal | Weight | How measured |
|---|---|---|
| Average word length | 25% | Mean letter count across all slots; longer = harder |
| Word frequency tier | 30% | Each answer rated Common/Uncommon/Rare via a bundled word frequency list; more rare words = harder |
| Grid fill ratio (black squares) | 20% | `blackCells / totalCells`; more black = fewer crossing helpers = harder |
| Intersection density | 15% | `intersectionCount / totalSlots`; fewer intersections = less help = harder |
| Word count | 10% | More words = more total effort = harder |

### 8.2 Tiers

| Score range | Tier |
|---|---|
| 0 – 33 | Easy |
| 34 – 66 | Medium |
| 67 – 100 | Hard |

### 8.3 Editor Display

The Level Editor shows a bar chart of the five signals, the weighted total, the computed tier, and a one-line human-readable summary (e.g. "High word frequency score and dense intersections make this easy"). Designer can override the tier label.

### 8.4 Level Comparison

The simulation output (Section 10) feeds an additional real-world difficulty signal: `averageTurnsToComplete` and `botWinRate` from headless runs. These are written to a JSON report file (not baked into the ScriptableObject) and displayed in the Simulation EditorWindow alongside the static score when the level is loaded.

---

## 9. Level Generator

Static class `LevelGenerator` (Editor only). Accessed via **Tools → Crossword Level Generator**.

### 9.1 Input

- Word + clue list: drag in a `.csv` file with columns `answer,clue`.
- Target grid size (width × height).
- Target difficulty tier (optional).

### 9.2 Algorithm

```
1. Sort words by length (descending).
2. Place the longest word horizontally at the center row.
3. For each remaining word:
   a. Find all letters in the word that match a letter already placed on the grid.
   b. For each candidate intersection, test if the word fits without
      violating existing cells or grid bounds.
   c. Place at the best-scoring intersection (maximises crossing count).
   d. If no intersection found, skip the word and try the next.
4. Fill remaining empty border cells as black.
5. Run auto-number.
6. Run LevelValidator; discard and retry (up to 10 attempts) if errors remain.
7. Run DifficultyAnalyzer; if target difficulty specified and score is outside
   ±15 of target tier midpoint, retry with a shuffled word order.
8. Output as LevelData ScriptableObject in Assets/Levels/Generated/.
```

The generator is best-effort — it will not always produce a result for very small grids or very short word lists. It reports a reason when it fails.

---

## 10. Bot Simulation

### 10.1 Purpose

Headless game runner to test whether levels are solvable, balanced, and appropriately difficult without manual play.

### 10.2 SimulationRunner

Static class. Takes `LevelData`, `BotDifficulty` for both sides, and `runCount`. Runs N complete games with two `BotController` instances (no UI, no delays) and returns `SimulationReport`.

```
SimulationReport
  int       runCount
  float     completionRate       // % of games where board was fully cleared
  float     stuckRate            // % of games where LetterPool exhausted before completion
  float     botWinRate           // bot (player 2) win % over N runs
  float     avgTurnsToComplete
  float     avgScoreGap          // abs(player1Score - player2Score) on average
  float     avgPlayer1Score
  float     avgPlayer2Score
```

### 10.3 Simulation EditorWindow

**Tools → Crossword Simulator**

- Drag in a `LevelData` asset.
- Set run count (default: 100).
- Set difficulty for each side independently.
- Run button executes synchronously (progress bar for large counts).
- Results displayed in the window and optionally exported to `Assets/Levels/Simulations/<LevelName>_sim.json`.

---

## 11. Folder Structure

```
Assets/
  Scripts/
    Core/
      Data/
        LevelData.cs
        CellData.cs
        WordSlotData.cs
        BoardState.cs
        PlayerState.cs
      Systems/
        LevelManager.cs
        TurnManager.cs
        LetterPool.cs
        ScoringSystem.cs
        GameStateManager.cs
      Views/
        GridView.cs
        CellView.cs
        CluePanel.cs
        LetterHandView.cs
        ScoreboardView.cs
      Input/
        PlayerInputController.cs
      Bot/
        BotController.cs
        BotStrategy.cs
        EasyBotStrategy.cs
        MediumBotStrategy.cs
        HardBotStrategy.cs
    Editor/
      LevelEditorWindow.cs
      LevelGeneratorWindow.cs
      SimulationWindow.cs
      LevelValidator.cs
      DifficultyAnalyzer.cs
      LevelGenerator.cs
      SimulationRunner.cs
  Levels/
    Sample/             hand-authored sample levels (≥3)
    Generated/          output from LevelGenerator
    Simulations/        JSON reports from SimulationRunner
  Prefabs/
    CellView.prefab
    LetterTile.prefab
  Scenes/
    Home.unity
    Game.unity
  Sprites/              placeholder art
  Resources/
    WordFrequency.txt   bundled word frequency list for difficulty scoring
```

---

## 12. Sample Content Plan

At least 3 hand-authored levels must ship with the prototype to demonstrate the full workflow:

| Level | Grid | Words | Target Difficulty |
|---|---|---|---|
| Tutorial | 7×7 | 6 | Easy |
| Standard | 11×11 | 14 | Medium |
| Challenge | 13×13 | 20 | Hard |

Each level must pass all validation rules before inclusion. Each must have a simulation report showing ≥ 90% completion rate with Hard bot on both sides.

---

## 13. Known Assumptions & Limitations

- **No network play.** Bot runs on the same device; no multiplayer infrastructure.
- **Letter hand always contains correct letters.** There are no "wrong" letters in the hand — difficulty comes from not knowing which cell each letter belongs to. The -1 penalty applies when the player places a letter in the wrong cell.
- **Unused letters carry over.** A player who never places any letters will accumulate the same 5 letters indefinitely until the pool changes; the bot will slowly fill the board around them.
- **No undo.** Once a turn is submitted, correct placements are locked.
- **Image clues are Sprite references.** The Level Editor requires Sprites to already exist as assets; there is no in-editor image import tool.
- **Word frequency list** applies only to text answers that are standard dictionary words. Abbreviations and fill-in-the-blank answers will score as "Rare" by default (conservative estimate, slight overestimate of difficulty).
- **Level Generator** produces text-only clues; image clues must be added manually after generation.
- **Simulation is synchronous** in the EditorWindow — very large run counts (>1000) may cause a brief editor freeze.
- **Platform-specific code** (Android back button, haptics, app lifecycle) is explicitly out of scope for this prototype.
