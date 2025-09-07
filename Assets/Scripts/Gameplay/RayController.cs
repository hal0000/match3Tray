using System;
using System.Collections.Generic;
using Match3Tray.Core;
using Match3Tray.Interface;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Match3Tray.Gameplay
{
    public class RayController : MonoBehaviourExtra
    {
        static readonly RaycastHit[] _hits = new RaycastHit[8];
        static readonly List<RaycastResult> _uiHits = new List<RaycastResult>(8);

        public Camera Cam;
        public LayerMask ItemMask;
        public event Action<IFruit> OnPicked;

        InputAction _press;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_press == null)
            {
                _press = new InputAction(type: InputActionType.Button);
                _press.AddBinding("<Pointer>/press");
                _press.AddBinding("<Touchscreen>/primaryTouch/press");
                _press.AddBinding("<Pen>/tip"); // stylus
                _press.started += OnPressStarted;
            }
            _press.Enable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_press == null) return;
            _press.started -= OnPressStarted;
            _press.Disable();
        }

        void OnPressStarted(InputAction.CallbackContext ctx)
        {
            if (Cam == null) Cam = Camera.main;
            if (Cam == null) return;

            Vector2 screenPos;

            var dev = ctx.control.device;
            if (dev is Mouse m)            screenPos = m.position.ReadValue();
            else if (dev is Touchscreen t) screenPos = t.primaryTouch.position.ReadValue();
            else if (dev is Pen pen)       screenPos = pen.position.ReadValue();
            else if (Pointer.current != null) screenPos = Pointer.current.position.ReadValue();
            else return;

            if (IsOverUI(screenPos)) return; // güvenilir UI filtresi
            TryPick(screenPos);
        }

        bool IsOverUI(Vector2 screenPos)
        {
            if (EventSystem.current == null) return false;
            var ped = new PointerEventData(EventSystem.current) { position = screenPos };
            _uiHits.Clear();
            EventSystem.current.RaycastAll(ped, _uiHits);
            return _uiHits.Count > 0;
        }

        void TryPick(Vector2 screenPos)
        {
            var ray = Cam.ScreenPointToRay(screenPos);
            int n = Physics.RaycastNonAlloc(ray, _hits, 100f, ItemMask, QueryTriggerInteraction.Ignore);
            if (n <= 0) return;
            int best = 0;
            float d = _hits[0].distance;
            for (int i = 1; i < n; i++)
                if (_hits[i].distance < d) { d = _hits[i].distance; best = i; }

            var col = _hits[best].collider;
            if (col == null) return;
            // collider child'da ise parenttan bile komponenti yakala
            if (col.transform.parent.TryGetComponent<IFruit>(out var temp))
            {
                OnPicked?.Invoke(temp);
            }
        }

        protected override void Tick() { /* event-driven; boş */ }
    }
}