using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]

    // A collection of variables from combined holdTimes of certain FSRs
    public float[] timeVariables = new float[9];
    public float triggerTime = 10f;
    
    [Header("FSR Current Hold Times")]
    // Public currentHoldTime variables for each FSR (0-5)
    public float[] fsrCurrentHoldTimes = new float[6];

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
        for (int i = 0; i < fsrComponents.Length; i++)
        {
            fsrCurrentHoldTimes[i] = fsrComponents[i].currentHoldTime;
        }
        
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

        // // Check if any individual FSR has been held long enough
        // if(fsrComponents[0].currentHoldTime > triggerTime ||
        //    fsrComponents[1].currentHoldTime > triggerTime ||
        //    fsrComponents[2].currentHoldTime > triggerTime ||
        //    fsrComponents[3].currentHoldTime > triggerTime ||
        //    fsrComponents[4].currentHoldTime > triggerTime ||
        //    fsrComponents[5].currentHoldTime > triggerTime)
        // {
        //     TextManager.S.Koan();
        //     AudioManager.S.PlayPhrase();
        // }

        // Versin 2: Check if Global Hold Time is greater than triggerTime
        if(globalHoldTime > triggerTime)
        {
            TextManager.S.Koan();
            AudioManager.S.PlayPhrase();

            // And then reset the global hold time
            globalHoldTime = 0f;
        }
    }

    void Reset(){
        fsrsTriggered = false;
        globalHoldTime = 0f;
        AudioManager.S.ResetScore();
    }
    
}
