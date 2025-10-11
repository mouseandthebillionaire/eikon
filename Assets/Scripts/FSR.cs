using UnityEngine;
using System;

public class FSR : MonoBehaviour
{
    [Header("Sensor Configuration")]
    [SerializeField] private int sensorId = 0;
    [Range(0f, 1f)]
    [SerializeField] public float force = 0.1f;
    [SerializeField] public float timeHeld = 0f;
    
    
    [Header("Keyboard Testing")]
    [SerializeField] private KeyCode testKey = KeyCode.Space;
    [SerializeField] private bool isKeyboardPressed = false;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool logValueChanges = false;
    
    // Runtime data
    private ForceSensorData currentData;
    private bool isActive = false;
    private bool wasActive = false;
    private float lastValue = 0f;

    
    // Events
    public static event Action<FSR, ForceSensorData> OnFSRValueChanged;
    public static event Action<FSR, bool> OnFSRActivationChanged;
    
    void Start()
    {
        InitializeFSR();
        SubscribeToEvents();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    void Update()
    {
        if (Controller.S.enableKeyboardTesting)
        {
            HandleKeyboardInput();
        }
        
        UpdateShapeAnimation();
        UpdateTimeHeld();
    }
    
    private void InitializeFSR()
    {
        // FSR initialization - no longer needs to find ShapeScript
        // Shapes will now subscribe to FSR events directly
    }
    
    private void SubscribeToEvents()
    {
        ForceSensorManager.OnSensorValueChanged += OnSensorValueChanged;
        ForceSensorManager.OnSensorActivationChanged += OnSensorActivationChanged;
    }
    
    private void UnsubscribeFromEvents()
    {
        ForceSensorManager.OnSensorValueChanged -= OnSensorValueChanged;
        ForceSensorManager.OnSensorActivationChanged -= OnSensorActivationChanged;
    }
    
    private void OnSensorValueChanged(ForceSensorData sensorData)
    {
        if (sensorData.sensorId == sensorId)
        {
            UpdateSensorData(sensorData);
        }
    }
    
    private void OnSensorActivationChanged(int id, bool active)
    {
        if (id == sensorId)
        {
            UpdateActivationState(active);
        }
    }
    
    private void UpdateSensorData(ForceSensorData newData)
    {
        currentData = newData;
        lastValue = newData.normalizedValue;
        // force = newData.normalizedValue;

        
        OnFSRValueChanged?.Invoke(this, newData);
    }
    
    private void UpdateActivationState(bool active)
    {
        wasActive = isActive;
        isActive = active;
        
        if (wasActive != isActive)
        {
            
            OnFSRActivationChanged?.Invoke(this, isActive);
        }
    }
    
    private void UpdateShapeAnimation()
    {
        // Animation is now handled by shapes listening to FSR events
        // This method is kept for compatibility but does nothing
    }

    private void UpdateTimeHeld()
    {
        if (isActive)
        {
            timeHeld += (force * Time.deltaTime);
        } else {
            if(timeHeld > 0f) {
                timeHeld -= Time.deltaTime;
            }
        }
    }
    
    private void HandleKeyboardInput()
    {
        bool keyPressed = Input.GetKey(testKey);
        
        // Check for key state change
        if (keyPressed != isKeyboardPressed)
        {
            isKeyboardPressed = keyPressed;
            
            if (keyPressed)
            {
                // Simulate sensor activation with full force
                SimulateSensorInput(1.0f);
            }
            else
            {
                // Simulate sensor deactivation
                SimulateSensorInput(0.0f);
            }
        }
    }
    
    private void SimulateSensorInput(float normalizedValue)
    {
        // Create simulated sensor data
        ForceSensorData simulatedData = new ForceSensorData(
            sensorId,
            normalizedValue * 1023f, // Convert to raw value (0-1023)
            normalizedValue,         // Normalized value (0-1)
            Time.time
        );
        
        // Update sensor data directly
        UpdateSensorData(simulatedData);
        
        // Update activation state based on threshold
        ForceSensorConfig config = GetSensorConfig();
        bool shouldBeActive = normalizedValue > config.activationThreshold;
        UpdateActivationState(shouldBeActive);
    }
    
    private ForceSensorConfig GetSensorConfig()
    {
        // Try to get config from ForceSensorManager if available
        ForceSensorManager manager = FindObjectOfType<ForceSensorManager>();
        if (manager != null)
        {
            return manager.GetSensorConfig(sensorId);
        }
        
        // Return default config if manager not found
        return new ForceSensorConfig
        {
            sensorId = sensorId,
            activationThreshold = 0.1f,
            deactivationThreshold = 0.05f
        };
    }
    
    // Public API
    public ForceSensorData GetCurrentData()
    {
        return currentData;
    }
    
    public float GetCurrentValue()
    {
        return currentData.normalizedValue;
    }
    
    public float GetRawValue()
    {
        return currentData.rawValue;
    }
    
    public bool IsActive()
    {
        return isActive;
    }
    
    public void SetScaleAnimation(bool enabled)
    {
        // This method is kept for compatibility but now does nothing
        // Scale animation is controlled directly on ShapeScript components
        Debug.LogWarning("SetScaleAnimation called on FSR. This should now be called directly on ShapeScript components.");
    }
    
    public void SetRotationAnimation(bool enabled)
    {
        // This method is kept for compatibility but now does nothing
        // Rotation animation is controlled directly on ShapeScript components
        Debug.LogWarning("SetRotationAnimation called on FSR. This should now be called directly on ShapeScript components.");
    }
    
    public void SetKeyboardTesting(bool enabled)
    {
        Controller.S.enableKeyboardTesting = enabled;
    }
    
    public bool IsKeyboardTestingEnabled()
    {
        return Controller.S.enableKeyboardTesting;
    }
    
    public KeyCode GetTestKey()
    {
        return testKey;
    }
    
    [ContextMenu("Test Activation")]
    public void TestActivation()
    {
        UpdateActivationState(!isActive);
    }
    
    [ContextMenu("Test Value Change")]
    public void TestValueChange()
    {
        float testValue = UnityEngine.Random.Range(0f, 1f);
        ForceSensorData testData = new ForceSensorData(sensorId, testValue * 1023f, testValue, Time.time);
        UpdateSensorData(testData);
    }
    
    [ContextMenu("Toggle Keyboard Testing")]
    public void ToggleKeyboardTesting()
    {
        SetKeyboardTesting(!Controller.S.enableKeyboardTesting);
    }
    
    [ContextMenu("Simulate Key Press")]
    public void SimulateKeyPress()
    {
        if (Controller.S.enableKeyboardTesting)
        {
            SimulateSensorInput(1.0f);
        }
        else
        {
            Debug.LogWarning("Keyboard testing is disabled. Enable it first to simulate key press.");
        }
    }
    
    [ContextMenu("Simulate Key Release")]
    public void SimulateKeyRelease()
    {
        if (Controller.S.enableKeyboardTesting)
        {
            SimulateSensorInput(0.0f);
        }
        else
        {
            Debug.LogWarning("Keyboard testing is disabled. Enable it first to simulate key release.");
        }
    }
}
