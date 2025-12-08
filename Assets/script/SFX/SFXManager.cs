using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }
    
    [Header("Settings")]
    public float defaultPitch = 1f;
    public float Volume = 1f;
    public float SpatialBlend = 1f;
    public float MinDistance = 1f;
    public float MaxDistance = 50f;
    public float Spread = 0f;


    [Header("References")]
    [SerializeField] private GameObject sfxPlayerPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // optional if you want it persistent
    }

    private void SetSettings(AudioSource source, float speed = 1f)
    {
        source.pitch = speed;
        source.volume = Volume;
        source.spatialBlend = SpatialBlend;
        source.minDistance = MinDistance;
        source.maxDistance = MaxDistance;
        source.spread = Spread;
    }

    public void PlaySFX(AudioClip clip, Vector3 position, float speed = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("SFXManager: Missing AudioClip!");
            return;
        }

        GameObject audioObject = Instantiate(sfxPlayerPrefab.gameObject, position, Quaternion.identity);
        AudioSource source = audioObject.GetComponent<AudioSource>();
        SetSettings(source, speed);
        source.clip = clip;
        source.Play();

        Destroy(audioObject, clip.length);
    }

    public void PlaySFXatSource(AudioClip clip, AudioSource source, float speed = 1f, bool loop = false)
    {
        if (clip == null)
        {
            Debug.LogWarning("SFXManager: Missing AudioClip!");
            return;
        }

        SetSettings(source, speed);
        source.clip = clip;
        source.loop = loop;
        source.Play();
    }

    public void StopSFXatSource(AudioSource source)
    {
        if (source == null)
            return;

        source.clip = null;
        source.loop = false;
        source.Stop();
    }

    

    public GameObject PlaySFXLooping(AudioClip clip, Vector3 position, float speed = 1f)
    {
        if (clip == null)
        {
            Debug.LogWarning("SFXManager: Missing AudioClip!");
            return null;
        }

        GameObject audioObject = Instantiate(sfxPlayerPrefab.gameObject, position, Quaternion.identity);
        AudioSource source = audioObject.GetComponent<AudioSource>();
        SetSettings(source, speed);
        source.clip = clip;
        source.loop = true;
        source.Play();
        return audioObject;
    }

    public void StopSFXLooping(GameObject audioObject)
    {
        if (audioObject == null)
            return;

        AudioSource source = audioObject.GetComponent<AudioSource>();
        source.Stop();
        Destroy(audioObject);
    }
}
