using UnityEngine;

public class PercussionScript : MonoBehaviour
{   
    [Header("Percussion Settings")]
    public AudioSource[] percussionSources;
    
    [Header("Timing Settings")]
    public float baseInterval = 1.0f; // Base interval between percussion hits
    public float minInterval = 0.1f;  // Minimum interval (fastest possible)
    public float maxInterval = 2.0f;  // Maximum interval (slowest possible)
    public float forceMultiplier = 2.0f; // How much force affects the interval
    
    [Header("Random Variation")]
    [Range(0f, 0.1f)]
    public float randomVariation = 0.02f; // Random timing variation (0-0.1 seconds)
    
    [Header("FSR Mapping")]
    public int[] fsrToPercussionMapping = {0, 1, 2, 3, 4, 5}; // Maps FSR index to percussion source index
    
    [Header("Debug")]
    public bool enableDebugLogging = false;
    
    // Timing variables for each FSR
    private float[] lastTriggerTime = new float[6];
    private float[] nextTriggerTime = new float[6];
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize timing arrays
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

    // Update is called once per frame
    void Update()
    {
        if (GameManager.S == null) return;
        
        // Get FSR current hold times from GameManager
        float[] fsrHoldTimes = {
            GameManager.S.fsr0CurrentHoldTime,
            GameManager.S.fsr1CurrentHoldTime,
            GameManager.S.fsr2CurrentHoldTime,
            GameManager.S.fsr3CurrentHoldTime,
            GameManager.S.fsr4CurrentHoldTime,
            GameManager.S.fsr5CurrentHoldTime
        };
        
        // Get FSR force values
        FSR[] fsrComponents = FindObjectsOfType<FSR>();
        float[] fsrForces = new float[6];
        for (int i = 0; i < Mathf.Min(fsrComponents.Length, 6); i++)
        {
            fsrForces[i] = fsrComponents[i].force;
        }
        
        // Check each FSR for percussion triggering
        for (int fsrIndex = 0; fsrIndex < 6; fsrIndex++)
        {
            // Only trigger if FSR is being held (currentHoldTime > 0)
            if (fsrHoldTimes[fsrIndex] > 0f)
            {
                // Calculate interval based on force (higher force = faster intervals)
                float forceValue = fsrForces[fsrIndex];
                float calculatedInterval = CalculateInterval(forceValue);
                
                // Initialize nextTriggerTime if it's 0 (first time this FSR is active)
                if (nextTriggerTime[fsrIndex] == 0f)
                {
                    // Trigger immediately on first press, then use calculated interval
                    nextTriggerTime[fsrIndex] = Time.time;
                }
                
                // Check if it's time to trigger percussion
                if (Time.time >= nextTriggerTime[fsrIndex])
                {
                    if (enableDebugLogging)
                    {
                        Debug.Log($"Triggering percussion for FSR {fsrIndex}, force: {forceValue:F2}, interval: {calculatedInterval:F3}s (with random variation)");
                    }
                    
                    TriggerPercussion(fsrIndex);
                    lastTriggerTime[fsrIndex] = Time.time;
                    nextTriggerTime[fsrIndex] = Time.time + calculatedInterval;
                }
            }
            else
            {
                // Reset timing when FSR is not being held
                nextTriggerTime[fsrIndex] = 0f;
            }
        }
    }
    
    private float CalculateInterval(float force)
    {
        // Higher force = faster intervals (lower interval time)
        // Force is typically 0-1, so we invert it and scale it
        float forceFactor = 1f - Mathf.Clamp01(force);
        float interval = Mathf.Lerp(minInterval, maxInterval, forceFactor);
        
        // Apply force multiplier for more dramatic effect
        interval = Mathf.Clamp(interval / forceMultiplier, minInterval, maxInterval);
        
        // Add random variation to make it sound less mechanical
        float randomOffset = Random.Range(-randomVariation, randomVariation);
        interval = Mathf.Max(interval + randomOffset, minInterval); // Ensure we don't go below minimum
        
        return interval;
    }
    
    private void TriggerPercussion(int fsrIndex)
    {
        // Map FSR index to percussion source index
        int percussionIndex = fsrToPercussionMapping[fsrIndex];
        
        if (enableDebugLogging)
        {
            Debug.Log($"Attempting to trigger percussion: FSR {fsrIndex} -> Percussion {percussionIndex}");
            Debug.Log($"PercussionSources array: {(percussionSources != null ? "Exists" : "NULL")}");
            if (percussionSources != null)
            {
                Debug.Log($"PercussionSources length: {percussionSources.Length}");
                Debug.Log($"Target percussion source: {(percussionIndex >= 0 && percussionIndex < percussionSources.Length ? "Valid" : "INVALID")}");
                if (percussionIndex >= 0 && percussionIndex < percussionSources.Length)
                {
                    Debug.Log($"Percussion source at index {percussionIndex}: {(percussionSources[percussionIndex] != null ? "Exists" : "NULL")}");
                }
            }
        }
        
        // Make sure we have a valid percussion source
        if (percussionSources != null && 
            percussionIndex >= 0 && 
            percussionIndex < percussionSources.Length && 
            percussionSources[percussionIndex] != null)
        {
            percussionSources[percussionIndex].Play();
            
            if (enableDebugLogging)
            {
                Debug.Log($"Successfully played percussion source {percussionIndex}");
            }
        }
        else
        {
            if (enableDebugLogging)
            {
                Debug.LogWarning($"Failed to play percussion: FSR {fsrIndex} -> Percussion {percussionIndex}");
            }
        }
    }
    
    [ContextMenu("Debug Percussion State")]
    public void DebugPercussionState()
    {
        Debug.Log("=== PercussionScript Debug Info ===");
        Debug.Log($"GameManager.S: {(GameManager.S != null ? "Found" : "NULL")}");
        
        if (GameManager.S != null)
        {
            Debug.Log($"FSR Hold Times: {GameManager.S.fsr0CurrentHoldTime:F2}, {GameManager.S.fsr1CurrentHoldTime:F2}, {GameManager.S.fsr2CurrentHoldTime:F2}, {GameManager.S.fsr3CurrentHoldTime:F2}, {GameManager.S.fsr4CurrentHoldTime:F2}, {GameManager.S.fsr5CurrentHoldTime:F2}");
        }
        
        Debug.Log($"PercussionSources: {(percussionSources != null ? $"Array of {percussionSources.Length} sources" : "NULL")}");
        if (percussionSources != null)
        {
            for (int i = 0; i < percussionSources.Length; i++)
            {
                Debug.Log($"  Source {i}: {(percussionSources[i] != null ? "Valid" : "NULL")}");
            }
        }
        
        Debug.Log($"FSR Mapping: [{string.Join(", ", fsrToPercussionMapping)}]");
        Debug.Log("================================");
    }
}
