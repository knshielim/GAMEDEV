using UnityEngine;

public class WeatherDebugBox : MonoBehaviour
{
    public WeatherType selected = WeatherType.Sunny;
    public float testDuration = 300f; // 5 menit

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 220, 170), GUI.skin.box);
        GUILayout.Label("WEATHER DEBUG");

        GUILayout.Label("Selected:");
        selected = (WeatherType)GUILayout.SelectionGrid(
            (int)selected,
            new[] { "Sunny", "Fog", "AcidRain" },
            1
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Apply Weather"))
        {
            if (WeatherManager.Instance != null)
                WeatherManager.Instance.StartWeather(selected, testDuration);
        }

        if (GUILayout.Button("Stop Ambience (Back to Sunny)"))
        {
            if (WeatherManager.Instance != null)
                WeatherManager.Instance.StartWeather(WeatherType.Sunny, testDuration);
        }

        GUILayout.EndArea();
    }
}
