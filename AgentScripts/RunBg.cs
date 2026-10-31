using UnityEngine;

public static class RunBg
{
    public static string Run()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        return "runInBackground=" + Application.runInBackground + " targetFrameRate=" + Application.targetFrameRate + " frame=" + Time.frameCount + " time=" + Time.time.ToString("0.00");
    }
}
