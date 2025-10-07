using UnityEngine;
using System.Collections.Generic;

public class Controller : MonoBehaviour
{
    [Header("System Components")]
    [SerializeField] private ArduinoCommunication arduinoCommunication;
    [SerializeField] private ForceSensorManager sensorManager;
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogging = true;
    [SerializeField] public bool enableKeyboardTesting = false;
    [SerializeField] private bool showSensorValuesInConsole = false;
    
    [Header("System Status")]
    [SerializeField] private bool isSystemInitialized = false;
    [SerializeField] private bool isArduinoConnected = false;
    
    // Runtime data
    private ForceSensorData[] lastSensorData = new ForceSensorData[6];
    private Dictionary<int, float> sensorChangeThresholds = new Dictionary<int, float>();

    public static Controller S;
    
    void Awake()
    {
        S = this;
    }
    
    void Start()
    {
        InitializeSystem();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    private void InitializeSystem()
    {
        // Auto-find components if not assigned
        if (arduinoCommunication == null)
        {
            arduinoCommunication = FindObjectOfType<ArduinoCommunication>();
        }
        
        if (sensorManager == null)
        {
            sensorManager = FindObjectOfType<ForceSensorManager>();
        }
        
        // Initialize change thresholds for each sensor
        for (int i = 0; i < 6; i++)
        {
            sensorChangeThresholds[i] = 0.01f; // 1% change threshold
        }
        
        SubscribeToEvents();
        isSystemInitialized = true;
        
        if (enableDebugLogging)
        {
            Debug.Log("Force Sensor System initialized successfully");
        }
    }
    
    private void SubscribeToEvents()
    {
        if (sensorManager != null)
        {
            ForceSensorManager.OnSensorValueChanged += OnSensorValueChanged;
            ForceSensorManager.OnSensorActivationChanged += OnSensorActivationChanged;
            ForceSensorManager.OnAllSensorsUpdated += OnAllSensorsUpdated;
        }
        
        if (arduinoCommunication != null)
        {
            ArduinoCommunication.OnConnectionStatusChanged += OnArduinoConnectionChanged;
            ArduinoCommunication.OnErrorOccurred += OnArduinoError;
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        if (sensorManager != null)
        {
            ForceSensorManager.OnSensorValueChanged -= OnSensorValueChanged;
            ForceSensorManager.OnSensorActivationChanged -= OnSensorActivationChanged;
            ForceSensorManager.OnAllSensorsUpdated -= OnAllSensorsUpdated;
        }
        
        if (arduinoCommunication != null)
        {
            ArduinoCommunication.OnConnectionStatusChanged -= OnArduinoConnectionChanged;
            ArduinoCommunication.OnErrorOccurred -= OnArduinoError;
        }
    }
    
    private void OnSensorValueChanged(ForceSensorData sensorData)
    {
        if (!isSystemInitialized) return;
        
        int sensorId = sensorData.sensorId;
        
        // Check if value changed significantly
        if (HasSignificantChange(sensorId, sensorData.normalizedValue))
        {
            lastSensorData[sensorId] = sensorData;
            
            if (enableDebugLogging)
            {
                Debug.Log($"Sensor {sensorId} value changed: {sensorData.normalizedValue:F3}");
            }
            
            // Handle sensor-specific logic here
            HandleSensorValueChange(sensorId, sensorData);
        }
    }
    
    private void OnSensorActivationChanged(int sensorId, bool isActive)
    {
        if (!isSystemInitialized) return;
        
        if (enableDebugLogging)
        {
            Debug.Log($"Sensor {sensorId} {(isActive ? "activated" : "deactivated")}");
        }
        
        // Handle sensor activation/deactivation logic here
        HandleSensorActivationChange(sensorId, isActive);
    }
    
    private void OnAllSensorsUpdated(ForceSensorData[] allSensorData)
    {
        if (!isSystemInitialized) return;
        
        if (showSensorValuesInConsole)
        {
            string sensorValues = "Sensor Values: ";
            for (int i = 0; i < allSensorData.Length; i++)
            {
                sensorValues += $"S{i}:{allSensorData[i].normalizedValue:F2} ";
            }
            Debug.Log(sensorValues);
        }
        
        // Handle global sensor update logic here
        HandleAllSensorsUpdate(allSensorData);
    }
    
    private void OnArduinoConnectionChanged(bool connected)
    {
        isArduinoConnected = connected;
        
        if (enableDebugLogging)
        {
            Debug.Log($"Arduino connection status: {(connected ? "Connected" : "Disconnected")}");
        }
        
        // Handle connection status change
        HandleArduinoConnectionChange(connected);
    }
    
    private void OnArduinoError(string error)
    {
        if (enableDebugLogging)
        {
            Debug.LogError($"Arduino Error: {error}");
        }
        
        // Handle Arduino errors
        HandleArduinoError(error);
    }
    
    private bool HasSignificantChange(int sensorId, float newValue)
    {
        if (sensorId >= 6) return false;
        
        float lastValue = lastSensorData[sensorId].normalizedValue;
        float threshold = sensorChangeThresholds[sensorId];
        
        return Mathf.Abs(newValue - lastValue) > threshold;
    }
    
    // Override these methods in derived classes or use them directly
    protected virtual void HandleSensorValueChange(int sensorId, ForceSensorData sensorData)
    {
        // Override this method to implement custom sensor value handling
        // Example: Trigger animations, update UI, control game objects, etc.
    }
    
    protected virtual void HandleSensorActivationChange(int sensorId, bool isActive)
    {
        // Override this method to implement custom activation handling
        // Example: Play sounds, show visual feedback, etc.
    }
    
    protected virtual void HandleAllSensorsUpdate(ForceSensorData[] allSensorData)
    {
        // Override this method to implement custom global update handling
        // Example: Calculate total force, update combined UI elements, etc.
    }
    
    protected virtual void HandleArduinoConnectionChange(bool connected)
    {
        // Override this method to implement custom connection handling
        // Example: Show connection status UI, pause/resume game logic, etc.
    }
    
    protected virtual void HandleArduinoError(string error)
    {
        // Override this method to implement custom error handling
        // Example: Show error messages, attempt reconnection, etc.
    }
    
    // Public API methods
    public ForceSensorData GetSensorData(int sensorId)
    {
        if (sensorManager != null && sensorId >= 0 && sensorId < 6)
        {
            return sensorManager.GetSensorData(sensorId);
        }
        return new ForceSensorData();
    }
    
    public ForceSensorData[] GetAllSensorData()
    {
        if (sensorManager != null)
        {
            return sensorManager.GetAllSensorData();
        }
        return new ForceSensorData[6];
    }
    
    public bool IsSensorActive(int sensorId)
    {
        if (sensorManager != null && sensorId >= 0 && sensorId < 6)
        {
            return sensorManager.IsSensorActive(sensorId);
        }
        return false;
    }
    
    public bool IsArduinoConnected()
    {
        return isArduinoConnected && arduinoCommunication != null && arduinoCommunication.IsArduinoConnected;
    }
    
    public void SetSensorChangeThreshold(int sensorId, float threshold)
    {
        if (sensorId >= 0 && sensorId < 6)
        {
            sensorChangeThresholds[sensorId] = Mathf.Clamp01(threshold);
        }
    }
    
    public void SendArduinoCommand(string command)
    {
        if (arduinoCommunication != null)
        {
            arduinoCommunication.SendCommand(command);
        }
    }
    
    // Debug methods
    [ContextMenu("Print System Status")]
    public void PrintSystemStatus()
    {
        Debug.Log($"System Initialized: {isSystemInitialized}");
        Debug.Log($"Arduino Connected: {isArduinoConnected}");
        Debug.Log($"Sensor Manager: {(sensorManager != null ? "Found" : "Missing")}");
        Debug.Log($"Arduino Communication: {(arduinoCommunication != null ? "Found" : "Missing")}");
    }
    
    [ContextMenu("Print All Sensor Data")]
    public void PrintAllSensorData()
    {
        ForceSensorData[] data = GetAllSensorData();
        for (int i = 0; i < data.Length; i++)
        {
            Debug.Log(data[i].ToString());
        }
    }
}
