using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine.InputSystem;

public class HapticsTest : MonoBehaviour
{
    #region ObstalceInRange
    public int dutyIntensity = 4;
    public float distanceDetection = 5;
    Thread hapticsThread;
    List<ObstacleInRange> obstacles = new List<ObstacleInRange>();
    Vector3 forwardVector = new Vector3(0, 0, 1);   
    Vector3 rightVector = new Vector3(1, 0, 0);

    List<Actuators> actuatorsRange = new List<Actuators>();

    #endregion

    #region NetworkLines

    List<Actuators> actuatorsBelly = new List<Actuators>();

    List<Actuators> lastDefined = new List<Actuators>();

    #endregion

    #region HapticsGamePad

    private Gamepad gamepad;



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

        gamepad = Gamepad.current;
        if (gamepad == null)
        {
            Debug.LogWarning("No gamepad connected.");
            gamepad.SetMotorSpeeds(0, 0);
        }
    }

    void OnDisable()
    {
        hapticsThread.Abort();
        gamepad.SetMotorSpeeds(0, 0);
    }
    public void HapticsPrediction(Prediction pred)
    {
        if (gamepad == null)
        {
            return;
        }

        if (pred.allData == null || pred.allData.Count == 0)
        {
            return; // Exit if no data to draw
        }

        //check is there is a crash
        float bestFractionOfPath = 2;
        foreach(DroneDataPrediction data in pred.allData) {
            if(data.idFirstCrash <= 0) {
                continue;
            }

            float fractionOfPath = 1-(float)data.idFirstCrash / data.positions.Count;
            if(fractionOfPath < bestFractionOfPath) {
                bestFractionOfPath = fractionOfPath;
            }
        }

        if(bestFractionOfPath < 1) {
            gamepad.SetMotorSpeeds(bestFractionOfPath, bestFractionOfPath);
        }else {
            gamepad.SetMotorSpeeds(0, 0);
        }
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

        List<Actuators> toSendList = new List<Actuators>();
        foreach (Actuators actuator in finalListNoDouble)
        {
            bool found = false;
            foreach (Actuators last in lastDefined)
            {
                if(actuator.Adresse == last.Adresse) {
                    found = true;
                    if (!actuator.Equal(last))
                    {
                        toSendList.Add(actuator); //send the new data

                        last.dutyIntensity = actuator.dutyIntensity; // update the old data 
                        last.frequency = actuator.frequency;
                    }
                }
            }

            if(!found) {
                Actuators newActuator = new Actuators(actuator.Adresse, actuator.Angle);
                newActuator.dutyIntensity = actuator.dutyIntensity;
                newActuator.frequency = actuator.frequency;

                toSendList.Add(newActuator);
                lastDefined.Add(newActuator);
            }
        }

       // print("FinalList: " + finalListNoDouble.Count + " toSendList: " + toSendList.Count + " lastDefined: " + lastDefined.Count);


        foreach(Actuators actuator in toSendList) {
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
                                actuator.dutyIntensity += (int)(10 - diff/3);
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
            getObstacles();

            hapticsThread = new Thread(new ThreadStart(CloseToWallThread));
            hapticsThread.Start();
        }
    }

    void getObstacles()
    {
        forwardVector = CameraMovement.embodiedDrone.transform.forward;
        rightVector = CameraMovement.embodiedDrone.transform.right;

        GameObject drone = CameraMovement.embodiedDrone;
        List<Vector3> pointObstacles = ClosestPointCalculator.ClosestPointsWithinRadius(drone.transform.position, distanceDetection);
        obstacles.Clear();
        foreach(Vector3 point in pointObstacles) {
            ObstacleInRange obstacle = new ObstacleInRange(point, Vector3.Distance(drone.transform.position, point));
            obstacles.Add(obstacle);
        }
    }

    void CloseToWallThread()
    {
        resetActuator(actuatorsRange);
        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            if(obstacles.Count > 0) {
                //find the closest obstacle and take its distance
                foreach(ObstacleInRange obstacle in obstacles) {
                    mappingObstacleToHaptics(obstacle);
                }
            }
        }
    }

    void mappingObstacleToHaptics(ObstacleInRange obstacle) {
        float angleToForward = Vector3.SignedAngle(forwardVector, obstacle.position, Vector3.up);
        float angleToBackward = Vector3.SignedAngle(-forwardVector, obstacle.position, Vector3.up);
        float angleToRight = Vector3.SignedAngle(rightVector, obstacle.position, Vector3.up);
        float angleToLeft = Vector3.SignedAngle(-rightVector, obstacle.position, Vector3.up);
        
        float distance = obstacle.distance;

        int forwardHaptic = Math.Abs(angleToForward) < 45 ? 1 : 0;
        int backwardHaptic = Math.Abs(angleToBackward) < 45 ? 1 : 0;
        int leftHaptic = Math.Abs(angleToLeft) < 45 ? 1 : 0;
        int rightHaptic = Math.Abs(angleToRight) < 45 ? 1 : 0;

        int intensity = (int)(6 - 4 * distance / distanceDetection);
        
        int freq = (int)(intensity * 1) + 1;
        int dutyForward = forwardHaptic * intensity;
        int dutyBackward = backwardHaptic * intensity;
        int dutyRight = rightHaptic * intensity;
        int dutyLeft = leftHaptic * intensity;

        if(dutyForward > 0) {
            setActuator(actuatorsRange, 4, dutyForward, freq);
            setActuator(actuatorsRange, 3, dutyForward, freq);
            setActuator(actuatorsRange, 7, dutyForward, freq);
            setActuator(actuatorsRange, 0, dutyForward, freq);
        }

        if(dutyBackward > 0) {
            setActuator(actuatorsRange, 2, dutyBackward, freq);
            setActuator(actuatorsRange, 5, dutyBackward, freq);
            setActuator(actuatorsRange, 1, dutyBackward, freq);
            setActuator(actuatorsRange, 6, dutyBackward, freq);
        }

        if(dutyRight > 0) {
            setActuator(actuatorsRange, 4, dutyRight, freq);
            setActuator(actuatorsRange, 5, dutyRight, freq);
            setActuator(actuatorsRange, 6, dutyRight, freq);
            setActuator(actuatorsRange, 7, dutyRight, freq);
        }

        if(dutyLeft > 0) {
            setActuator(actuatorsRange, 1, dutyLeft, freq);
            setActuator(actuatorsRange, 2, dutyLeft, freq);
            setActuator(actuatorsRange, 3, dutyLeft, freq);
            setActuator(actuatorsRange, 0, dutyLeft, freq);
        }

    }


    void setActuator(List<Actuators> actuators, int adresse, int intensity)
    {
        setActuator(actuators, adresse, intensity, 1);
    }
    void setActuator(List<Actuators> actuators, int adresse, int intensity, int freq)
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

    public int dutyIntensity = 0;
    public int frequency = 1;

    public int duty
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

    //create operator overload
    public bool Equal(Actuators a)
    {
        return a.duty == this.duty && a.frequency == this.frequency;
    }
}
