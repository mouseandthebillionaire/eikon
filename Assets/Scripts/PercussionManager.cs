using UnityEngine;

public class PercussionManager : MonoBehaviour
{   
    [Header("Percussion Settings")]
    public AudioSource[] percussionSources;
    
    [Header("Timing Settings")]
    public float baseInterval = 1.0f;
    public float minInterval = 0.5f;
    public float maxInterval = 2.0f;
    public float forceMultiplier = 2.0f;
    [Tooltip("Maximum modifiedForce value. Interval reaches minInterval only at this force.")]
    public float maxForceValue = 4.0f;
    
    [Header("Random Variation")]
    [Range(0f, 0.1f)]
    public float randomVariation = 0.02f;

    [Header("Pitch Variations")]
    public float pitchInterval = 20.0f;
    public float[] pitchValues = {0.5f, 1.0f, 1.5f, 2.0f};


    [Header("FSR Mapping")]
    public int[] fsrToPercussionMapping = {0, 1, 2, 3, 4, 5};
    
    [Header("Debug")]
    public bool enableDebugLogging = false;
    
    private float[] lastTriggerTime = new float[6];
    
    void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            lastTriggerTime[i] = 0f;
        }
    }

    void Update()
    {
        if (GameManager.S == null) return;
        
        FSR[] fsrComponents = FindObjectsOfType<FSR>();
        float[] fsrForces = new float[6];
        for (int i = 0; i < Mathf.Min(fsrComponents.Length, 6); i++)
        {
            // Use modifiedForce which includes amplification for better responsiveness
            fsrForces[i] = fsrComponents[i].modifiedForce;
        }
        
        for (int fsrIndex = 0; fsrIndex < 6; fsrIndex++)
        {
            // Is the FSR being pressed?
            if (GameManager.S.fsrCurrentHoldTimes[fsrIndex] > 0f)
            {
                float forceValue = fsrForces[fsrIndex];
                float calculatedInterval = CalculateInterval(forceValue);
                
                // Check if enough time has passed since last trigger
                float timeSinceLastTrigger = Time.time - lastTriggerTime[fsrIndex];
                
                if (timeSinceLastTrigger >= calculatedInterval || lastTriggerTime[fsrIndex] == 0f)
                {
                    if (enableDebugLogging)
                    {
                        Debug.Log($"Triggering percussion for FSR {fsrIndex}, force: {forceValue:F2}, interval: {calculatedInterval:F3}s");
                    }
                    
                    TriggerPercussion(fsrIndex);
                    lastTriggerTime[fsrIndex] = Time.time;
                }
            }
            else
            {
                // Reset when not pressed
                lastTriggerTime[fsrIndex] = 0f;
            }
        }
    }
    
    private float CalculateInterval(float force)
    {
        // Normalize modifiedForce from 0 to maxForceValue (e.g., 0-4)
        // This ensures minInterval is only reached at maximum force
        float normalizedForce = Mathf.Clamp01(force / maxForceValue);
        
        // Invert: hard press (maxForceValue) = minInterval, soft press (0.0) = maxInterval
        float forceFactor = 1f - normalizedForce;
        float interval = Mathf.Lerp(minInterval, maxInterval, forceFactor);
        
        // Add random variation
        float randomOffset = Random.Range(-randomVariation, randomVariation);
        interval = Mathf.Clamp(interval + randomOffset, minInterval, maxInterval);
        
        return interval;
    }
    
    private void TriggerPercussion(int fsrIndex)
    {
        int percussionIndex = fsrToPercussionMapping[fsrIndex];
        
        if (percussionSources != null && 
            percussionIndex >= 0 && 
            percussionIndex < percussionSources.Length && 
            percussionSources[percussionIndex] != null)
        {
            // As time increases make the note pitch change randomly at certain pitch intervals (0.5, 1, 1.5, 2)
            // This should only happen once the note has been held for longer than a certain interval (pitchInterval)
            if (GameManager.S.fsrCurrentHoldTimes[fsrIndex] > pitchInterval)
            {
                percussionSources[percussionIndex].pitch = pitchValues[Random.Range(0, pitchValues.Length)];
            }
            else
            {
                // Reset pitch to default when not in pitch variation mode
                percussionSources[percussionIndex].pitch = 1.0f;
            }
            
            // Use PlayOneShot for one-shot percussion sounds - prevents looping and allows overlapping sounds
            // Note: PlayOneShot uses the AudioSource's volume, so echo effects will still work
            if (percussionSources[percussionIndex].clip != null)
            {
                percussionSources[percussionIndex].PlayOneShot(percussionSources[percussionIndex].clip);
            }
        }
        else if (enableDebugLogging)
        {
            Debug.LogWarning($"Failed to play percussion: FSR {fsrIndex} -> Percussion {percussionIndex}");
        }
    }
}
