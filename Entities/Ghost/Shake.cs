using System.Collections;
using UnityEngine;

public class Shake : MonoBehaviour
{
    public float shakeDuration = 1f; // Duration of the shake
    public float shakeMagnitude = 0.5f; // Magnitude of the shake
    public bool isShaking = false; // Flag to start/stop shaking

    private Vector3 originalPosition;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    public void StartShaking(float duration)
    {
        this.shakeDuration = duration;

        if (!isShaking)
        {
            isShaking = true;
            originalPosition = transform.localPosition;
            StartCoroutine(ShakeObject());
        }
    }

    public void StopShaking()
    {
        isShaking = false;
    }

    private IEnumerator ShakeObject()
    {
        float elapsedTime = 0f;

        while (isShaking && elapsedTime < shakeDuration)
        {
            Vector3 randomPoint = originalPosition + Random.insideUnitSphere * shakeMagnitude;
            transform.localPosition = randomPoint;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        isShaking = false;
    }
}
