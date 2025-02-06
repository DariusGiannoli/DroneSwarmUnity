using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class swarmDisconnection : MonoBehaviour
{
    // Start is called before the first frame update
    public int numberOfDronesLost = 4;
    public List<string> options = new List<string> { "Bip", "White Noise", "Bap", "Bop", "Beep" };
    private List<GameObject> drones
    {
        get
        {
            List<GameObject> drone = new List<GameObject>();
            foreach (Transform child in swarmModel.swarmHolder.transform)
            {
                if (dronesID.Contains(child.GetComponent<DroneController>().droneFake.id))
                {
                    drone.Add(child.gameObject);
                }
            }
            return drone;
        }
    }
    public List<AudioClip> droneSounds = new List<AudioClip>();
    public int selectedOption;

    public int radiusMin = 30;
    public int radiusMax = 60;

    private Coroutine currentSoundCoroutine = null;
    static public List<int> dronesID = new List<int>();
    public static bool playingSound = false;

    private static GameObject gm;


    void Start()
    {
        gm = this.gameObject;
    }

    public static void CheckDisconnection()
    {
        List<int> dronesIDAnalysis = new List<int>();
        
        NetworkCreator network = swarmModel.network;

        foreach (DroneFake drone in network.drones)
        {
            if (!network.IsInMainNetwork(drone))
            {
                dronesIDAnalysis.Add(drone.id);
            }
        }

        if(dronesIDAnalysis.Count > dronesID.Count) // if more drone than before retsrta animation
        {
            print("More drones disconnected");
             gm.GetComponent<swarmDisconnection>().StopAndPlaySound(0);
        }
        dronesID = dronesIDAnalysis;
        
    }

    public void StopAndPlaySound(int clipIndex)
    {
        if (!playingSound) // if he wasnrt playing any sound and wa sjust waiting before relaunching the sound then stop the coroutine
        {
            if (currentSoundCoroutine != null)
            {
                StopCoroutine(currentSoundCoroutine);
            }
            //restart the coroutine to force the sound to play
            currentSoundCoroutine = StartCoroutine(disconnectionSound());
        }
    }

    public IEnumerator disconnectionSound()
    {
        while (true)
        {
            yield return StartCoroutine(PlayClip(0));

            yield return new WaitForSeconds(10);
        }
    }

    private IEnumerator PlayClip(int clipIndex)
    {
        if (dronesID.Count == 0)
        {
            yield break;
        }

        
        playingSound = true;
        foreach (GameObject drone in drones)
        {
            drone.GetComponent<AudioSource>().clip = droneSounds[clipIndex];
            drone.GetComponent<AudioSource>().Play();
            yield return new WaitForSeconds(0.4f);
            drone.GetComponent<AudioSource>().Stop();
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(1f);
        playingSound = false;
    }

    public void PlaySound()
    {
        if(selectedOption == 0)
        {
            Debug.Log("Playing Bip sound");
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

        return;


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
            drone.GetComponent<AudioSource>().spatialBlend = 1;
            //make linear falloff
            drone.GetComponent<AudioSource>().rolloffMode = AudioRolloffMode.Linear;
            drones.Add(drone);
        }
    }

    public void RandomizeSwarm()
    {
        Debug.Log("Randomizing swarm");
    }
}
