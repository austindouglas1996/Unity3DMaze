using System.Collections;
using UnityEngine;

public class LightController : MonoBehaviour
{
    public Light pointLight;
    public Light pointLight1;
    public float targetIntensity = 5f;
    public float targetTemperature = 6500f;
    public Color targetColor = Color.white;
    public float duration = 6f;

    private float initialIntensity;
    private float initialTemperature;
    private Color initialColor;

    private void Start()
    {
        if (pointLight == null)
        {
            Debug.LogError("Point Light is not assigned.");
            return;
        }

        // Initialize the initial values
        initialIntensity = pointLight.intensity;
        initialTemperature = pointLight.colorTemperature;
        initialColor = pointLight.color;
    }

    public IEnumerator RevertLightProperties()
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Lerp the properties
            pointLight1.intensity = Mathf.Lerp(3, 0, t);
            pointLight.intensity = Mathf.Lerp(targetIntensity, initialIntensity, t);
            pointLight.colorTemperature = Mathf.Lerp(targetTemperature, initialTemperature, t);
            pointLight.color = Color.Lerp(targetColor, initialColor, t);

            yield return null; // Wait for the next frame
        }

        // Ensure the final values are set
        pointLight.intensity = initialIntensity;
        pointLight.colorTemperature = initialTemperature;
        pointLight.color = initialColor;
    }

    public IEnumerator ChangeLightProperties()
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Lerp the properties
            pointLight1.intensity = Mathf.Lerp(0, 3, t);
            pointLight.intensity = Mathf.Lerp(initialIntensity, targetIntensity, t);
            pointLight.colorTemperature = Mathf.Lerp(initialTemperature, targetTemperature, t);
            pointLight.color = Color.Lerp(initialColor, targetColor, t);

            yield return null; // Wait for the next frame
        }

        // Ensure the final values are set
        pointLight.intensity = targetIntensity;
        pointLight.colorTemperature = targetTemperature;
        pointLight.color = targetColor;
    }
}
