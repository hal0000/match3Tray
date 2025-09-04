using System.Collections.Generic;
using Match3Tray.Core;
using UnityEngine;

namespace Match3Tray.Manager
{
    /// <summary>
    /// Central manager class that handles core game systems, scene management, and resource loading.
    /// Implements the Singleton pattern for global access to game systems.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /// <summary>
        /// The currently active scene in the game.
        /// </summary>
        public BaseScene CurrentScene;

        /// <summary>
        /// Singleton instance of the GameManager for global access.
        /// </summary>
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            if (Instance != null && Instance != this) Destroy(gameObject);
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}