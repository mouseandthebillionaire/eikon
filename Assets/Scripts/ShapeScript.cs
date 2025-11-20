using UnityEngine;
using System.Collections;

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
    [SerializeField] private float rotationRate = 90f;
    
    private SpriteRenderer sr;
    private Sprite[] frames;
    public float frameRate = 10f;

    private Vector3 targetScale;
    private Vector3 targetRotation;
    private float accumulatedRotation = 0f;
    private float accumulationTime = 0f;
    private float returnStartRotation = 0f;
    private float returnTimeRemaining = 0f;
    
    private float scaleAccumulationTime = 0f;
    private Vector3 scaleReturnStart = Vector3.one;
    private float scaleReturnTimeRemaining = 0f;
    
    private bool isActive = false;
    private float currentIntensity = 0f;
    
    private bool enableAlphaControl = true;
    private float inactiveAlpha = 0.1f;
    private float activeAlpha = 0.9f;
    private float currentHoldThreshold = 0.5f;
    private float alphaTransitionSpeed = 5f;
    private float targetAlpha;
    private bool isAlphaActivated = false;
    
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
        UpdateAlphaControl();
    }
    
    private void InitializeShape()
    {
        targetScale = baseScale;
        transform.localScale = baseScale;
        baseRotation = transform.localEulerAngles;
        targetRotation = baseRotation;

        sr = GetComponent<SpriteRenderer>();
        frames = Resources.LoadAll<Sprite>("Circles");
        StartCoroutine(ShapeAnimation());
        
        if (enableAlphaControl && sr != null)
        {
            targetAlpha = inactiveAlpha;
            Color currentColor = sr.color;
            currentColor.a = targetAlpha;
            sr.color = currentColor;
        }
    }
    
    private void AssignFSR()
    {
        if (assignedFSR == null && autoFindFSR)
        {
            FSR[] fsrs = FindObjectsOfType<FSR>();
            if (fsrs.Length > 0)
            {
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
            currentIntensity = fsr.modifiedForce;
        }
    }
    
    private void OnFSRActivationChanged(FSR fsr, bool active)
    {
        if (fsr == assignedFSR)
        {
            isActive = active;
        }
    }
    
    public void AssignFSR(FSR fsr)
    {
        UnsubscribeFromFSREvents();
        assignedFSR = fsr;
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

    private IEnumerator ShapeAnimation()
    {
        int currentFrame = Random.Range(0, frames.Length);
        sr.sprite = frames[currentFrame];
        yield return new WaitForSeconds(1f / frameRate);
        StartCoroutine(ShapeAnimation());
    }
    
    private void UpdateScaleAnimation()
    {
        if (!enableScaleAnimation) return;
        
        if (isActive)
        {
            float maxExpectedForce = 3.5f;
            float normalizedIntensity = Mathf.Clamp01(currentIntensity / maxExpectedForce);
            normalizedIntensity = Mathf.Pow(normalizedIntensity, 0.7f);
            
            targetScale = Vector3.Lerp(baseScale, maxScale, normalizedIntensity);
            scaleAccumulationTime += Time.deltaTime;
            scaleReturnTimeRemaining = 0f;
            
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);
        }
        else
        {
            if (scaleAccumulationTime > 0f && scaleReturnTimeRemaining <= 0f)
            {
                scaleReturnStart = transform.localScale;
                scaleReturnTimeRemaining = scaleAccumulationTime;
            }
            
            if (scaleReturnTimeRemaining > 0f)
            {
                scaleReturnTimeRemaining -= Time.deltaTime;
                
                if (scaleReturnTimeRemaining <= 0f)
                {
                    transform.localScale = baseScale;
                    scaleAccumulationTime = 0f;
                }
                else
                {
                    float progress = 1f - (scaleReturnTimeRemaining / scaleAccumulationTime);
                    transform.localScale = Vector3.Lerp(scaleReturnStart, baseScale, progress);
                }
            }
            else
            {
                transform.localScale = baseScale;
            }
        }
    }
    
    private void UpdateRotationAnimation()
    {
        if (!enableRotationAnimation) return;
        
        if (isActive)
        {
            accumulatedRotation += rotationRate * currentIntensity * Time.deltaTime;
            accumulationTime += Time.deltaTime;
            returnTimeRemaining = 0f;
        }
        else
        {
            if (accumulatedRotation > 0f && returnTimeRemaining <= 0f)
            {
                returnStartRotation = accumulatedRotation;
                returnTimeRemaining = accumulationTime;
            }
            
            if (returnTimeRemaining > 0f)
            {
                returnTimeRemaining -= Time.deltaTime;
                
                if (returnTimeRemaining <= 0f)
                {
                    accumulatedRotation = 0f;
                    accumulationTime = 0f;
                }
                else
                {
                    float progress = 1f - (returnTimeRemaining / accumulationTime);
                    accumulatedRotation = returnStartRotation * (1f - progress);
                }
            }
        }
        
        targetRotation = baseRotation + new Vector3(0f, 0f, accumulatedRotation);
        transform.localEulerAngles = targetRotation;
    }
    
    private void UpdateAlphaControl()
    {
        if (!enableAlphaControl || sr == null || assignedFSR == null) return;
        
        bool shouldBeActivated = assignedFSR.currentHoldTime > currentHoldThreshold;
        
        if (shouldBeActivated && !isAlphaActivated)
        {
            targetAlpha = activeAlpha;
            isAlphaActivated = true;
        }
        else if (!shouldBeActivated && isAlphaActivated)
        {
            targetAlpha = inactiveAlpha;
            isAlphaActivated = false;
        }
        
        Color currentColor = sr.color;
        currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * alphaTransitionSpeed);
        sr.color = currentColor;
    }
    
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
    
    public void SetAlphaSettings(bool enabled, float inactive, float active, float threshold, float speed)
    {
        enableAlphaControl = enabled;
        inactiveAlpha = Mathf.Clamp01(inactive);
        activeAlpha = Mathf.Clamp01(active);
        currentHoldThreshold = threshold;
        alphaTransitionSpeed = speed;
        
        if (!enabled && sr != null)
        {
            Color currentColor = sr.color;
            currentColor.a = 1f;
            sr.color = currentColor;
        }
        else if (enabled && sr != null)
        {
            targetAlpha = isAlphaActivated ? activeAlpha : inactiveAlpha;
        }
    }
}
