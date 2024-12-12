using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading;
using Unity.VisualScripting;

public class HapticsTest : MonoBehaviour
{
    #region ObstalceInRange
    public int dutyIntensity = 4;
    Thread hapticsThread;
    List<ObstacleInRange> obstacles = new List<ObstacleInRange>();
    Vector3 forwardVector = new Vector3(0, 0, 1);   

    List<Actuators> actuatorsRange = new List<Actuators>();

    #endregion

    #region NetworkLines

    List<Actuators> actuatorsBelly = new List<Actuators>();

    #endregion


    public int sendEvery = 200;
    // Update is called once per frame
    void Start()
    {
        hapticsThread = new Thread(new ThreadStart(CloseToWallThread));

        for (int i = 28; i <= 39; i++)
        {
            float angleDeg = (i - 28) * 30;  
            angleDeg = angleDeg - 90;
            if(angleDeg < 0) {
                angleDeg += 360;
            }

            angleDeg = 360 - angleDeg;

            int adresse = 37 + (i - 28);
            actuatorsBelly.Add(new Actuators(adresse, angleDeg));
        }


        for (int i = 0; i < 8; i++)
        {
            int adresse = i;
            actuatorsRange.Add(new Actuators(adresse, 0));
        }


        StartCoroutine(HapticsCoroutine());

        hapticsThread.Start();
    }


    IEnumerator HapticsCoroutine()
    {
        while (true)
        {
            sendCommands();
            resetThread();
            yield return new WaitForSeconds(sendEvery / 1000);
        }
    }

    void sendCommands()
    {
        List<Actuators> finalList = new List<Actuators>();
        finalList.AddRange(actuatorsRange);
        finalList.AddRange(actuatorsBelly);

        //check if the actuators have the same adresse is so add the duty and keep highest frequency
        List<Actuators> finalListNoDouble = new List<Actuators>();
        foreach(Actuators actuator in finalList) {
            bool found = false;
            foreach(Actuators actuatorNoDouble in finalListNoDouble) {
                if(actuator.Adresse == actuatorNoDouble.Adresse) {
                    actuatorNoDouble.dutyIntensity += actuator.dutyIntensity;
                    actuatorNoDouble.frequency = Math.Max(actuator.frequency, actuatorNoDouble.frequency);
                    found = true;
                }
            }
            if(!found) {
                finalListNoDouble.Add(actuator);
            }
        }


        print("finalListNoDouble: " + finalListNoDouble.Count);
        foreach(Actuators actuator in finalListNoDouble) {
            VibraForge.SendCommand(actuator.Adresse, (int)actuator.duty == 0 ? 0:1, (int)actuator.duty, (int)actuator.frequency);
        }
    }


    void resetThread()
    {
        if(hapticsThread.ThreadState == ThreadState.Stopped) {
            closeToWall();
        }
        NetworkLinesThread();
    }


    void getActuratorsBellyNetworkLines()
    {
        resetActuator(actuatorsBelly);

        Dictionary<GameObject, List<GameObject>> adjacencyList = this.GetComponent<DroneNetworkManager>().adjacencyList;

        List<GameObject> alreadyChecked = new List<GameObject>();

        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            foreach (var drone in adjacencyList.Keys){
                foreach (var neighbor in adjacencyList[drone]){
                    if(CameraMovement.embodiedDrone == drone)
                    {
                        Vector3 direction = (drone.transform.position - neighbor.transform.position).normalized;
                        float angle = Vector3.SignedAngle(direction, CameraMovement.embodiedDrone.transform.forward, Vector3.up);
                        if(angle < 0) {
                            angle += 360;
                        }

                        foreach(Actuators actuator in actuatorsBelly) {
                            float diff = angle - actuator.Angle;
                            if(diff > 180) {
                                diff = 360 - diff;
                            }
                            //map the internsity linearly
                            if(diff < 30 && diff > -30) {
                                actuator.dutyIntensity += 10 - diff/3;
                            }
                        }
                    }
                }
            }
        }
    }


    void NetworkLinesThread()
    {
        getActuratorsBellyNetworkLines();

    }

    void closeToWall()
    {
        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            print("CloseToWall");
            getObstacles();
            print("Obstacles: " + obstacles.Count);

            hapticsThread = new Thread(new ThreadStart(CloseToWallThread));
            hapticsThread.Start();
        }
    }

    void getObstacles()
    {
        forwardVector = CameraMovement.embodiedDrone.transform.forward;

        GameObject drone = CameraMovement.embodiedDrone;
        List<Vector3> pointObstacles = ClosestPointCalculator.ClosestPointsWithinRadius(drone.transform.position, DroneFake.avoidanceRadius);
        obstacles.Clear();
        foreach(Vector3 point in pointObstacles) {
            ObstacleInRange obstacle = new ObstacleInRange(point, Vector3.Distance(drone.transform.position, point));
            obstacles.Add(obstacle);
        }
    }

    void CloseToWallThread()
    {
        resetActuator(actuatorsRange);
        print("Obstacles: " + obstacles.Count);
        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            if(obstacles.Count > 0) {
                //find the closest obstacle and take its distance
                ObstacleInRange closestObstacle = obstacles[0];
                foreach(ObstacleInRange obstacle in obstacles) {
                    if(obstacle.distance < closestObstacle.distance) {
                        closestObstacle = obstacle;
                    }
                }

                mappingObstacleToHaptics(closestObstacle);
            }
        }
    }

    void mappingObstacleToHaptics(ObstacleInRange obstacle) {
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

        setActuator(actuatorsRange, 4, (int)(forwardHaptic*dutyIntensity), freq);
        setActuator(actuatorsRange, 0, (int)(rightHaptic*dutyIntensity), freq);
        setActuator(actuatorsRange, 1, (int)(backwardHaptic*dutyIntensity), freq);
        setActuator(actuatorsRange, 2, (int)(backwardHaptic*dutyIntensity), freq);
        setActuator(actuatorsRange, 3, (int)(leftHaptic*dutyIntensity), freq);

        setActuator(actuatorsRange, 5, 10, freq);
        print("actuatorsRange: " + actuatorsRange.Count);
        foreach (Actuators actuator in actuatorsRange)
        {
            if(actuator.Adresse == 5) {
                print("Adresse: " + actuator.Adresse + " Intensity: " + actuator.dutyIntensity + " Frequency: " + actuator.frequency);
            }
        }

        print("Forward: " + forwardHaptic + " Backward: " + backwardHaptic + " Left: " + leftHaptic + " Right: " + rightHaptic + " distance: " + distance);
    }


    void setActuator(List<Actuators> actuators, int adresse, float intensity)
    {
        setActuator(actuators, adresse, intensity, 1);
    }
    void setActuator(List<Actuators> actuators, int adresse, float intensity, int freq)
    {
        foreach(Actuators actuator in actuators) {
            if(actuator.Adresse == adresse) {
                actuator.dutyIntensity += intensity;
                actuator.frequency = Math.Max(actuator.frequency, freq);
            }
        }
    }

    
    void resetActuator(List<Actuators> actuators)
    {
        foreach(Actuators actuator in actuators) {
            actuator.dutyIntensity = 0;
            actuator.frequency = 1;
        }
    }

}


public class Actuators
{
    public int Adresse { get; set; }
    public float Angle { get; set; }

    public float dutyIntensity = 0;
    public float frequency = 1;

    public float duty
    {
        get{
            if(dutyIntensity > 10) {
                return 10;
            }else return dutyIntensity;
        }
    }

    public Actuators(int adresse, float angle)
    {
        Adresse = adresse;
        Angle = angle;
    }
}
