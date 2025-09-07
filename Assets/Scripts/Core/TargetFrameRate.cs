using UnityEngine;

namespace Match3Tray.Core
{
    [DefaultExecutionOrder(-1000)]
    public sealed class TargetFramerate : MonoBehaviour
    {
        [SerializeField] private int hardCapHz = 240;
        [SerializeField] private int fallbackHz = 60;
        [SerializeField] private bool log;

        private void Awake()
        {
            Application.targetFrameRate = 120;
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnApplicationFocus(bool f)
        {
            if (f) Apply();
        }

        private void Apply()
        {
            QualitySettings.vSyncCount = 0;
            var hz = GetBestRefreshRate();
            hz = Mathf.Clamp(hz, 30, hardCapHz);
            Application.targetFrameRate = hz;
            if (log) Debug.Log($"[TargetFramerate] Set {hz} Hz");
        }

        private int GetBestRefreshRate()
        {
            var hz = GetUnityReportedHz();
#if UNITY_ANDROID && !UNITY_EDITOR
        int aHz = GetAndroidNativeHz();
        if (aHz > hz) hz = aHz;
#endif
            return hz > 0 ? hz : fallbackHz;
        }

        private int GetUnityReportedHz()
        {
#if UNITY_2022_2_OR_NEWER
            var hz = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
#else
        int hz = Screen.currentResolution.refreshRate;
#endif
            if (hz > 0) return hz;

            var res = Screen.resolutions;
            for (var i = 0; i < res.Length; i++)
            {
#if UNITY_2022_2_OR_NEWER
                var r = Mathf.RoundToInt((float)res[i].refreshRateRatio.value);
#else
            int r = res[i].refreshRate;
#endif
                if (r > hz) hz = r;
            }

            return hz;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    int GetAndroidNativeHz()
    {
        try
        {
            using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = up.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var ctx = activity.Call<AndroidJavaObject>("getApplicationContext"))
            using (var dm = ctx.Call<AndroidJavaObject>("getSystemService", "display"))
            {
                float rr = 0f;

                if (dm != null)
                {
                    var display = dm.Call<AndroidJavaObject>("getDisplay", 0); // DEFAULT_DISPLAY = 0
                    if (display != null) rr = display.Call<float>("getRefreshRate");
                }

                if (rr <= 0f)
                {
                    using (var wm = activity.Call<AndroidJavaObject>("getSystemService", "window"))
                    {
                        var defDisp = wm?.Call<AndroidJavaObject>("getDefaultDisplay");
                        if (defDisp != null) rr = defDisp.Call<float>("getRefreshRate");
                    }
                }

                int hz = Mathf.RoundToInt(rr);
                return hz;
            }
        }
        catch { return 0; }
    }
#endif
    }
}