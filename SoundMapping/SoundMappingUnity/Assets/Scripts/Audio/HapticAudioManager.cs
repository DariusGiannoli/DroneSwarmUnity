using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class GlobalConstants
{
    public const float NETWORK_REFRESH_RATE = 0.1f; // in seconds
    public const int NUMBER_OF_PAST_SCORE = 20; // 2 sec of moving avergae 
}
public class HapticAudioManager : MonoBehaviour
{
    //Make  dict with 
    public static List<droneStatus> drones = new List<droneStatus>();

    public const float DUTYFREQ = 50; // in percent
    public const float INITFREQ = 1f ; // in Hz


    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0; i < swarmModel.swarmHolder.transform.childCount; i++)
        {
            drones.Add(new droneStatus(swarmModel.swarmHolder.transform.GetChild(i).gameObject));
        }
    }

    public void Reset()
    {
        drones.Clear();
        for (int i = 0; i < swarmModel.swarmHolder.transform.childCount; i++)
        {
            drones.Add(new droneStatus(swarmModel.swarmHolder.transform.GetChild(i).gameObject));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static float GetDroneNetworkScore(GameObject drone)
    {
        foreach (var d in drones)
        {
            if (d.isThisDrone(drone))
            {
                return d.currentScore;
            }
        }

        return -1;
    }

    public static void SetDroneNetworkScore(GameObject drone, float score)
    {
        foreach (var d in drones)
        {
            if (d.isThisDrone(drone))
            {
                d.currentScore = score;
            }
        }
    }

    
    public static bool GetAudioSourceCharacteristics(float time_elapsed)
    {
        float startFrequency = INITFREQ;
        float time_remaining = time_elapsed;
        int time_repeat = 0;
        //every time a cycleis completed, the frequency is increased by 1 Hz
        while(startFrequency < 20)
        {
            if(time_remaining > 1 / startFrequency)
            {
                if(time_repeat >= 2)
                {
                    time_remaining -= 1 / startFrequency; 
                    startFrequency += 1;
                    time_repeat = 0;
                }
                else
                {
                    time_remaining -= 1 / startFrequency;
                    time_repeat++;
                }
            }
            else
            {
                break;
            }
        }

        //if the time remaining is less than the duty cycle, the audio source is on
        if(time_remaining < DUTYFREQ / 100 * 1 / startFrequency)
        {
            return true;
        }
       return false;
    }
}


public class droneStatus
{
    public GameObject drone;
    public List<float> droneScores = new List<float>();
    public float currentScore;

    public droneStatus(GameObject drone)
    {
        this.drone = drone;
    }

    public bool isThisDrone(GameObject d)
    {
        
        return d == drone;
    }

    public void SetScore(float score)
    {
        droneScores.Add(score);
        updateScore();
    }

    private void updateScore()
    {
        //make moving average of last 5 scores
        if(droneScores.Count > GlobalConstants.NUMBER_OF_PAST_SCORE)
        {
            droneScores.RemoveAt(0);
        }

        float sum = 0;
        foreach (var score in droneScores)
        {
            sum += score;
        }

        currentScore = sum / droneScores.Count;
    }
}
