using UnityEngine;
using System.Collections;

public class PercussionScript : MonoBehaviour
{   
    public FSR fsr;
    public AudioClip clip;

    [Header("Audio Sources")]
    [Tooltip("Array of 4 AudioSources for round-robin playback")]
    public AudioSource[] audioSources = new AudioSource[4];
    
    private int currentAudioSourceIndex = 0;
    private Coroutine playAudioCoroutine;
    private Coroutine decreaseVolumeCoroutine;
    
    void Start()
    {
        // If audioSources aren't assigned, try to get them from children or create them
        if (audioSources[0] == null)
        {
            // Try to find AudioSources on this GameObject or children
            AudioSource[] foundSources = GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < Mathf.Min(4, foundSources.Length); i++)
            {
                audioSources[i] = foundSources[i];
                audioSources[i].clip = clip;
            }
        }
    }

    void Update()
    {
        if (fsr.currentHoldTime > 0f)
        {
            // Set volume to 1 for all audio sources
            foreach (AudioSource source in audioSources)
            {
                if (source != null)
                {
                    source.volume = 1f;
                }
            }
            
            // Stop volume decrease coroutine if running
            if (decreaseVolumeCoroutine != null)
            {
                StopCoroutine(decreaseVolumeCoroutine);
                decreaseVolumeCoroutine = null;
            }
            
            // Start the coroutine if it's not already running
            if (playAudioCoroutine == null)
            {
                playAudioCoroutine = StartCoroutine(PlayAudio());
            }
        } 
        else 
        {   
            // Start volume decrease if not already running
            if (decreaseVolumeCoroutine == null)
            {
                decreaseVolumeCoroutine = StartCoroutine(DecreaseVolume());
            }
        }
    }

    public IEnumerator PlayAudio()
    {
        // Get the next AudioSource in round-robin fashion
        AudioSource currentSource = audioSources[currentAudioSourceIndex];
        
        if (currentSource != null)
        {
            // Play on this AudioSource (can overlap with others for echo effects)
            currentSource.Play();

            // Set Echo Delay: light force = 1200ms, full force = 50ms
            // Normalize force from 0 to maxForceValue
            float normalizedForce = Mathf.Clamp01(fsr.modifiedForce / PercussionVariables.S.maxForceValue);
            // Lerp from 1200ms (light) to 50ms (full force), convert to seconds for AudioEchoFilter
            float delayMs = Mathf.Lerp(PercussionVariables.S.minEchoDelay, PercussionVariables.S.maxEchoDelay, normalizedForce);
            
            AudioEchoFilter echoFilter = currentSource.GetComponent<AudioEchoFilter>();
            echoFilter.delay = delayMs;
            
            
        }

        // Move to next AudioSource for next trigger (round-robin)
        currentAudioSourceIndex = (currentAudioSourceIndex + 1) % audioSources.Length;

        // Calculate the interval based on the force of the FSR
        float interval = CalculateInterval(fsr.modifiedForce);
        Debug.Log("Interval: " + interval);
        yield return new WaitForSeconds(interval);

        // Only continue if FSR is still being held
        if (fsr.currentHoldTime > 0f)
        {
            playAudioCoroutine = StartCoroutine(PlayAudio());
        }
        else
        {
            playAudioCoroutine = null;
        }
    }

    public IEnumerator DecreaseVolume()
    {
        // Decrease volume for all audio sources
        foreach (AudioSource source in audioSources)
        {
            if (source != null && source.volume > 0f)
            {
                source.volume = Mathf.Max(0f, source.volume - 0.025f);
            }
        }
        
        yield return new WaitForSeconds(0.25f);
        StartCoroutine(DecreaseVolume());

    }

    private float CalculateInterval(float force)
    {
        // Normalize modifiedForce from 0 to maxForceValue (e.g., 0-4)
        // This ensures minInterval is only reached at maximum force
        float normalizedForce = Mathf.Clamp01(force / PercussionVariables.S.maxForceValue);
        
        // Invert: hard press (maxForceValue) = minInterval, soft press (0.0) = maxInterval
        float forceFactor = 1f - normalizedForce;
        float interval = Mathf.Lerp(PercussionVariables.S.minInterval, PercussionVariables.S.maxInterval, forceFactor);
        
        return interval;
    }
}