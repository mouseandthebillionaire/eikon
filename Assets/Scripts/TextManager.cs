using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TextManager : MonoBehaviour
{
    private TextAsset    koans_asset;
    private string[]     koans = new string[0];

    private string currKoan;

    private int    koanID;
    public  TextMeshProUGUI textDisplay;
    public Image background;
    private bool isKoanTriggered = false;

    public float backgroundFadeDuration = 1f;
    public float targetBackgroundAlpha = 0.5f;

    public static TextManager S;

    void Awake()
    {
        S = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        textDisplay = GetComponent<TextMeshProUGUI>();
        
        Reset();
        LoadKoans();
    }
    

    public void Koan()
    {
        if(isKoanTriggered) {
            return;
        }
        isKoanTriggered = true;
        StartCoroutine(KoanDisplay());
    }

    private IEnumerator KoanDisplay(){
        currKoan = koans[koanID];
        textDisplay.text = currKoan;

        StartCoroutine(FadeInBackground());

        Debug.Log($"Koan: {currKoan}");
        
        // Reset all FSR timeHeld and currentHoldTime values to 0
        FSR.ResetAllTimeHeld();
        
        yield return new WaitForSeconds(4f);
        
        // Fade out the text
        yield return StartCoroutine(FadeOutText());
        
        ClearText();
        isKoanTriggered = false;

        // Get next Koan ready
        koanID = (koanID + 1) % koans.Length;

    }

    private IEnumerator FadeInBackground()
    {
        float fadeDuration = 1f; // Duration of fade in seconds
        float elapsedTime = 0f;
        Color originalColor = background.color;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, targetBackgroundAlpha, elapsedTime / fadeDuration);
            background.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }
    }
    
    private IEnumerator FadeOutText()
    {
        float fadeDuration = 1f; // Duration of fade in seconds
        float elapsedTime = 0f;
        Color originalColor = textDisplay.color;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            float backgroundAlpha = Mathf.Lerp(targetBackgroundAlpha, 0f, elapsedTime / fadeDuration);
            textDisplay.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            background.color = new Color(background.color.r, background.color.g, background.color.b, backgroundAlpha);
            yield return null;
        }
        
        // Ensure alpha is exactly 0
        textDisplay.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
    }

    public void ClearText()
    {
        textDisplay.text = "";
        // Reset alpha to full opacity for next text display
        Color currentColor = textDisplay.color;
        textDisplay.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
    }

    public void LoadKoans()
    {
        string file = "koans";
        
        koans_asset = Resources.Load(file) as TextAsset;
        koans = koans_asset.text.Split("\n");
    }


    public void Reset()
    {
        koanID = 0;
        ClearText();
    }
}
