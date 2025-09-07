using System.Runtime.CompilerServices;
using Match3Tray.Logging;
using UnityEngine;

namespace Match3Tray.UI
{
    /// <summary>
    ///     Applies safe area adjustments for notched devices.
    ///     Attach this component to the top-level UI panel (or its immediate child if a full-screen background is used).
    /// </summary>
    public sealed class SafeArea : MonoBehaviour
    {
        [SerializeField] private bool ConformX = true; // Conform on X-axis
        [SerializeField] private bool ConformY = true; // Conform on Y-axis
        private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;
        private Rect _lastSafeArea = new(0, 0, 0, 0);
        private Vector2Int _lastScreenSize = new(0, 0);

        private RectTransform _panel;

        private void Awake()
        {
            _panel = GetComponent<RectTransform>();
            if (_panel == null)
            {
                LoggerExtra.LogError("SafeArea: No RectTransform found on " + name);
                Destroy(gameObject);
                return;
            }

            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Refresh()
        {
            int screenW = Screen.width, screenH = Screen.height;
            var orientation = Screen.orientation;
            var safeArea = GetSafeArea();
            if (safeArea.Equals(_lastSafeArea) && _lastScreenSize.x == screenW && _lastScreenSize.y == screenH && orientation == _lastOrientation) return;
            _lastScreenSize = new Vector2Int(screenW, screenH);
            _lastOrientation = orientation;
            ApplySafeArea(safeArea, screenW, screenH);
            _lastSafeArea = safeArea;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Rect GetSafeArea()
        {
            var safeArea = Screen.safeArea;
            if (!Application.isEditor || Sim == SimDevice.None) return safeArea;
            Rect nsa = new(0, 0, 1, 1);
            var isPortrait = Screen.height > Screen.width;
            switch (Sim)
            {
                case SimDevice.IPhoneX:
                    nsa = isPortrait ? NSA_iPhoneX[0] : NSA_iPhoneX[1];
                    break;
                case SimDevice.IPhoneXsMax:
                    nsa = isPortrait ? NSA_iPhoneXsMax[0] : NSA_iPhoneXsMax[1];
                    break;
                case SimDevice.Pixel3XlLsl:
                    nsa = isPortrait ? NSA_Pixel3XL_LSL[0] : NSA_Pixel3XL_LSL[1];
                    break;
                case SimDevice.Pixel3XlLsr:
                    nsa = isPortrait ? NSA_Pixel3XL_LSR[0] : NSA_Pixel3XL_LSR[1];
                    break;
            }

            safeArea = new Rect(Screen.width * nsa.x, Screen.height * nsa.y, Screen.width * nsa.width, Screen.height * nsa.height);
            return safeArea;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplySafeArea(Rect safeArea, int screenWidth, int screenHeight)
        {
            if (!ConformX)
            {
                safeArea.x = 0;
                safeArea.width = screenWidth;
            }

            if (!ConformY)
            {
                safeArea.y = 0;
                safeArea.height = screenHeight;
            }

            var anchorMin = safeArea.position;
            var anchorMax = safeArea.position + safeArea.size;
            anchorMin.x /= screenWidth;
            anchorMin.y /= screenHeight;
            anchorMax.x /= screenWidth;
            anchorMax.y /= screenHeight;

            if (float.IsNaN(anchorMin.x) || float.IsNaN(anchorMin.y) || float.IsNaN(anchorMax.x) || float.IsNaN(anchorMax.y)) return;
            _panel.anchorMin = anchorMin;
            _panel.anchorMax = anchorMax;
        }

        #region Simulation Constants (Static Readonly)

        public enum SimDevice
        {
            None,
            IPhoneX,
            IPhoneXsMax,
            Pixel3XlLsl,
            Pixel3XlLsr
        }

        public static SimDevice Sim = SimDevice.None;

        private static readonly Rect[] NSA_iPhoneX =
        {
            new(0f, 102f / 2436f, 1f, 2202f / 2436f), // Portrait
            new(132f / 2436f, 63f / 1125f, 2172f / 2436f, 1062f / 1125f) // Landscape
        };

        private static readonly Rect[] NSA_iPhoneXsMax =
        {
            new(0f, 102f / 2688f, 1f, 2454f / 2688f), // Portrait
            new(132f / 2688f, 63f / 1242f, 2424f / 2688f, 1179f / 1242f) // Landscape
        };

        private static readonly Rect[] NSA_Pixel3XL_LSL =
        {
            new(0f, 0f, 1f, 2789f / 2960f), // Portrait
            new(171f / 2960f, 0f, 2789f / 2960f, 1f) // Landscape (Landscape Left simulated as this)
        };

        private static readonly Rect[] NSA_Pixel3XL_LSR =
        {
            new(0f, 0f, 1f, 2789f / 2960f), // Portrait
            new(0f, 0f, 2789f / 2960f, 1f) // Landscape (Landscape Right simulated as this)
        };

        #endregion
    }
}