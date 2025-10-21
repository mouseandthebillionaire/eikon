using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public AudioSource phrases;
    public AudioClip[] phraseClips;
    private int phraseIndex = 0;

    public AudioSource[] chordNotes;

    public AudioSource wah;
    public AudioClip[] wahClips;

    public AudioSource[] nodeNotes;
    public static AudioManager S;

    public AudioSource chime;
    public AudioClip[] chimeClips;

    public AudioSource transition;
    public AudioClip[] transitionClips;
    
    private Dictionary<int, Coroutine> pitchTransitionCoroutines = new Dictionary<int, Coroutine>();
    private Dictionary<int, bool> hasInitializedPitch = new Dictionary<int, bool>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        S = this;   
    }

    void Update(){

        // Get all FSR components
        FSR[] fsrComponents = FindObjectsOfType<FSR>();

        for(int i = 0; i < chordNotes.Length; i++){
            chordNotes[i].volume = fsrComponents[i].timeHeld / 10f;
            if(chordNotes[i].volume > 1f){
                chordNotes[i].volume = 1f;
            }
            if(chordNotes[i].volume < 0f){
                chordNotes[i].volume = 0f;
            }
        }
    }

    public void PlayPhrase(){
        phrases.clip = phraseClips[phraseIndex];
        phrases.Play();
        phraseIndex = (phraseIndex + 1) % phraseClips.Length;
    }

    public void PlayWah(){
        wah.Play();
    }

    public void PlayChime(){
        chime.clip = chimeClips[Random.Range(0, chimeClips.Length)];
        chime.Play();
    }

    public void PlayNodeNote(int index)
    {
        nodeNotes[index].Play();
        StartCoroutine(FadeInVolume(index));
        hasInitializedPitch[index] = false;
    }

    public void PlayTransition(){
        transition.clip = transitionClips[Random.Range(0, transitionClips.Length)];
        transition.Play();
    }

    private IEnumerator FadeInVolume(int index)
    {
        // Give it a short pause
        yield return new WaitForSeconds(1f);
        float fadeDuration = 1.0f;
        float elapsedTime = 0f;
        
        // Start at 0
        nodeNotes[index].volume = 0f;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            
            // Smoothly interpolate volume from 0 to 1
            nodeNotes[index].volume = Mathf.Lerp(0f, 1f, t);
            
            yield return null;
        }
        
        // Ensure final volume is set
        nodeNotes[index].volume = 1f;
    }
    
    private IEnumerator TransitionPitch(AudioSource source, float targetPitch, float duration)
    {
        float startPitch = 1f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            source.pitch = Mathf.Lerp(startPitch, targetPitch, t);
            yield return null;
        }

        source.pitch = targetPitch;
    }

    public void NodeFilter(int index, float filter){
        float filterVal = 3000f - (filter * 2900f);
        nodeNotes[index].GetComponent<AudioLowPassFilter>().cutoffFrequency = filterVal;
        
        float targetPitch = 1f - (filter * 0.5f);
        
        if (!hasInitializedPitch.ContainsKey(index) || !hasInitializedPitch[index])
        {
            // First time setting pitch - do the transition
            if (pitchTransitionCoroutines.ContainsKey(index))
            {
                StopCoroutine(pitchTransitionCoroutines[index]);
            }
            pitchTransitionCoroutines[index] = StartCoroutine(TransitionPitch(nodeNotes[index], targetPitch, 1f));
            hasInitializedPitch[index] = true;
        }
        else
        {
            // Subsequent changes - set pitch directly
            nodeNotes[index].pitch = targetPitch;
        }
    }

    public void NodeClearFilter(int index){
        nodeNotes[index].GetComponent<AudioLowPassFilter>().cutoffFrequency = 5000f;
        nodeNotes[index].pitch = 1f;
    }

    public void ResetNodeNotes(){
        for (int i = 0; i < nodeNotes.Length; i++){
            nodeNotes[i].GetComponent<AudioLowPassFilter>().cutoffFrequency = 5000f;
            StartCoroutine(FadeOutVolume(i));
        }
    }

    private IEnumerator FadeOutVolume(int index)
    {
        float fadeDuration = 3.0f;
        float elapsedTime = 0f;
        
        // Start at current volume
        float startVolume = nodeNotes[index].volume;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeDuration;
            
            // Smoothly interpolate volume from current to 0
            nodeNotes[index].volume = Mathf.Lerp(startVolume, 0f, t);
            
            yield return null;
        }
        
        // Ensure final volume is set
        nodeNotes[index].volume = 0f;
    }

}
