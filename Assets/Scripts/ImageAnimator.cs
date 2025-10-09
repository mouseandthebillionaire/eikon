using UnityEngine;
using System.Collections;

public class ImageAnimator : MonoBehaviour
{
    public string spriteSheetName = "layer_0";
    public float frameRate = 10f;
    public bool loop = true;

    private Sprite[] frames;
    
    private int currentFrame = 0;
    private int direction = 1; // 1 for forward, -1 for backward
    private float timer = 0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        frames = Resources.LoadAll<Sprite>("wickSprites/" + spriteSheetName);
        
        if (frames.Length == 0)
        {
            Debug.LogError("No frames found in the Resources folder");
            return;
        }

        // Randomize rotation
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        
        StartCoroutine(Play());
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
}
