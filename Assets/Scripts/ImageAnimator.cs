using UnityEngine;
using System.Collections;

public class ImageAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public string spriteSheetFolder = "wickSprites";
    public string spriteSheetName = "layer_0";
    public float frameRate = 10f;
    public bool loop = true;

    [Header("FSR Rotation Settings")]
    [SerializeField] private int fsrSensorId = 0; // Which FSR sensor to use (0-5)
    public float returnToZeroTime = 180f; // TimeHeld value when rotation reaches 0 (in Seconds)
    private float rotationSmoothing = 5f; // How smoothly rotation follows FSR
    private bool invertRotation = false; // Reverse rotation direction
    
    private Sprite[] frames;
    
    private int currentFrame = 0;
    private int direction = 1; // 1 for forward, -1 for backward
    private float timer = 0f;
    
    // FSR rotation
    private float targetRotation = 0f;
    private float currentRotation = 0f;
    private float initialRandomRotation = 0f;
    private bool hasStartedReturning = false;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get miliseconds from Seconds
        returnToZeroTime = returnToZeroTime * 1000f;
        
        frames = Resources.LoadAll<Sprite>(spriteSheetFolder + "/" + spriteSheetName);
        
        if (frames.Length == 0)
        {
            Debug.LogError("No frames found in the Resources folder");
            return;
        }

        // Set initial random rotation
        initialRandomRotation = Random.Range(0, 360);
        currentRotation = initialRandomRotation;
        targetRotation = initialRandomRotation;
        transform.rotation = Quaternion.Euler(0, 0, currentRotation);
        
        StartCoroutine(Play());
    }

    void Update()
    {
        UpdateFSRRotation();
    }

    private void UpdateFSRRotation()
    {
        // Get FSR component directly to access timeHeld value
        FSR[] fsrComponents = FindObjectsOfType<FSR>();
        FSR targetFSR = null;
        
        // Debug: Show all FSR components found
        if (Time.frameCount % 60 == 0) // Every second
        {
            Debug.Log($"Found {fsrComponents.Length} FSR components. Looking for sensor ID: {fsrSensorId}");
        }
        
        // Find the FSR component with matching sensor ID
        foreach (FSR fsr in fsrComponents)
        {
            // Check if this FSR has the matching sensor ID
            // We need to access the sensorId field directly since GetCurrentData() might not be initialized
            var fsrType = fsr.GetType();
            var sensorIdField = fsrType.GetField("sensorId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sensorIdField != null)
            {
                int fsrSensorId = (int)sensorIdField.GetValue(fsr);
                if (Time.frameCount % 60 == 0) // Every second
                {
                    Debug.Log($"FSR component has sensorId: {fsrSensorId}, timeHeld: {fsr.timeHeld}");
                }
                if (fsrSensorId == this.fsrSensorId)
                {
                    targetFSR = fsr;
                    break;
                }
            }
        }
        
        if (targetFSR != null)
        {
            // Get the timeHeld value from the FSR component
            float timeHeld = targetFSR.timeHeld;
            
            // Calculate progress towards return to zero (0 to 1)
            float progress = Mathf.Clamp01(timeHeld / returnToZeroTime);
            
            // Interpolate from initial random rotation to 0 degrees
            float targetRotationFromZero = Mathf.LerpAngle(initialRandomRotation, 0f, progress);
            
            if (invertRotation)
            {
                targetRotationFromZero = -targetRotationFromZero;
            }
            
            targetRotation = targetRotationFromZero;
            
            // Debug rotation info every 10 frames to reduce spam
            if (Time.frameCount % 10 == 0)
            {
                Debug.Log($"timeHeld: {timeHeld:F1}, Progress: {progress:F3}, Target: {targetRotationFromZero:F1}, Current: {currentRotation:F1}");
            }
            
            // Smoothly interpolate to target rotation
            currentRotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * rotationSmoothing);
            
            // Apply rotation
            transform.rotation = Quaternion.Euler(0, 0, currentRotation);
        }
        else
        {
            Debug.LogWarning($"No FSR component found with sensor ID {fsrSensorId}");
        }
    }

    private IEnumerator Play()
    {
        GetComponent<SpriteRenderer>().sprite = frames[currentFrame];
        
        // Ping-pong logic
        currentFrame += direction;
        
        // Check if we've reached the end and need to reverse
        if (currentFrame >= frames.Length - 1)
        {
            direction = -1; // Start going backward
        }
        else if (currentFrame <= 0)
        {
            direction = 1; // Start going forward
        }

        yield return new WaitForSeconds(1f / frameRate);

        StartCoroutine(Play());
    }

    public void SetFSRSensorId(int sensorId)
    {
        fsrSensorId = Mathf.Clamp(sensorId, 0, 5);
    }

    public void SetReturnToZeroTime(float timeInSeconds)
    {
        returnToZeroTime = timeInSeconds;
    }

    public void SetRotationSmoothing(float smoothing)
    {
        rotationSmoothing = smoothing;
    }

    public void SetInvertRotation(bool invert)
    {
        invertRotation = invert;
    }

    // Get current timeHeld value for debugging
    public float GetCurrentTimeHeld()
    {
        FSR[] fsrComponents = FindObjectsOfType<FSR>();
        foreach (FSR fsr in fsrComponents)
        {
            if (fsr.GetCurrentData().sensorId == fsrSensorId)
            {
                return fsr.timeHeld;
            }
        }
        return 0f;
    }

    // Reset to a new random rotation
    public void ResetToRandomRotation()
    {
        initialRandomRotation = Random.Range(0, 360);
        currentRotation = initialRandomRotation;
        targetRotation = initialRandomRotation;
        transform.rotation = Quaternion.Euler(0, 0, currentRotation);
    }

    // Get the progress towards zero rotation (0 to 1)
    public float GetReturnToZeroProgress()
    {
        FSR[] fsrComponents = FindObjectsOfType<FSR>();
        foreach (FSR fsr in fsrComponents)
        {
            if (fsr.GetCurrentData().sensorId == fsrSensorId)
            {
                return Mathf.Clamp01(fsr.timeHeld / returnToZeroTime);
            }
        }
        return 0f;
    }

    // Test method to manually set rotation for debugging
    [ContextMenu("Test Rotation")]
    public void TestRotation()
    {
        float testProgress = 0.5f; // 50% progress
        float testRotation = Mathf.LerpAngle(initialRandomRotation, 0f, testProgress);
        transform.rotation = Quaternion.Euler(0, 0, testRotation);
        Debug.Log($"Test rotation set to: {testRotation:F1} degrees (50% progress)");
    }
}
