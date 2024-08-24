using UnityEngine;
using VHierarchy.Libs;

[RequireComponent(typeof(BoxCollider))]
public class LandMine : MonoBehaviour
{
    [SerializeField] private GameObject ShinyLight;
    [SerializeField] private AudioSource ExplosionSound;
    [SerializeField] private AudioSource BeepSound;
    [SerializeField] private ParticleSystem ExplosionParticle;
    [SerializeField] private int Damage = 100;

    [SerializeField] private float TimeInSecondsToShowLight = 0.5f;
    [SerializeField] private float TimeInSecondsBetweenShowLight = 4f;
    [SerializeField] private float TimeInSecondsBetweenBeep = 4f;

    private float ElapsedTimeSinceBeep = 0f;
    private float ElapsedTimeSinceShowLight = 0f;
    private float ElapsedTimeShowingLight = 0f;

    private bool IsExploded = false;
    private BoxCollider Collider;

    public void Explode()
    {
        ExplosionSound.Play();
        ExplosionParticle.Play();

        ShinyLight.Destroy();
        this.IsExploded = true;
    }

    private void Start()
    {
        this.Collider = GetComponent<BoxCollider>();
    }

    private void Update()
    {
        if (IsExploded)
        {
            if (!ExplosionSound.isPlaying && !ExplosionParticle.isPlaying)
            {
                Destroy(gameObject);
            }
            return;
        }

        // Handle Beeping
        ElapsedTimeSinceBeep += Time.deltaTime;
        if (ElapsedTimeSinceBeep > TimeInSecondsBetweenBeep)
        {
            PlayBeep();
            ElapsedTimeSinceBeep = 0f;
        }

        // Handle Light Showing
        ElapsedTimeShowingLight += Time.deltaTime;
        if (ElapsedTimeShowingLight > TimeInSecondsToShowLight)
        {
            HideLight();
            ElapsedTimeShowingLight = 0f;
        }

        // Handle Light Appearance Timing
        ElapsedTimeSinceShowLight += Time.deltaTime;
        if (ElapsedTimeSinceShowLight > TimeInSecondsBetweenShowLight)
        {
            ShowLight();
            ElapsedTimeSinceShowLight = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        this.Explode();
    }

    private void PlayBeep()
    {
        this.BeepSound.Play();
        this.ElapsedTimeSinceBeep = 0f;
    }

    private void ShowLight()
    {
        this.ShinyLight.SetActive(true);
        this.ElapsedTimeShowingLight = 0f;
    }

    private void HideLight()
    {
        this.ShinyLight?.SetActive(false);
        this.ElapsedTimeShowingLight = 0f;
    }
}