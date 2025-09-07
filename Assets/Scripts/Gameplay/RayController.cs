using System;
using Match3Tray.Core;
using Match3Tray.Interface;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Match3Tray.Gameplay
{
    public class RayController : MonoBehaviourExtra
    {
        private static readonly RaycastHit[] _hits = new RaycastHit[8];
        public Camera Cam;
        public LayerMask ItemMask;
        public event Action<IFruit> OnPicked;

        private void TryPick(Vector2 screenPos)
        {
            var ray = Cam.ScreenPointToRay(screenPos);
            var n = Physics.RaycastNonAlloc(ray, _hits, 100f, ItemMask, QueryTriggerInteraction.Ignore);
            if (n <= 0) return;
            var best = 0;
            var d = _hits[0].distance;
            for (var i = 1; i < n; i++)
                if (_hits[i].distance < d)
                {
                    d = _hits[i].distance;
                    best = i;
                }

            if (_hits[best].collider.transform.parent.TryGetComponent<IFruit>(out var fruit)) OnPicked?.Invoke(fruit);
        }

        protected override void Tick()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (!Input.GetMouseButtonDown(0)) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            TryPick(Input.mousePosition);
#else
    if (Input.touchCount > 0)
    {
        var t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))return;
            TryPick(t.position);
        }
    }
#endif
        }
    }
}