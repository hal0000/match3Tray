using System;
using Match3Tray.Model;

namespace Match3Tray.Manager
{
    public static class EventManager
    {
        public static bool IsGamePaused { get; private set; }

        /// <summary>
        ///     Event triggered when the game's pause state changes.
        /// </summary>
        public static event Action<bool> OnPauseChanged;

        /// <summary>
        ///     Sets the game's pause state and notifies subscribers.
        /// </summary>
        /// <param name="isPaused">The new pause state</param>
        public static void SetPause(bool isPaused)
        {
            IsGamePaused = isPaused;
            OnPauseChanged?.Invoke(isPaused);
        }
        #region GameStateActions

        /// <summary>
        ///     Delegate for handling player events.
        /// </summary>
        public delegate void GameState(Enums.GameState type);

        /// <summary>
        ///     Event triggered when a player make an action
        /// </summary>
        public static event GameState OnGameStateChanged;


        /// <summary>
        ///     Notifies subscribers that a player did something
        /// </summary>
        public static void GameStateChanged(Enums.GameState type)
        {
            OnGameStateChanged?.Invoke(type);
        }

        #endregion
    }
}