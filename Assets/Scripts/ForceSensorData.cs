using System;
using UnityEngine;

[System.Serializable]
public struct ForceSensorData
{
    public int sensorId;
    public float rawValue;
    public float normalizedValue; // 0-1 range
    public float timestamp;
    
    public ForceSensorData(int id, float raw, float normalized, float time)
    {
        sensorId = id;
        rawValue = raw;
        normalizedValue = normalized;
        timestamp = time;
    }
    
    public override string ToString()
    {
        return $"Sensor {sensorId}: Raw={rawValue:F1}, Normalized={normalizedValue:F3}, Time={timestamp:F2}";
    }
}

[System.Serializable]
public class ForceSensorConfig
{
    public int sensorId;
    public float activationThreshold = 0.1f;
    public float deactivationThreshold = 0.05f;
    [Range(0.1f, 1f)]
    public float smoothingFactor = 0.8f;
}
