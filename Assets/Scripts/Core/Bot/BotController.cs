using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CrosswordGo
{
    public class BotController : MonoBehaviour
    {
        [SerializeField] private Difficulty difficulty = Difficulty.Medium;
        [SerializeField] private float thinkDelay = 1.5f;
        [Range(0f, 1f)]
        [SerializeField] private float handUsageRate = 1f; // 1 = use all matchable letters

        private BotStrategy _strategy;

        private void Awake() => ApplyDifficulty();

        public void SetDifficulty(Difficulty d)
        {
            difficulty = d;
            ApplyDifficulty();
        }

        private void ApplyDifficulty()
        {
            _strategy = difficulty switch
            {
                Difficulty.Easy   => new EasyBotStrategy(),
                Difficulty.Hard   => new HardBotStrategy(),
                _                 => new MediumBotStrategy()
            };
        }

        // Called by TurnManager during a bot turn.
        // Returns the placements after simulating think time.
        public IEnumerator TakeTurn(
            LevelData level, BoardState board, PlayerState botState,
            System.Action<List<(int, char)>> onComplete)
        {
            yield return new WaitForSeconds(thinkDelay);

            var placements = _strategy.ChoosePlacements(level, board, botState.hand);

            // Apply handUsageRate: trim to fraction of matchable placements
            if (handUsageRate < 1f)
            {
                int keep = Mathf.Max(0, Mathf.FloorToInt(placements.Count * handUsageRate));
                placements = placements.GetRange(0, keep);
            }

            onComplete(placements);
        }

        // Headless version for SimulationRunner (no coroutine, no delay).
        public List<(int, char)> TakeTurnImmediate(
            LevelData level, BoardState board, PlayerState botState)
        {
            var placements = _strategy.ChoosePlacements(level, board, botState.hand);
            if (handUsageRate < 1f)
            {
                int keep = Mathf.Max(0, Mathf.FloorToInt(placements.Count * handUsageRate));
                placements = placements.GetRange(0, keep);
            }
            return placements;
        }
    }
}
