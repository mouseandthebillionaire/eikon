using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]

    // A collection of variables from combined holdTimes of certain FSRs
    public float[] timeVariables = new float[9];
    public float triggerTime = 10f;
    
    [Header("FSR Current Hold Times")]
    // Public currentHoldTime variables for each FSR (0-5)
    public float fsr0CurrentHoldTime = 0f;
    public float fsr1CurrentHoldTime = 0f;
    public float fsr2CurrentHoldTime = 0f;
    public float fsr3CurrentHoldTime = 0f;
    public float fsr4CurrentHoldTime = 0f;
    public float fsr5CurrentHoldTime = 0f;

    public bool fsrsTriggered = false;
    public float globalHoldTime = 0f;
    public float noFSRTime = 0f;
    public float resetThreshold = 15f;

    public static GameManager S;
    
    void Awake(){
        S = this;
    }

    void Start(){
        Cursor.visible = false; 
    }

    // Update is called once per frame
    void Update()
    {
        // Get all FSR components
        FSR[] fsrComponents = FindObjectsOfType<FSR>();
        
        // Update public currentHoldTime variables for each FSR
        if (fsrComponents.Length > 0) fsr0CurrentHoldTime = fsrComponents[0].currentHoldTime;
        if (fsrComponents.Length > 1) fsr1CurrentHoldTime = fsrComponents[1].currentHoldTime;
        if (fsrComponents.Length > 2) fsr2CurrentHoldTime = fsrComponents[2].currentHoldTime;
        if (fsrComponents.Length > 3) fsr3CurrentHoldTime = fsrComponents[3].currentHoldTime;
        if (fsrComponents.Length > 4) fsr4CurrentHoldTime = fsrComponents[4].currentHoldTime;
        if (fsrComponents.Length > 5) fsr5CurrentHoldTime = fsrComponents[5].currentHoldTime;
        
        timeVariables[0] = fsrComponents[0].timeHeld + fsrComponents[4].timeHeld;
        timeVariables[1] = fsrComponents[1].timeHeld + fsrComponents[5].timeHeld;
        timeVariables[2] = fsrComponents[2].timeHeld + fsrComponents[3].timeHeld;
        timeVariables[3] = fsrComponents[3].timeHeld + fsrComponents[2].timeHeld;
        timeVariables[4] = fsrComponents[4].timeHeld + fsrComponents[0].timeHeld;
        timeVariables[5] = fsrComponents[5].timeHeld + fsrComponents[1].timeHeld;
        timeVariables[6] = fsrComponents[4].timeHeld + fsrComponents[2].timeHeld;
        timeVariables[7] = fsrComponents[3].timeHeld + fsrComponents[2].timeHeld;
        timeVariables[8] = fsrComponents[1].timeHeld + fsrComponents[2].timeHeld;

        // Check if any FSR is currently active
        bool anyFSRActive = false;
        for (int i = 0; i < fsrComponents.Length; i++)
        {
            if (fsrComponents[i].IsActive())
            {
                anyFSRActive = true;
                break;
            }
        }
        
        // Update globalHoldTime based on whether any FSR is active
        if (anyFSRActive)
        {
            globalHoldTime += Time.deltaTime;
            noFSRTime = 0f; // Reset the no-FSR timer when any FSR is active

            // For now, advance the background color
            ColorChanger.S.ShiftHue();
        }
        else
        {
            // Decrease globalHoldTime at the same rate when no FSRs are active
            globalHoldTime -= Time.deltaTime;
            // Ensure it doesn't go below zero
            if (globalHoldTime < 0f)
            {
                globalHoldTime = 0f;
            }
            
            // Track time with no FSRs active
            noFSRTime += Time.deltaTime;
            
            // Trigger reset if no FSRs have been active for more than 15 seconds
            if (noFSRTime > resetThreshold)
            {
                Reset();
            }
        }

        // Check if any individual FSR has been held long enough
        if(fsrComponents[0].currentHoldTime > triggerTime ||
           fsrComponents[1].currentHoldTime > triggerTime ||
           fsrComponents[2].currentHoldTime > triggerTime ||
           fsrComponents[3].currentHoldTime > triggerTime ||
           fsrComponents[4].currentHoldTime > triggerTime ||
           fsrComponents[5].currentHoldTime > triggerTime)
        {
            TextManager.S.Koan();
            AudioManager.S.PlayPhrase();
        }
    }

    void Reset(){
        fsrsTriggered = false;
        globalHoldTime = 0f;
        AudioManager.S.ResetScore();
    }
    
}
