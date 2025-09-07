using System.Collections;
using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Match3Tray.UI
{
    public class UIButton : UIElement, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool ButtonDisabled;
        public UnityEvent ClickAction = new();
        public UnityEvent<UnityAction> Disabled = new();
        public UnityEvent<UnityAction> Enabled = new();
        public bool NoAnimation;

        [Header("Hold Settings")] public bool EnableHold;

        public float HoldInvokeInterval = 0.1f;
        private readonly float _animationDuration = 0.1f;

        private readonly float _scaleDownSize = 0.85f;
        private Coroutine _holdCoroutine;

        private Vector3 _originalScale;
        private Tween _scaleTween;

        private void Start()
        {
            _originalScale = !IsVisible ? Vector3.one : transform.localScale;
            if (ButtonDisabled) Disabled?.Invoke(null);
        }

        /// <summary>
        ///     Normal tek tıklama (Pointer Up anında çalışan mekanik)
        /// </summary>
        public void OnPointerClick(PointerEventData data)
        {
            if (ButtonDisabled) return;
            if (data.button == PointerEventData.InputButton.Left) ClickAction.Invoke();
        }

        /// <summary>
        ///     Basılı tutmaya başlarken
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (ButtonDisabled) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;

            if (EnableHold)
            {
                StopHold();
                _holdCoroutine = StartCoroutine(HoldRoutine());
            }

            if (NoAnimation) return;
            _scaleTween.Complete();
            var tr = transform;
            var trLocalScale = tr.localScale;
            _scaleTween = Tween.Scale(tr, trLocalScale, _originalScale * _scaleDownSize, _animationDuration, Ease.InSine);
        }

        /// <summary>
        ///     İşaretçi butonun dışına çıkarsa hold'u da keseriz.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (ButtonDisabled) return;
            if (EnableHold)
                StopHold();
            if (NoAnimation) return;
            _scaleTween.Complete();
            var tr = transform;
            _scaleTween = Tween.Scale(tr, tr.localScale, _originalScale, _animationDuration, Ease.OutSine);
        }

        /// <summary>
        ///     Basılı tutma bittiğinde veya parmak/kursor kalktığında
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            if (ButtonDisabled) return;
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (EnableHold)
                StopHold();
            if (NoAnimation) return;
            _scaleTween.Complete();
            var tr = transform;
            _scaleTween = Tween.Scale(tr, tr.localScale, _originalScale, _animationDuration, Ease.OutSine);
        }

        public void SetDisabled()
        {
            ButtonDisabled = true;
            Disabled?.Invoke(null);
        }

        public void SetEnabled()
        {
            ButtonDisabled = false;
            Enabled?.Invoke(null);
        }

        /// <summary>
        ///     Basılı tutma tween'i yerine coroutine'i kullanarak her HoldInvokeInterval'de bir click tetikler.
        /// </summary>
        private IEnumerator HoldRoutine()
        {
            while (true)
            {
                ClickAction.Invoke();
                yield return new WaitForSeconds(HoldInvokeInterval);
            }
        }

        /// <summary>
        ///     Basılı tutmayı durdurur.
        /// </summary>
        private void StopHold()
        {
            if (_holdCoroutine == null) return;
            StopCoroutine(_holdCoroutine);
            _holdCoroutine = null;
        }
    }
}