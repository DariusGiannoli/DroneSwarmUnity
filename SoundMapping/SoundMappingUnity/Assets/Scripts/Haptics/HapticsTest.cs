using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class HapticsTest : MonoBehaviour
{
    public int dutyIntensity = 4;
    // Update is called once per frame
    void Update()
    {

    }

    void Start()
    {
        StartCoroutine(startHaptics());
    }



    IEnumerator startHaptics()
    {
        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            List<ObstacleInRange> obstacles = CameraMovement.embodiedDrone.GetComponent<DroneController>().obstaclesInRange;
            if(obstacles.Count > 0) {
                //find the closest obstacle and take its distance
                ObstacleInRange closestObstacle = obstacles[0];
                foreach(ObstacleInRange obstacle in obstacles) {
                    if(obstacle.distance < closestObstacle.distance) {
                        closestObstacle = obstacle;
                    }
                }

                mappingObstacleToHaptics(closestObstacle);
            }else{
                for(int i = 0; i < 8; i++) {
                    VibraForge.SendCommand(i, 0, 0, 0);
                }
            }
        }else {
            for(int i = 0; i < 8; i++) {
                VibraForge.SendCommand(i, 0, 0, 0);
            }
        }

        yield return new WaitForSeconds(0.3f);
        StartCoroutine(startHaptics());
    }

    void mappingObstacleToHaptics(ObstacleInRange obstacle) {
        Vector3 forwardVector = CameraMovement.embodiedDrone.transform.forward;
        float angleToForward = Vector3.SignedAngle(forwardVector, obstacle.position, Vector3.up);
        float distance = obstacle.distance;

        float forwardHaptic = 1/(Math.Abs(angleToForward)+float.Epsilon);
        float backwardHaptic = 1/(Math.Abs(180-angleToForward)+float.Epsilon);
        float leftHaptic = 1/(Math.Abs(angleToForward+90)+float.Epsilon);
        float rightHaptic = 1/(Math.Abs(angleToForward-90)+float.Epsilon);

        float sum = forwardHaptic + backwardHaptic + leftHaptic + rightHaptic;

        //apply min max scaling
        float maxHaptic = Mathf.Max(forwardHaptic, backwardHaptic, leftHaptic, rightHaptic);
        float minHaptic = Mathf.Min(forwardHaptic, backwardHaptic, leftHaptic, rightHaptic);

        forwardHaptic = (forwardHaptic - minHaptic) / (maxHaptic - minHaptic);
        backwardHaptic = (backwardHaptic - minHaptic) / (maxHaptic - minHaptic);
        leftHaptic = (leftHaptic - minHaptic) / (maxHaptic - minHaptic);
        rightHaptic = (rightHaptic - minHaptic) / (maxHaptic - minHaptic);

        float intensity =  (1 - distance / 5)*(1 - distance / 5);
        int freq = (int)(intensity * 1) + 1;

        
        
        //VibraForge.SendCommand(0, 1, (int)(forwardHaptic*dutyIntensity), freq);
        VibraForge.SendCommand(4, forwardHaptic>0.2f?1:0, (int)(forwardHaptic*dutyIntensity), freq);

        VibraForge.SendCommand(0, rightHaptic>0.2f?1:0, (int)(rightHaptic*dutyIntensity), freq);
        
        VibraForge.SendCommand(1, backwardHaptic>0.2f?1:0, (int)(backwardHaptic*dutyIntensity), freq);
        VibraForge.SendCommand(2, backwardHaptic>0.2f?1:0, (int)(backwardHaptic*dutyIntensity), freq);

        VibraForge.SendCommand(3, leftHaptic>0.2f?1:0, (int)(leftHaptic*dutyIntensity), freq);

        print("Forward: " + forwardHaptic + " Backward: " + backwardHaptic + " Left: " + leftHaptic + " Right: " + rightHaptic + " distance: " + distance);
    }
}
