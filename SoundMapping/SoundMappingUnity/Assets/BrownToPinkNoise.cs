using UnityEngine;
using System;

[RequireComponent(typeof(AudioSource))]
public class BrownToPinkNoise : MonoBehaviour
{
    [Header("Blend = 0 → 100% Brown  |  1 → 100% Pink")]
    [Range(0f, 1f)]
    public float blend = 0f;

    // ------------------ Brown Noise State ------------------
    private float _brownSample = 0f;    
    private const float BrownStep  = 0.02f;
    private const float BrownClamp = 1.0f;

    // ------------------ Pink Noise State -------------------
    // A simple one-pole filter approach: pink = pink + α * (white - pink)
    private float _pinkSample = 0f;
    private const float PinkAlpha = 0.05f;   // Adjust to taste

    // Use a thread-safe random from .NET
    private System.Random _rng;

    bool isPlaying = true;

    void Awake()
    {
        // Initialize our System.Random with a seed (optional)
        _rng = new System.Random(); // or specify a seed, e.g. new System.Random(1234)
        // Enable the AudioSource component
        this.GetComponent<AudioSource>().enabled = true;
    }

    void OnApplicationQuit()
    {
        // Clean up our System.Random
        _rng = null;
        // Stop audio
        GetComponent<AudioSource>().Stop();
        isPlaying = false;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        // Early exit if the audio is not supposed to play
        if (!isPlaying) return;

        for (int i = 0; i < data.Length; i += channels)
        {
            // 1) Generate a white noise sample [-1, +1]
            float white = NextFloat() * 2f - 1f;

            // 2) Generate Brown noise (integration)
            _brownSample += white * BrownStep;
            _brownSample = Mathf.Clamp(_brownSample, -BrownClamp, BrownClamp);
            float brown = _brownSample;

            // 3) Generate Pink noise (simple 1-pole filter)
            //    pink[n] = pink[n-1] + α(white[n] - pink[n-1])
            _pinkSample += PinkAlpha * (white - _pinkSample);
            float pink = _pinkSample;

            // 4) Blend between Brown and Pink
            float sample = Mathf.Lerp(brown, pink, blend);

            // Write the sample to all channels
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }
        }
    }

    /// <summary>
    /// Returns a float in [0,1) using System.Random, cast to float.
    /// </summary>
    private float NextFloat()
    {
        try
        {
            return (float)_rng.NextDouble();
        }
        catch (Exception)
        {
            // In case of threading issues, fallback
            return UnityEngine.Random.value;
        }
    }
}
