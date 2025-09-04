using Match3Tray.Manager;
using UnityEngine;

namespace Match3Tray.Core
{
    public abstract class MonoBehaviourExtra : MonoBehaviour
    {
        protected bool _isPaused;

        private void Update()
        {
            if (_isPaused) return;
            Tick();
        }

        protected virtual void OnEnable() => EventManager.OnPauseChanged += HandlePauseChanged;

        protected virtual void OnDisable() => EventManager.OnPauseChanged -= HandlePauseChanged;

        protected virtual void HandlePauseChanged(bool paused)
        {
            _isPaused = paused;
            OnPauseUpdate(paused);
        }
        protected virtual void OnPauseUpdate(bool paused) { }
        protected abstract void Tick();
    }
}