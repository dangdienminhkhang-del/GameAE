using UnityEngine;

public static class TimeProbe
{
    public static string Run()
    {
        var pc = GameObject.FindGameObjectWithTag("Player");
        return "timeScale=" + Time.timeScale + " time=" + Time.time.ToString("0.00") + " unscaled=" + Time.unscaledTime.ToString("0.00") + " frame=" + Time.frameCount + " playerPos=" + (pc != null ? pc.transform.position.ToString() : "null");
    }
}
