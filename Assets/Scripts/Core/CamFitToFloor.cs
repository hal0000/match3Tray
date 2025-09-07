using UnityEngine;

namespace Match3Tray.Core
{
    public sealed class CamFitToFloor : MonoBehaviour
    {
        [Header("Refs")]
        public Camera cam;
        public Renderer floorRenderer;

        [Header("Padding (ratio)")]
        [Range(0f, 0.5f)] public float padding = 0.05f; // %5 pay

        void Awake()
        {
            if (!cam) cam = Camera.main;
            if (!floorRenderer) floorRenderer = GetComponentInChildren<Renderer>();
            if (!cam || !floorRenderer) return;

            if (!cam.orthographic) cam.orthographic = true; // garanti

            // Floor'un dünya uzayındaki AABB köşeleri
            var b = floorRenderer.bounds;
            var c = b.center;
            var e = b.extents;

            Vector3[] corners =
            {
                c + new Vector3(-e.x,-e.y,-e.z), c + new Vector3( e.x,-e.y,-e.z),
                c + new Vector3(-e.x,-e.y, e.z), c + new Vector3( e.x,-e.y, e.z),
                c + new Vector3(-e.x, e.y,-e.z), c + new Vector3( e.x, e.y,-e.z),
                c + new Vector3(-e.x, e.y, e.z), c + new Vector3( e.x, e.y, e.z),
            };

            // Kamera uzayına projeksiyon (XY'de kapla)
            var w2c = cam.worldToCameraMatrix;
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

            for (int i = 0; i < 8; i++)
            {
                var v = w2c.MultiplyPoint3x4(corners[i]);
                if (v.x < minX) minX = v.x; if (v.x > maxX) maxX = v.x;
                if (v.y < minY) minY = v.y; if (v.y > maxY) maxY = v.y;
            }

            float halfW = (maxX - minX) * 0.5f;
            float halfH = (maxY - minY) * 0.5f;

            // Ortho size: yükseklik yarısı; genişlik için aspect'e böl
            float aspect = Mathf.Max(0.0001f, cam.aspect);
            float needed = Mathf.Max(halfH, halfW / aspect);
            cam.orthographicSize = needed * (1f + padding);
        }
    }
}