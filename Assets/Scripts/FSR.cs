using UnityEngine;
using System;

public class FSR : MonoBehaviour
{
    [Header("Sensor Configuration")]
    [SerializeField] private int sensorId = 0;
    [SerializeField] public float force = 0.1f;
    [SerializeField] public float modifiedForce = 0.1f;
    public float timeHeld = 0f;
    public float currentHoldTime = 0f;
    
    [Header("Keyboard Testing")]
    [SerializeField] private KeyCode testKey = KeyCode.Space;
    [SerializeField] private bool isKeyboardPressed = false;
    
    private ForceSensorData currentData;
    private bool isActive = false;
    private bool wasActive = false;
    private float lastValue = 0f;
    
    public static event Action<FSR, ForceSensorData> OnFSRValueChanged;
    public static event Action<FSR, bool> OnFSRActivationChanged;
    
    void Start()
    {
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
        
        UpdateTimeHeld();
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
        force = newData.normalizedValue;
        
        ForceSensorManager manager = FindObjectOfType<ForceSensorManager>();
        if (manager != null)
        {
            modifiedForce = manager.CalculateModifiedForce(force);
        }
        else
        {
            modifiedForce = force;
        }
        
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

    private void UpdateTimeHeld()
    {
        if (isActive)
        {
            timeHeld += (modifiedForce * Time.deltaTime);
            currentHoldTime += (modifiedForce * Time.deltaTime);
        }
        else
        {
            if (timeHeld > 0f)
            {
                timeHeld -= Time.deltaTime;
            }
            currentHoldTime = 0f;
        }
    }
    
    private void HandleKeyboardInput()
    {
        bool keyPressed = Input.GetKey(testKey);
        
        if (keyPressed != isKeyboardPressed)
        {
            isKeyboardPressed = keyPressed;
            
            if (keyPressed)
            {
                SimulateSensorInput(1.0f);
            }
            else
            {
                SimulateSensorInput(0.0f);
            }
        }
    }
    
    private void SimulateSensorInput(float normalizedValue)
    {
        ForceSensorData simulatedData = new ForceSensorData(
            sensorId,
            normalizedValue * 1023f,
            normalizedValue,
            Time.time
        );
        
        UpdateSensorData(simulatedData);
        
        ForceSensorConfig config = GetSensorConfig();
        bool shouldBeActive = normalizedValue > config.activationThreshold;
        UpdateActivationState(shouldBeActive);
    }
    
    private ForceSensorConfig GetSensorConfig()
    {
        ForceSensorManager manager = FindObjectOfType<ForceSensorManager>();
        if (manager != null)
        {
            return manager.GetSensorConfig(sensorId);
        }
        
        return new ForceSensorConfig
        {
            sensorId = sensorId,
            activationThreshold = 0.1f,
            deactivationThreshold = 0.05f
        };
    }
    
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
    
    public void SetSensorId(int newSensorId)
    {
        sensorId = newSensorId;
    }
    
    public int GetSensorId()
    {
        return sensorId;
    }
    
    public void ResetTimeHeld()
    {
        currentHoldTime = 0f;
    }
    
    public static void ResetAllTimeHeld()
    {
        FSR[] allFSRs = FindObjectsOfType<FSR>();
        foreach (FSR fsr in allFSRs)
        {
            fsr.ResetTimeHeld();
        }
    }
    
    public void UpdateSensorDataDirect(ForceSensorData newData)
    {
        currentData = newData;
        lastValue = newData.normalizedValue;
        force = newData.normalizedValue;
        
        ForceSensorManager manager = FindObjectOfType<ForceSensorManager>();
        if (manager != null)
        {
            modifiedForce = manager.CalculateModifiedForce(force);
        }
        else
        {
            modifiedForce = force;
        }
        
        ForceSensorConfig config = GetSensorConfig();
        bool shouldBeActive = force > config.activationThreshold;
        UpdateActivationState(shouldBeActive);
        
        OnFSRValueChanged?.Invoke(this, newData);
    }
}
