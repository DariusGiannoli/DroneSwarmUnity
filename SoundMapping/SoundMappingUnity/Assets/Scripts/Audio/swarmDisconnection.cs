using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class swarmDisconnection : MonoBehaviour
{
    // Start is called before the first frame update
    public int numberOfDronesLost = 4;
    public List<string> options = new List<string> { "Bip", "White Noise", "Bap", "Bop", "Beep" };
    private List<GameObject> drones = new List<GameObject>();
    public List<AudioClip> droneSounds = new List<AudioClip>();
    public int selectedOption;

    public int radiusMin = 30;
    public int radiusMax = 60;

    private Coroutine currentSoundCoroutine;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void Bip()
    {
        
    }

    public IEnumerator playBip()
    {
        foreach (GameObject drone in drones)
        {
            drone.GetComponent<AudioSource>().clip = droneSounds[0];
            drone.GetComponent<AudioSource>().Play();
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void StopAndPlaySound(int clipIndex)
    {
        if(currentSoundCoroutine != null)
        {
            StopCoroutine(currentSoundCoroutine);
        }
        currentSoundCoroutine = StartCoroutine(PlayClip(clipIndex));
    }

    private IEnumerator PlayClip(int clipIndex)
    {
        foreach (GameObject drone in drones)
        {
            drone.GetComponent<AudioSource>().clip = droneSounds[clipIndex];
            drone.GetComponent<AudioSource>().Play();
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void PlaySound()
    {
        if(selectedOption == 0)
        {
            Bip();
        }
        else if(selectedOption == 1)
        {
            Debug.Log("Playing White Noise sound");
        }
        else if(selectedOption == 2)
        {
            Debug.Log("Playing Bap sound");
        }
        else if(selectedOption == 3)
        {
            Debug.Log("Playing Bop sound");
        }
        else if(selectedOption == 4)
        {
            Debug.Log("Playing Beep sound");
        }
    }

    public void RegenerateSwarm()
    {
        foreach (GameObject drone in drones)
        {
            Destroy(drone);
        }

        for (int i = 0; i < numberOfDronesLost; i++)
        {
            GameObject drone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            float randomRadius = Random.Range(radiusMin, radiusMax);
            float randomAngle = Random.Range(0, 360);
            drone.transform.position = new Vector3(randomRadius * Mathf.Cos(randomAngle), 0, randomRadius * Mathf.Sin(randomAngle));
            //add audio source
            drone.AddComponent<AudioSource>();
            drones.Add(drone);
        }
    }

    public void RandomizeSwarm()
    {
        Debug.Log("Randomizing swarm");
    }
}
