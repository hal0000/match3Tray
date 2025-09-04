using System;

namespace Match3Tray.Manager
{
    public static class EventManager
    {
        public static bool IsGamePaused { get; private set; }
        /// <summary>
        /// Event triggered when the game's pause state changes.
        /// </summary>
        public static event Action<bool> OnPauseChanged;

        /// <summary>
        /// Sets the game's pause state and notifies subscribers.
        /// </summary>
        /// <param name="isPaused">The new pause state</param>
        public static void SetPause(bool isPaused)
        {
            IsGamePaused = isPaused;
            OnPauseChanged?.Invoke(isPaused);
        }
    }
}