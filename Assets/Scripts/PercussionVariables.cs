using UnityEngine;

public class PercussionVariables : MonoBehaviour
{
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

    [Header("Echo Delay")]
    public float minEchoDelay = 1200f;
    public float maxEchoDelay = 50f;

    public static PercussionVariables S;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        S = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
