using UnityEngine;

public class ShapeScript : MonoBehaviour
{
    [Header("FSR Assignment")]
    [SerializeField] private FSR assignedFSR;
    [SerializeField] private bool autoFindFSR = true;
    
    [Header("Scale Animation")]
    [SerializeField] private bool enableScaleAnimation = true;
    [SerializeField] private Vector3 baseScale = Vector3.one;
    [SerializeField] private Vector3 maxScale = Vector3.one * 1.2f;
    [SerializeField] private float animationSpeed = 5f;
    
    [Header("Rotation Animation")]
    [SerializeField] private bool enableRotationAnimation = true;
    [SerializeField] private Vector3 baseRotation;
    [SerializeField] private float rotationRate = 90f; // degrees per second
    
    // Runtime animation data
    private Vector3 targetScale;
    private Vector3 targetRotation;
    private float accumulatedRotation = 0f;
    private float accumulationTime = 0f;
    private float returnStartRotation = 0f;
    private float returnTimeRemaining = 0f;
    
    // Scale animation tracking
    private float scaleAccumulationTime = 0f;
    private Vector3 scaleReturnStart = Vector3.one;
    private float scaleReturnTimeRemaining = 0f;
    
    // Current state
    private bool isActive = false;
    private float currentIntensity = 0f;
    
    void Start()
    {
        InitializeShape();
        AssignFSR();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromFSREvents();
    }
    
    void Update()
    {
        UpdateScaleAnimation();
        UpdateRotationAnimation();
    }
    
    private void InitializeShape()
    {
        // Initialize scale
        targetScale = baseScale;
        transform.localScale = baseScale;
        
        // Initialize rotation - capture current transform rotation as base
        baseRotation = transform.localEulerAngles;
        targetRotation = baseRotation;
    }
    
    private void AssignFSR()
    {
        if (assignedFSR == null && autoFindFSR)
        {
            // Try to find an FSR in the scene
            FSR[] fsrs = FindObjectsOfType<FSR>();
            if (fsrs.Length > 0)
            {
                // For now, assign the first available FSR
                // Later we can implement random assignment
                assignedFSR = fsrs[0];
            }
        }
        
        if (assignedFSR != null)
        {
            SubscribeToFSREvents();
        }
    }
    
    private void SubscribeToFSREvents()
    {
        if (assignedFSR != null)
        {
            FSR.OnFSRValueChanged += OnFSRValueChanged;
            FSR.OnFSRActivationChanged += OnFSRActivationChanged;
        }
    }
    
    private void UnsubscribeFromFSREvents()
    {
        FSR.OnFSRValueChanged -= OnFSRValueChanged;
        FSR.OnFSRActivationChanged -= OnFSRActivationChanged;
    }
    
    private void OnFSRValueChanged(FSR fsr, ForceSensorData sensorData)
    {
        if (fsr == assignedFSR)
        {
            currentIntensity = sensorData.normalizedValue;
        }
    }
    
    private void OnFSRActivationChanged(FSR fsr, bool active)
    {
        if (fsr == assignedFSR)
        {
            isActive = active;
        }
    }
    
    // FSR Assignment Methods
    public void AssignFSR(FSR fsr)
    {
        // Unsubscribe from current FSR if any
        UnsubscribeFromFSREvents();
        
        // Assign new FSR
        assignedFSR = fsr;
        
        // Subscribe to new FSR events
        if (assignedFSR != null)
        {
            SubscribeToFSREvents();
        }
    }
    
    public FSR GetAssignedFSR()
    {
        return assignedFSR;
    }
    
    public void AssignRandomFSR()
    {
        FSR[] fsrs = FindObjectsOfType<FSR>();
        if (fsrs.Length > 0)
        {
            int randomIndex = Random.Range(0, fsrs.Length);
            AssignFSR(fsrs[randomIndex]);
        }
    }
    
    private void UpdateScaleAnimation()
    {
        if (!enableScaleAnimation) return;
        
        if (isActive)
        {
            targetScale = Vector3.Lerp(baseScale, maxScale, currentIntensity);
            scaleAccumulationTime += Time.deltaTime;
            
            // Reset scale return state when becoming active
            scaleReturnTimeRemaining = 0f;
            
            // Apply scale normally during active state
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
        }
        else
        {
            // Start scale return animation if we have accumulated scale and haven't started returning yet
            if (scaleAccumulationTime > 0f && scaleReturnTimeRemaining <= 0f)
            {
                scaleReturnStart = transform.localScale;
                scaleReturnTimeRemaining = scaleAccumulationTime; // Use the same time it took to accumulate
            }
            
            // Perform linear scale return animation
            if (scaleReturnTimeRemaining > 0f)
            {
                scaleReturnTimeRemaining -= Time.deltaTime;
                
                if (scaleReturnTimeRemaining <= 0f)
                {
                    // Return complete - set directly to base scale
                    transform.localScale = baseScale;
                    scaleAccumulationTime = 0f;
                }
                else
                {
                    // Linear interpolation from start to base scale over the remaining time
                    float progress = 1f - (scaleReturnTimeRemaining / scaleAccumulationTime);
                    transform.localScale = Vector3.Lerp(scaleReturnStart, baseScale, progress);
                }
            }
            else
            {
                // No return animation needed, just set to base scale
                transform.localScale = baseScale;
            }
        }
    }
    
    private void UpdateRotationAnimation()
    {
        if (!enableRotationAnimation) return;
        
        if (isActive)
        {
            // Continuously add rotation based on force intensity
            accumulatedRotation += rotationRate * currentIntensity * Time.deltaTime;
            accumulationTime += Time.deltaTime;
            
            // Reset return state when becoming active
            returnTimeRemaining = 0f;
        }
        else
        {
            // Start return animation if we have accumulated rotation and haven't started returning yet
            if (accumulatedRotation > 0f && returnTimeRemaining <= 0f)
            {
                returnStartRotation = accumulatedRotation;
                returnTimeRemaining = accumulationTime; // Use the same time it took to accumulate
            }
            
            // Perform linear return animation
            if (returnTimeRemaining > 0f)
            {
                returnTimeRemaining -= Time.deltaTime;
                
                if (returnTimeRemaining <= 0f)
                {
                    // Return complete
                    accumulatedRotation = 0f;
                    accumulationTime = 0f;
                }
                else
                {
                    // Linear interpolation from start to zero over the remaining time
                    float progress = 1f - (returnTimeRemaining / accumulationTime);
                    accumulatedRotation = returnStartRotation * (1f - progress);
                }
            }
        }
        
        // Apply the accumulated rotation to the base rotation
        targetRotation = baseRotation + new Vector3(0f, 0f, accumulatedRotation);
        transform.localEulerAngles = targetRotation;
    }
    
    // Public API for external control
    public void SetScaleAnimation(bool enabled)
    {
        enableScaleAnimation = enabled;
        if (!enabled)
        {
            transform.localScale = baseScale;
        }
    }
    
    public void SetRotationAnimation(bool enabled)
    {
        enableRotationAnimation = enabled;
        if (!enabled)
        {
            transform.localEulerAngles = baseRotation;
        }
    }
    
    public void SetBaseScale(Vector3 scale)
    {
        baseScale = scale;
        if (!isActive)
        {
            transform.localScale = baseScale;
        }
    }
    
    public void SetMaxScale(Vector3 scale)
    {
        maxScale = scale;
    }
    
    public void SetBaseRotation(Vector3 rotation)
    {
        baseRotation = rotation;
        if (!isActive)
        {
            transform.localEulerAngles = baseRotation;
        }
    }
    
    public void SetRotationRate(float rate)
    {
        rotationRate = rate;
    }
    
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }
}
