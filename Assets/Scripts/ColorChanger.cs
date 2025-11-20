using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ColorChanger : MonoBehaviour
{
    // Get the Color Adjustments component from the Volume
    private Volume volume;
    private ColorAdjustments colorAdjustments;

    [SerializeField] private float hueShift = 0f;
    [SerializeField] private float shiftSpeed = 1f;
    private float direction = 1f; // 1 for increasing, -1 for decreasing

    public static ColorChanger S;

    void Awake()
    {
        S = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        volume = GameObject.Find("PostProcessing").GetComponent<Volume>();
        if (volume == null)
        {
            Debug.LogError("Volume not found on PostProcessing GameObject");
            return;
        }

        if (volume.profile.TryGet<ColorAdjustments>(out colorAdjustments))
        {
            Debug.Log("ColorAdjustments found successfully");
        }
        else
        {
            Debug.LogError("ColorAdjustments not found in Volume profile");
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void ShiftHue(){
        if (colorAdjustments == null) return;
        
        hueShift += shiftSpeed * direction * Time.deltaTime;
        
        if(hueShift >= 360f){
            hueShift = 360f;
            direction = -1f; // Reverse direction
        }
        if(hueShift <= -360f){
            hueShift = -360f;
            direction = 1f; // Reverse direction
        }
        
        colorAdjustments.hueShift.value = hueShift;
    }
}
