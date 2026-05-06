using System;
using UnityEngine;

namespace CrosswordGo
{
    public enum GamePhase { Idle, LoadingLevel, PlayerTurn, BotTurn, Result }

    public class GameStateManager : MonoBehaviour
    {
        public GamePhase CurrentPhase { get; private set; } = GamePhase.Idle;
        public event Action<GamePhase> OnPhaseChanged;

        public void SetPhase(GamePhase phase)
        {
            CurrentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
