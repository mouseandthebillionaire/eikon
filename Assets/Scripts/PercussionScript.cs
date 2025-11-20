using UnityEngine;

public class PercussionScript : MonoBehaviour
{   
    [Header("Percussion Settings")]
    public AudioSource[] percussionSources;
    
    [Header("Timing Settings")]
    public float baseInterval = 1.0f;
    public float minInterval = 0.1f;
    public float maxInterval = 2.0f;
    public float forceMultiplier = 2.0f;
    
    [Header("Random Variation")]
    [Range(0f, 0.1f)]
    public float randomVariation = 0.02f;
    
    [Header("FSR Mapping")]
    public int[] fsrToPercussionMapping = {0, 1, 2, 3, 4, 5};
    
    [Header("Debug")]
    public bool enableDebugLogging = false;
    
    private float[] lastTriggerTime = new float[6];
    private float[] nextTriggerTime = new float[6];
    
    void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            lastTriggerTime[i] = 0f;
            nextTriggerTime[i] = 0f;
        }
        
        if (enableDebugLogging)
        {
            Debug.Log($"PercussionScript initialized with {percussionSources?.Length ?? 0} percussion sources");
        }
    }

    void Update()
    {
        if (GameManager.S == null) return;
        
        float[] fsrHoldTimes = {
            GameManager.S.fsr0CurrentHoldTime,
            GameManager.S.fsr1CurrentHoldTime,
            GameManager.S.fsr2CurrentHoldTime,
            GameManager.S.fsr3CurrentHoldTime,
            GameManager.S.fsr4CurrentHoldTime,
            GameManager.S.fsr5CurrentHoldTime
        };
        
        FSR[] fsrComponents = FindObjectsOfType<FSR>();
        float[] fsrForces = new float[6];
        for (int i = 0; i < Mathf.Min(fsrComponents.Length, 6); i++)
        {
            fsrForces[i] = fsrComponents[i].modifiedForce;
        }
        
        for (int fsrIndex = 0; fsrIndex < 6; fsrIndex++)
        {
            if (fsrHoldTimes[fsrIndex] > 0f)
            {
                float forceValue = fsrForces[fsrIndex];
                float calculatedInterval = CalculateInterval(forceValue);
                
                if (nextTriggerTime[fsrIndex] == 0f)
                {
                    nextTriggerTime[fsrIndex] = Time.time;
                }
                
                if (Time.time >= nextTriggerTime[fsrIndex])
                {
                    if (enableDebugLogging)
                    {
                        Debug.Log($"Triggering percussion for FSR {fsrIndex}, force: {forceValue:F2}, interval: {calculatedInterval:F3}s");
                    }
                    
                    TriggerPercussion(fsrIndex);
                    lastTriggerTime[fsrIndex] = Time.time;
                    nextTriggerTime[fsrIndex] = Time.time + calculatedInterval;
                }
            }
            else
            {
                nextTriggerTime[fsrIndex] = 0f;
            }
        }
    }
    
    private float CalculateInterval(float force)
    {
        float forceFactor = 1f - Mathf.Clamp01(force);
        float interval = Mathf.Lerp(minInterval, maxInterval, forceFactor);
        interval = Mathf.Clamp(interval / forceMultiplier, minInterval, maxInterval);
        
        float randomOffset = Random.Range(-randomVariation, randomVariation);
        interval = Mathf.Max(interval + randomOffset, minInterval);
        
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
            percussionSources[percussionIndex].Play();
        }
        else if (enableDebugLogging)
        {
            Debug.LogWarning($"Failed to play percussion: FSR {fsrIndex} -> Percussion {percussionIndex}");
        }
    }
}
