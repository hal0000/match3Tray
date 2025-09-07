using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Match3Tray.UI
{
    public class UIBlurEffect : MonoBehaviour
    {
        public Material BlurMaterial;
        public int BlurPasses = 2;
        public float BlurSize = 1f;
        public Image TargetImage;
        public bool UpdateEnabled;
        private Camera _cam;
        private RenderTexture _rt1;
        private RenderTexture _rt2;

        private void Awake()
        {
            _cam = Camera.main;
        }

        private void Update()
        {
            if (!UpdateEnabled) return;
            TakeScreenShot();
        }

        public void OnSoftShow()
        {
            StartCoroutine(EaseAlpha(0, 1, 0.2f));
            TargetImage.raycastTarget = true;
            int width = Screen.width;
            int height = Screen.height;
            _rt1 = new RenderTexture(width, height, 24);
            _rt2 = new RenderTexture(width, height, 24);
            TakeScreenShot();
            UpdateEnabled = true;
        }

        public void OnSoftHide()
        {
            UpdateEnabled = false;
            if (_rt1 != null)
            {
                _rt1.Release();
                _rt1 = null;
            }

            if (_rt2 != null)
            {
                _rt2.Release();
                _rt2 = null;
            }

            StartCoroutine(EaseAlpha(1, 0, 0.2f));
            TargetImage.raycastTarget = false;
            TargetImage.material.mainTexture = null;
            TargetImage.material = null;
        }

        private void TakeScreenShot()
        {
            if (_rt1 == null || _rt2 == null) return;
            if (transform.localScale == Vector3.zero) return;
            _cam.targetTexture = _rt1;
            _cam.Render();
            _cam.targetTexture = null;
            RenderTexture currentSource = _rt1;
            RenderTexture currentDestination = _rt2;
            for (int i = 0; i < BlurPasses; i++)
            {
                BlurMaterial.SetVector("_Direction", new Vector4(1f, 0f, 0f, 0f));
                BlurMaterial.SetFloat("_BlurSize", BlurSize);
                Graphics.Blit(currentSource, currentDestination, BlurMaterial);
                RenderTexture temp = currentSource;
                currentSource = currentDestination;
                currentDestination = temp;
                BlurMaterial.SetVector("_Direction", new Vector4(0f, 1f, 0f, 0f));
                BlurMaterial.SetFloat("_BlurSize", BlurSize);
                Graphics.Blit(currentSource, currentDestination, BlurMaterial);
                temp = currentSource;
                currentSource = currentDestination;
                currentDestination = temp;
            }

            TargetImage.material.mainTexture = currentSource;
            TargetImage.SetVerticesDirty();
            TargetImage.SetMaterialDirty();
        }

        private IEnumerator EaseAlpha(float startAlpha, float TargetAlpha, float duration)
        {
            float elapsedTime = 0f;
            Color temp = TargetImage.color;
            temp.a = startAlpha;
            TargetImage.color = temp;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                temp.a = Mathf.LerpUnclamped(startAlpha, TargetAlpha, t);
                TargetImage.color = temp;
                yield return null;
            }

            temp.a = TargetAlpha;
            TargetImage.color = temp;
        }
    }
}