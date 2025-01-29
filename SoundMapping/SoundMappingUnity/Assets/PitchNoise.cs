using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PitchNoise : MonoBehaviour
{
    [Header("Pitch Control (0 → minPitch, 1 → maxPitch)")]
    [Range(0f, 1f)] public float freqLerp = 0f;

    [Header("Pitch Settings")]
    public float minPitch = 0.5f;
    public float maxPitch = 2.0f;

    private AudioSource _audioSource;

    // Start is called before the first frame update
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = true;
        _audioSource.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        float pitch = Mathf.Lerp(minPitch, maxPitch, freqLerp);
        _audioSource.pitch = pitch;
    }
}
