using System.Collections.Generic;
using System.Linq;

namespace CrosswordGo
{
    // Generates a random set of SlotDrafts for the standard 9×7 outer-edge layout:
    //   Across clues at col 0, words span cols 1–6 (6 letters)
    //   Down   clues at row 0, words span rows 1–8 (8 letters)
    //
    // Strategy: pick Down words first (they fix letter constraints), then find
    // Across words from the pool whose letters satisfy every Down constraint at
    // the selected columns.  Tries up to MaxAttempts random combos before
    // falling back to fewer Down slots.
    public static class LevelGenerator
    {
        private const int MaxAttempts = 200;

        // ── public entry point ────────────────────────────────────────────────

        public static List<SlotDraft> Generate(Difficulty difficulty, WordDatabase db = null) =>
            Generate(difficulty, new System.Random(), db);

        public static List<SlotDraft> Generate(Difficulty difficulty, System.Random rng, WordDatabase db = null)
        {
            string[] sixLetter  = db != null ? db.GetWordsOfLength(6)  : DefaultSixLetterWords;
            string[] eightLetter = db != null ? db.GetWordsOfLength(8) : DefaultEightLetterWords;

            int numDown = difficulty switch
            {
                Difficulty.Easy   => 0,
                Difficulty.Medium => 1 + rng.Next(2),    // 1–2
                Difficulty.Hard   => 4 + rng.Next(3),    // 4–6
                _                 => 0
            };

            for (int n = numDown; n >= 0; n--)
            {
                var result = n == 0
                    ? MakeAcrossOnly(rng, sixLetter)
                    : TryWithDown(n, rng, sixLetter, eightLetter);
                if (result != null) return result;
            }

            return MakeAcrossOnly(rng, sixLetter);
        }

        // ── generation ────────────────────────────────────────────────────────

        private static List<SlotDraft> MakeAcrossOnly(System.Random rng, string[] sixLetter)
        {
            var words = Shuffled(sixLetter, rng);
            var slots = new List<SlotDraft>();
            for (int r = 1; r <= 8; r++)
                slots.Add(MakeAcross(r, words[r - 1]));
            return slots;
        }

        private static List<SlotDraft> TryWithDown(int numDown, System.Random rng,
            string[] sixLetter, string[] eightLetter)
        {
            int[] allCols = { 1, 2, 3, 4, 5, 6 };

            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                int[]    downCols  = PickN(allCols, numDown, rng);
                string[] downWords = PickN(eightLetter, numDown, rng);

                string[] acrossWords = FindAcrossWords(downCols, downWords, rng, sixLetter);
                if (acrossWords == null) continue;

                var slots = new List<SlotDraft>();
                for (int r = 1; r <= 8; r++)
                    slots.Add(MakeAcross(r, acrossWords[r - 1]));
                for (int d = 0; d < numDown; d++)
                    slots.Add(MakeDown(downCols[d], downWords[d]));
                return slots;
            }

            return null;
        }

        // For each of the 8 rows find a 6-letter word whose letters at the Down
        // column positions match the corresponding Down word letters.
        private static string[] FindAcrossWords(int[] downCols, string[] downWords,
            System.Random rng, string[] sixLetter)
        {
            var result = new string[8];
            var used   = new HashSet<string>();
            var pool   = Shuffled(sixLetter, rng);

            for (int r = 0; r < 8; r++)
            {
                string found = null;
                foreach (var word in pool)
                {
                    if (used.Contains(word)) continue;

                    bool ok = true;
                    for (int d = 0; d < downCols.Length; d++)
                    {
                        // acrossPos: 0-indexed position inside the 6-letter answer
                        // (clueCol=0, so position = downCol - 1)
                        int  acrossPos = downCols[d] - 1;
                        char required  = downWords[d][r];
                        if (word[acrossPos] != required) { ok = false; break; }
                    }

                    if (ok) { found = word; break; }
                }

                if (found == null) return null;
                result[r] = found;
                used.Add(found);
            }

            return result;
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static SlotDraft MakeAcross(int row, string word) => new SlotDraft
            { direction = Direction.Across, clueRow = row, clueCol = 0, answer = word, clue = "" };

        private static SlotDraft MakeDown(int col, string word) => new SlotDraft
            { direction = Direction.Down, clueRow = 0, clueCol = col, answer = word, clue = "" };

        private static T[] Shuffled<T>(T[] source, System.Random rng)
        {
            var arr = (T[])source.Clone();
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr;
        }

        private static T[] PickN<T>(T[] source, int n, System.Random rng) =>
            Shuffled(source, rng).Take(n).ToArray();

        // ── built-in fallback word lists (used when no WordDatabase is provided) ──

        internal static readonly string[] DefaultSixLetterWords =
        {
            "ACCEPT", "ACROSS", "ACTIVE", "ADMIRE", "AFFORD", "ALMOST", "ALWAYS",
            "BRIDGE", "BRUTAL", "BUTTER",
            "CANDLE", "CASTLE", "CHERRY", "CIRCLE", "CLEVER", "CLOUDS", "COFFEE",
            "CORNER", "COTTON", "COUPLE", "COURSE", "CREDIT", "CUSTOM",
            "DANGER", "DESERT", "DOUBLE", "DREAMS",
            "ENABLE", "ENERGY", "ESCAPE",
            "FAMILY", "FAMOUS", "FIGURE", "FINISH", "FLOWER", "FOREST", "FROZEN", "FUTURE",
            "GARDEN", "GATHER", "GENTLE", "GOLDEN",
            "HARBOR", "HEALTH", "HIDDEN", "HOLLOW", "HONEST", "HUNGRY",
            "IMPACT", "INSECT", "ISLAND",
            "JOYFUL", "JUNGLE",
            "KNIGHT",
            "LAUNCH", "LINGER", "LISTEN", "LIVELY",
            "MASTER", "MEADOW", "MIRROR", "MODERN", "MONKEY",
            "NARROW", "NATURE", "NEEDLE", "NORMAL", "NOTICE",
            "OBJECT", "ORANGE",
            "PENCIL", "PRETTY", "PRINCE",
            "RABBIT", "RANDOM", "REPAIR", "RESCUE", "RETURN", "RIBBON",
            "SIMPLE", "SINGLE", "SPIDER", "SPRING", "STABLE", "STREAM", "STREET", "STRONG", "SUMMER",
            "TACKLE", "TARGET", "TEMPLE", "TENDER", "TICKET", "TRAVEL",
            "USEFUL",
            "VALLEY", "VERBAL", "VIOLET", "VISION",
            "WINTER", "WISDOM", "WONDER",
            "YELLOW",
        };

        internal static readonly string[] DefaultEightLetterWords =
        {
            "ABSOLUTE", "BACKBONE", "BALANCED", "BECOMING", "BIRTHDAY",
            "BREAKING", "BRINGING", "CALENDAR", "CAPTAINS", "CARDINAL",
            "CATCHING", "CHANGING", "CHILDREN", "CLIMBING", "CONSIDER",
            "CREATIVE", "DARKNESS", "DECIDING", "DESCRIBE", "DISCOVER",
            "DISTANCE", "DRAMATIC", "DRAWINGS", "ENORMOUS", "EVERYONE",
            "EXPECTED", "FINISHED", "FUNCTION", "GATHERED", "GREATEST",
            "HAPPENED", "INCLUDED", "INNOCENT", "LEARNING", "MIDNIGHT",
            "MOUNTAIN", "MOVEMENT", "ORIGINAL", "PAINTING", "PICTURES",
            "PLEASANT", "POSSIBLE", "PRACTICE", "PRESENCE", "QUESTION",
            "RAILROAD", "REMEMBER", "RESOURCE", "ROMANTIC", "SCHEDULE",
            "SEPARATE", "SHEPHERD", "SLEEPING", "SMALLEST", "SPECIFIC",
            "STANDING", "SURPRISE", "THOUSAND", "TOGETHER", "TOMORROW",
            "TROUBLED", "UMBRELLA", "VACATION", "WHATEVER", "WHENEVER",
            "YOURSELF",
        };
    }
}
