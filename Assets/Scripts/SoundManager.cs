using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private AudioSource openPackSource;
    [SerializeField] private AudioSource whooshSource;
    [SerializeField] private AudioSource openCardSource;
    [SerializeField] private AudioSource clickSource;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayOpenPack()
    {
        if (openPackSource != null)
        {
            openPackSource.Play();
        }
    }

    public void PlayOpenCard()
    {
        if (openCardSource != null)
        {
            openCardSource.Play();
        }
    }

    public void PlayClick()
    {
        if (clickSource != null)
        {
            clickSource.Play();
        }
    }

    public void PlayWhoosh()
    {
        if (whooshSource != null)
        {
            whooshSource.Play();
        }
    }
}