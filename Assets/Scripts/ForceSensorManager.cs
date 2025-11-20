using System;
using UnityEngine;
using System.Collections.Generic;

public class ForceSensorManager : MonoBehaviour
{
    [Header("Force Response Curve")]
    [Tooltip("Exponential power for force response. Lower values (0.2-0.4) create more dramatic ramping on harder presses. 1.0 = linear, 0.3 = very aggressive amplification")]
    [Range(0.1f, 2.0f)]
    [SerializeField] private float exponentialPower = 0.3f;
    [SerializeField] private bool enableExponentialCurve = true;
    
    [Header("High Pressure Amplification")]
    [Tooltip("Multiplier for maximum pressure. At full pressure, modifiedForce will be force * this value. Lower pressures are less affected.")]
    [Range(1.0f, 5.0f)]
    [SerializeField] private float maxPressureMultiplier = 3.5f;
    [Tooltip("Pressure threshold below which no amplification is applied (0-1). Lower values amplify more gradually.")]
    [Range(0f, 0.8f)]
    [SerializeField] private float amplificationStartThreshold = 0.2f;
    
    [Header("Sensor Settings")]
    [SerializeField] private float activationThreshold = 0.1f;
    [SerializeField] private float deactivationThreshold = 0.05f;
    [Range(0.1f, 1f)]
    [SerializeField] private float smoothingFactor = 0.8f;
    [SerializeField] private bool enableSmoothing = true;
    
    [Header("Data Management")]
    [SerializeField] private int maxDataHistory = 100;
    [SerializeField] private bool enableDataLogging = false;
    
    private ForceSensorData[] currentSensorData = new ForceSensorData[6];
    private float[] smoothedValues = new float[6];
    private bool[] sensorActive = new bool[6];
    private ForceSensorConfig[] sensorConfigs = new ForceSensorConfig[6];
    
    private Queue<ForceSensorData[]> dataHistory = new Queue<ForceSensorData[]>();
    
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
        for (int i = 0; i < 6; i++)
        {
            sensorConfigs[i] = new ForceSensorConfig
            {
                sensorId = i,
                activationThreshold = activationThreshold,
                deactivationThreshold = deactivationThreshold,
                smoothingFactor = smoothingFactor
            };
            
            smoothedValues[i] = 0f;
            sensorActive[i] = false;
        }
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
        
        if (enableDataLogging)
        {
            StoreDataInHistory(newData);
        }
        
        OnAllSensorsUpdated?.Invoke(currentSensorData);
    }
    
    private void ProcessIndividualSensor(ForceSensorData data)
    {
        if (data.sensorId >= 6) return;
        
        ForceSensorConfig config = sensorConfigs[data.sensorId];
        float processedValue = data.normalizedValue;
        
        if (enableSmoothing)
        {
            smoothedValues[data.sensorId] = Mathf.Lerp(smoothedValues[data.sensorId], processedValue, config.smoothingFactor);
            processedValue = smoothedValues[data.sensorId];
        }
        
        if (enableExponentialCurve && processedValue > 0f)
        {
            processedValue = Mathf.Pow(processedValue, Mathf.Max(0.1f, Mathf.Min(2.0f, exponentialPower)));
        }
        
        currentSensorData[data.sensorId] = new ForceSensorData(
            data.sensorId, 
            data.rawValue, 
            processedValue, 
            data.timestamp
        );
        
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
    
    public float CalculateModifiedForce(float force)
    {
        if (force <= amplificationStartThreshold)
        {
            return force;
        }
        
        float normalizedPressure = Mathf.InverseLerp(amplificationStartThreshold, 1.0f, force);
        float curveValue = normalizedPressure * normalizedPressure * normalizedPressure;
        float amplificationFactor = Mathf.Lerp(1.0f, maxPressureMultiplier, curveValue);
        
        return force * amplificationFactor;
    }
}
