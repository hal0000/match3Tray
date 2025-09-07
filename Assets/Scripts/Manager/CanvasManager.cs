using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Match3Tray.Manager
{
    [RequireComponent(typeof(Canvas))]
    [ExecuteInEditMode]
    public class CanvasManager : MonoBehaviour
    {
        public UnityEvent OnResolutionOrOrientationChanged;

        private CanvasScaler _canvasScaler;
        private Vector2 _lastResolution = Vector2.zero;
        private bool _screenChangeVarsInitialized;

        private void Awake()
        {
            _canvasScaler = GetComponent<CanvasScaler>();
            if (_screenChangeVarsInitialized) return;
            _lastResolution.x = Screen.width;
            _lastResolution.y = Screen.height;
            _screenChangeVarsInitialized = true;
            SetScaleFactor();
        }

        private void Update()
        {
            if (!(Math.Abs(Screen.width - _lastResolution.x) > 2) && !(Math.Abs(Screen.height - _lastResolution.y) > 2)) return;
            ResolutionChanged();
        }

        public void SetScaleFactor()
        {
            var designSize = _canvasScaler.referenceResolution;
            var ww = designSize.x;
            var hh = designSize.y;
            var ratio = hh / ww;

            float w = Screen.width;
            float h = (int)(w * ratio);
            if (h - 2 > Screen.height)
            {
                h = Screen.height;
                w = (int)(h / ratio);
                _canvasScaler.matchWidthOrHeight = 1;
            }
            else
            {
                _canvasScaler.matchWidthOrHeight = 0;
            }
        }

        public void ResolutionChanged()
        {
            _lastResolution.x = Screen.width;
            _lastResolution.y = Screen.height;
            OnResolutionOrOrientationChanged?.Invoke();
        }
    }
}