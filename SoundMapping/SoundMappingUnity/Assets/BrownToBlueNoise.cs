using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class BrownToBlueNoise : MonoBehaviour
{
    [Header("Blend = 0 → 100% Brown  |  1 → 100% Blue")]
    [Range(0f, 1f)]
    public float blend = 0f;

    [Header("Moving range for Animation")]
    [Range(0f, 1f)]
    public float a = 0.1f;

    public float animationDuration = 1f;

    // ----- Brown Noise State -----
    private float _brownSample = 0f;    
    private const float BrownStep  = 0.02f;
    private const float BrownClamp = 1.0f;

    // ----- Blue Noise State -----
    // We'll do a simple derivative of white noise: blue[n] = (white[n] - white[n-1]) * gain
    private float _lastWhite  = 0f;   
    private const float BlueGain = 0.2f; // Adjust this gain to taste

    // Use a thread‐safe random from .NET
    private System.Random _rng;

    bool isPlaying = true;

    void Awake()
    {
        // Initialize our System.Random with a seed (optional)
        _rng = new System.Random(); // or specify a seed, e.g. (1234)
        this.GetComponent<AudioSource>().enabled = true;
    }

    void OnApplicationQuit()
    {
        // Clean up our System.Random
        _rng = null;
        // stop the audio
        GetComponent<AudioSource>().Stop();
        isPlaying = false;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            if (!isPlaying)
            {
                return;
            }
            // ----- 1) Generate a white noise sample -----
            float white = NextFloat() * 2f - 1f; 

            // ----- 2) Generate Brown Noise Sample (integration) -----
            _brownSample += white * BrownStep;
            _brownSample = Mathf.Clamp(_brownSample, -BrownClamp, BrownClamp);
            float brown = _brownSample;

            // ----- 3) Generate Blue Noise Sample (differentiation) -----
            float blue = (white - _lastWhite) * BlueGain;
            _lastWhite = white;

            // ----- 4) Blend between Brown and Blue noise -----
            float sample = Mathf.Lerp(brown, blue, blend);

            // Write the same sample to each channel
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }
        }
    }

    /// <summary>
    /// Returns a float in [0,1) using System.Random, then cast to float.
    /// </summary>
    private float NextFloat()
    {
        try
        {
            return (float)_rng.NextDouble();
        }
        catch (Exception)
        {
            // If we hit an exception, it's likely due to threading issues.
            // In that case, we'll just return a random value from Unity's Random class.
            return 0;
        }
    }

    public void Shrink()
    {
        StartCoroutine(startAnimation(blend, Mathf.Clamp(blend - a, 0f, 1f), animationDuration));
    }

    public void Expand()
    {
        StartCoroutine(startAnimation(blend, Mathf.Clamp(blend + a, 0f, 1f), animationDuration));
    }

    IEnumerator startAnimation(float start, float end, float duration)
    {
        float startTime = Time.time;
        float endTime = startTime + duration;
        float t = 0f;
        while (Time.time < endTime)
        {
            t = (Time.time - startTime) / duration;
            blend = Mathf.Lerp(start, end, t);
            yield return null;
        }
        blend = start;
    }
}
