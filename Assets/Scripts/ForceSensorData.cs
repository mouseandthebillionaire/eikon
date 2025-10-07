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
    [Header("Sensor Identification")]
    public int sensorId;
    public string sensorName;
    
    [Header("Calibration")]
    public float minValue = 0f;
    public float maxValue = 1023f;
    public bool useCustomRange = false;
    
    [Header("Filtering")]
    public bool enableSmoothing = true;
    [Range(0.1f, 1f)]
    public float smoothingFactor = 0.8f;
    
    [Header("Thresholds")]
    public float activationThreshold = 0.1f;
    public float deactivationThreshold = 0.05f;
    
    [Header("Visualization")]
    public Color sensorColor = Color.white;
    public bool showInUI = true;
}

public class ForceSensorManager : MonoBehaviour
{
    [Header("Sensor Configuration")]
    [SerializeField] private ForceSensorConfig[] sensorConfigs = new ForceSensorConfig[6];
    [SerializeField] private bool autoCreateConfigs = true;
    
    [Header("Data Management")]
    [SerializeField] private int maxDataHistory = 100;
    [SerializeField] private bool enableDataLogging = false;
    
    // Current sensor data
    private ForceSensorData[] currentSensorData = new ForceSensorData[6];
    private float[] smoothedValues = new float[6];
    private bool[] sensorActive = new bool[6];
    
    // Data history for analysis
    private System.Collections.Generic.Queue<ForceSensorData[]> dataHistory = 
        new System.Collections.Generic.Queue<ForceSensorData[]>();
    
    // Events
    public static event Action<ForceSensorData> OnSensorValueChanged;
    public static event Action<int, bool> OnSensorActivationChanged;
    public static event Action<ForceSensorData[]> OnAllSensorsUpdated;
    
    void Start()
    {
        InitializeSensorConfigs();
        SubscribeToArduinoEvents();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromArduinoEvents();
    }
    
    private void InitializeSensorConfigs()
    {
        if (autoCreateConfigs)
        {
            for (int i = 0; i < 6; i++)
            {
                if (sensorConfigs[i] == null)
                {
                    sensorConfigs[i] = new ForceSensorConfig
                    {
                        sensorId = i,
                        sensorName = $"Force Sensor {i + 1}",
                        sensorColor = GetDefaultSensorColor(i)
                    };
                }
            }
        }
        
        // Initialize arrays
        for (int i = 0; i < 6; i++)
        {
            smoothedValues[i] = 0f;
            sensorActive[i] = false;
        }
    }
    
    private Color GetDefaultSensorColor(int index)
    {
        Color[] colors = {
            Color.red, Color.green, Color.blue, 
            Color.yellow, Color.magenta, Color.cyan
        };
        return colors[index % colors.Length];
    }
    
    private void SubscribeToArduinoEvents()
    {
        ArduinoCommunication.OnSensorDataReceived += ProcessSensorData;
        ArduinoCommunication.OnConnectionStatusChanged += OnArduinoConnectionChanged;
        ArduinoCommunication.OnErrorOccurred += OnArduinoError;
    }
    
    private void UnsubscribeFromArduinoEvents()
    {
        ArduinoCommunication.OnSensorDataReceived -= ProcessSensorData;
        ArduinoCommunication.OnConnectionStatusChanged -= OnArduinoConnectionChanged;
        ArduinoCommunication.OnErrorOccurred -= OnArduinoError;
    }
    
    private void ProcessSensorData(ForceSensorData[] newData)
    {
        for (int i = 0; i < newData.Length && i < 6; i++)
        {
            ProcessIndividualSensor(newData[i]);
        }
        
        // Store in history
        if (enableDataLogging)
        {
            StoreDataInHistory(newData);
        }
        
        // Notify all sensors updated
        OnAllSensorsUpdated?.Invoke(currentSensorData);
    }
    
    private void ProcessIndividualSensor(ForceSensorData data)
    {
        if (data.sensorId >= 6) return;
        
        ForceSensorConfig config = sensorConfigs[data.sensorId];
        
        // Apply custom range if configured
        float processedValue = data.normalizedValue;
        if (config.useCustomRange)
        {
            processedValue = Mathf.InverseLerp(config.minValue, config.maxValue, data.rawValue);
        }
        
        // Apply smoothing
        if (config.enableSmoothing)
        {
            smoothedValues[data.sensorId] = Mathf.Lerp(smoothedValues[data.sensorId], processedValue, config.smoothingFactor);
            processedValue = smoothedValues[data.sensorId];
        }
        
        // Update current data
        currentSensorData[data.sensorId] = new ForceSensorData(
            data.sensorId, 
            data.rawValue, 
            processedValue, 
            data.timestamp
        );
        
        // Check activation state
        bool wasActive = sensorActive[data.sensorId];
        bool isActive = processedValue > config.activationThreshold;
        
        if (wasActive && !isActive && processedValue < config.deactivationThreshold)
        {
            sensorActive[data.sensorId] = false;
            OnSensorActivationChanged?.Invoke(data.sensorId, false);
        }
        else if (!wasActive && isActive)
        {
            sensorActive[data.sensorId] = true;
            OnSensorActivationChanged?.Invoke(data.sensorId, true);
        }
        
        // Notify value change
        OnSensorValueChanged?.Invoke(currentSensorData[data.sensorId]);
    }
    
    private void StoreDataInHistory(ForceSensorData[] data)
    {
        dataHistory.Enqueue(data);
        if (dataHistory.Count > maxDataHistory)
        {
            dataHistory.Dequeue();
        }
    }
    
    private void OnArduinoConnectionChanged(bool connected)
    {
        Debug.Log($"Arduino connection status: {(connected ? "Connected" : "Disconnected")}");
    }
    
    private void OnArduinoError(string error)
    {
        Debug.LogError($"Arduino Error: {error}");
    }
    
    // Public API
    public ForceSensorData GetSensorData(int sensorId)
    {
        if (sensorId >= 0 && sensorId < 6)
        {
            return currentSensorData[sensorId];
        }
        return new ForceSensorData();
    }
    
    public ForceSensorData[] GetAllSensorData()
    {
        return (ForceSensorData[])currentSensorData.Clone();
    }
    
    public bool IsSensorActive(int sensorId)
    {
        if (sensorId >= 0 && sensorId < 6)
        {
            return sensorActive[sensorId];
        }
        return false;
    }
    
    public ForceSensorConfig GetSensorConfig(int sensorId)
    {
        if (sensorId >= 0 && sensorId < 6)
        {
            return sensorConfigs[sensorId];
        }
        return null;
    }
    
    public void UpdateSensorConfig(int sensorId, ForceSensorConfig config)
    {
        if (sensorId >= 0 && sensorId < 6)
        {
            sensorConfigs[sensorId] = config;
        }
    }
    
    public ForceSensorData[][] GetDataHistory()
    {
        return dataHistory.ToArray();
    }
    
    public void ClearDataHistory()
    {
        dataHistory.Clear();
    }
    
    // For debugging in inspector
    [ContextMenu("Print Current Sensor Data")]
    public void PrintCurrentSensorData()
    {
        for (int i = 0; i < 6; i++)
        {
            Debug.Log(currentSensorData[i].ToString());
        }
    }
}
