using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]

    // A collection of variables from combined holdTimes of certain FSRs
    public float[] timeVariables = new float[9];
    public float triggerTime = 10f;

    public static GameManager S;
    
    void Awake(){
        S = this;
    }

    // Update is called once per frame
    void Update()
    {
        // Get all FSR components
        FSR[] fsrComponents = FindObjectsOfType<FSR>();
        
        timeVariables[0] = fsrComponents[0].timeHeld + fsrComponents[4].timeHeld;
        timeVariables[1] = fsrComponents[1].timeHeld + fsrComponents[5].timeHeld;
        timeVariables[2] = fsrComponents[2].timeHeld + fsrComponents[3].timeHeld;
        timeVariables[3] = fsrComponents[3].timeHeld + fsrComponents[2].timeHeld;
        timeVariables[4] = fsrComponents[4].timeHeld + fsrComponents[0].timeHeld;
        timeVariables[5] = fsrComponents[5].timeHeld + fsrComponents[1].timeHeld;
        timeVariables[6] = fsrComponents[4].timeHeld + fsrComponents[2].timeHeld;
        timeVariables[7] = fsrComponents[3].timeHeld + fsrComponents[2].timeHeld;
        timeVariables[8] = fsrComponents[1].timeHeld + fsrComponents[2].timeHeld;

        if(fsrComponents[0].currentHoldTime > triggerTime ||
           fsrComponents[1].currentHoldTime > triggerTime ||
           fsrComponents[2].currentHoldTime > triggerTime ||
           fsrComponents[3].currentHoldTime > triggerTime ||
           fsrComponents[4].currentHoldTime > triggerTime ||
           fsrComponents[5].currentHoldTime > triggerTime)
        {
            TextManager.S.Koan();
        }
    }
}
