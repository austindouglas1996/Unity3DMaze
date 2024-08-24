using System.Collections.Generic;
using UnityEngine;

public class LightCycle : MonoBehaviour
{
    [SerializeField] private List<Light> lights = new List<Light>();
    [SerializeField] private float timeBetweenLights = 0.5f;

    private int currentLight = 0;
    private float currentTimeOnLight = 0f;

    private void Update()
    {
        currentTimeOnLight += Time.deltaTime;
        if (currentTimeOnLight >= timeBetweenLights)
        {
            currentTimeOnLight = 0f;
            currentLight += 1;

            if (currentLight >= lights.Count)
            {
                currentLight = 0;
            }

            SetLightActive();
        }
    }

    private void SetLightActive()
    {
        for (int i = 0; i < lights.Count; i++)
        {
            if (i == currentLight)
            {
                lights[i].gameObject.SetActive(true);
            }
            else
            {
                lights[i].gameObject.SetActive(false);
            }
        }
    }
}