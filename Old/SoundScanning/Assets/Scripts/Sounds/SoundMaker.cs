using System.Collections;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

public class SoundMaker : MonoBehaviour
{
    [Header("Sound Description")]
    public float playEverySeconds = 1.0f; // play sound every x seconds
    public float rotationSpeed = 10.0f; // degrees per second
    [Header("Sound Clips")]
    public AudioClip[] sounds;


    public GameObject audioObject;

    // Start is called before the first frame update


    // Update is called once per frame
    void Update()
    {
        // Rotate the object
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);   
        Vector3 pos = getPosition(transform.forward);
        audioObject.transform.position = pos;
        //draw a ray
        Debug.DrawRay(transform.position, transform.forward * 100, Color.red);

        if(Time.time % playEverySeconds < Time.deltaTime)
        {
            //playSound(transform.forward);
           
        }
    }

    private Vector3 getPosition(Vector3 direction)
    {
        Ray ray = new Ray(transform.position, direction);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return hit.point;
        }
        return Vector3.zero;
    }
    private void playSound(Vector3 direction)
    {
        // Ray with infinite distance
        Ray ray = new Ray(transform.position, direction);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            // Calculate the distance between the hit point and the origin of the ray
            float distance = Vector3.Distance(transform.position, hit.point);
            
            // Randomly select a sound
            int index = Random.Range(0, sounds.Length);
            
            // Dynamically create an AudioSource at the hit point
            GameObject audioObject = new GameObject("TemporaryAudio");
            audioObject.transform.position = hit.point;
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.clip = sounds[index];

            // Adjust the pitch based on distance (example: closer = higher pitch, farther = lower pitch)
            // You can fine-tune this scale to fit your specific use case
            audioSource.pitch = Mathf.Clamp(1.0f / (distance * 0.1f), 0.5f, 2.0f); // Clamps pitch between 0.5 and 2.0
            
            // Play the sound
            audioSource.Play();

            // Destroy the audioObject after the sound finishes playing
            Destroy(audioObject, sounds[index].length);
        }
    }
}

