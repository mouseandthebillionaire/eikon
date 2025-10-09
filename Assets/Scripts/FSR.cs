using UnityEngine;
using System;

public class FSR : MonoBehaviour
{
    [Header("Sensor Configuration")]
    [SerializeField] private int sensorId = 0;
    [Range(0f, 1f)]
    [SerializeField] public float force = 0.1f;
    [SerializeField] public float timeHeld = 0f;
    
    [Header("Animation")]
    [SerializeField] private bool enableScaleAnimation = true;
    [SerializeField] private Vector3 baseScale = Vector3.one;
    [SerializeField] private Vector3 maxScale = Vector3.one * 1.2f;
    [SerializeField] private float animationSpeed = 5f;
    
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

    private Vector3 targetScale;
    
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
        
        UpdateScaleAnimation();
        UpdateTimeHeld();
    }
    
    private void InitializeFSR()
    {
        
        // Initialize scale
        targetScale = baseScale;
        transform.localScale = baseScale;
    
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
    
    private void UpdateScaleAnimation()
    {
        if (!enableScaleAnimation) return;
        
        if (isActive)
        {
            float intensity = currentData.normalizedValue;
            targetScale = Vector3.Lerp(baseScale, maxScale, intensity);
        }
        else
        {
            targetScale = baseScale;
        }
        
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
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
        enableScaleAnimation = enabled;
        if (!enabled)
        {
            transform.localScale = baseScale;
        }
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
    
    // Gizmos for editor visualization
    void OnDrawGizmos()
    {
        if (showDebugInfo)
        {
            // Main sensor indicator
            Gizmos.color = isActive ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            
            // Draw value as height
            if (isActive)
            {
                Gizmos.color = Color.yellow;
                Vector3 valueHeight = transform.position + Vector3.up * currentData.normalizedValue;
                Gizmos.DrawLine(transform.position, valueHeight);
            }
            
            // Keyboard testing indicator
            if (Controller.S.enableKeyboardTesting)
            {
                Gizmos.color = isKeyboardPressed ? Color.cyan : Color.blue;
                Gizmos.DrawWireCube(transform.position + Vector3.right * 1.5f, Vector3.one * 0.3f);
            }
        }
    }
}
