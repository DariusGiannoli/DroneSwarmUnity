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
    public float distanceDetection = 3;
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

    #region Variables

    public List<Actuators> actuatorsVariables = new List<Actuators>();
    #endregion

    public int sendEvery = 1000;
    // Update is called once per frame
    void Start()
    {

        for (int i = 10; i < 10; i++)
        {
            int adresse = i;
            actuatorsBelly.Add(new Actuators(adresse, 310/10 * i));
        }

        for (int i = 10; i < 10; i++)
        {
            int adresse = i;
            actuatorsRange.Add(new Actuators(adresse, 310/10 * i));
        }

        for (int i = 0; i < 10; i++)
        {
            int adresse = i;
            actuatorsVariables.Add(new Actuators(adresse, 310/10 * i));
        }


        StartCoroutine(HapticsCoroutine());

        gamepad = Gamepad.current;
        if (gamepad == null)
        {
            Debug.LogWarning("No gamepad connected.");
            gamepad.SetMotorSpeeds(0, 0);
        }else {
            gamepad.SetMotorSpeeds(0.0f, 0.0f);
        }
    }

    void Disable()
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
            //closeToWall();
            variableTest();
            //getActuratorsBellyNetworkLines();
            sendCommands();
            yield return new WaitForSeconds(sendEvery / 1000);
        }
    }

    void sendCommands()
    {
        List<Actuators> finalList = new List<Actuators>();
        finalList.AddRange(actuatorsRange);
        finalList.AddRange(actuatorsBelly);
        finalList.AddRange(actuatorsVariables);

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

      //  print("FinalList: " + finalListNoDouble.Count + " toSendList: " + toSendList.Count + " lastDefined: " + lastDefined.Count);


        foreach(Actuators actuator in toSendList) {
            VibraForge.SendCommand(actuator.Adresse, (int)actuator.duty == 0 ? 0:1, (int)actuator.duty, (int)actuator.frequency);
        }
    }

    void variableTest()
    {
        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            DroneFake main = CameraMovement.embodiedDrone.GetComponent<DroneController>().droneFake;
            Vector3 forcesDir  = main.getHapticVector();

            Actuators closestActuator = getDirectionActuator(forcesDir.normalized, actuatorsVariables);
            float duty = forcesDir.magnitude / 2;

            foreach(Actuators actuator in actuatorsVariables) {
                actuator.dutyIntensity = 0;
                actuator.frequency = 1;
            }

            duty = MathF.Min(duty, 10);
        
            closestActuator.dutyIntensity = (int)duty;  
            closestActuator.frequency = 1;          
        }
    }

    void getActuratorsBellyNetworkLines()
    {
        if(swarmModel.network == null) {
            return;
        }

        foreach(Actuators actuator in actuatorsBelly) {
            actuator.dutyIntensity = 0;
            actuator.frequency = 1;
        }

        Dictionary<DroneFake, List<DroneFake>> adjacencyList = swarmModel.network.adjacencyList;

        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            DroneFake main = CameraMovement.embodiedDrone.GetComponent<DroneController>().droneFake;
            List<DroneFake> neighbors = swarmModel.network.GetNeighbors(main);

            foreach(DroneFake neighbor in neighbors) {
                Vector3 direction = neighbor.position - main.position;
                Actuators closestActuator = getDirectionActuator(direction.normalized, actuatorsBelly);

                float dist = (neighbor.position - main.position).magnitude;
                closestActuator.dutyIntensity = Mathf.Max(6 - 5*(int)(dist / swarmModel.desiredSeparation),0);
            }
        }

        foreach(Actuators actuator in actuatorsBelly) {
            if(actuator.dutyIntensity > 0) {
                print("Actuator: " + actuator.Adresse + " Duty: " + actuator.dutyIntensity + "angle: " + actuator.Angle);
            }
        }
    }




    Actuators getDirectionActuator(Vector3 direction, List<Actuators> actuatorList)
    {
        float angle = Vector3.SignedAngle(direction, CameraMovement.embodiedDrone.transform.forward, Vector3.up);
        if(angle < 0) {
            angle += 360;
        }

        //FIUND THE closest actuator
        float minAngle = 360;
        Actuators closestActuator = null;
        foreach(Actuators actuator in actuatorList) {
            float diff = Math.Abs(actuator.Angle - angle);
            if(diff < minAngle) {
                minAngle = diff;
                closestActuator = actuator;
            }
        }

        return closestActuator;
    }

    void closeToWall()
    {
        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            foreach(Actuators actuator in actuatorsRange) {
                float angle = actuator.Angle;
                //make a ray and check if it hits something at 4m
                Vector3 direction = Quaternion.Euler(0, angle, 0) * CameraMovement.embodiedDrone.transform.forward;
                RaycastHit hit;
                if(Physics.Raycast(CameraMovement.embodiedDrone.transform.position, direction, out hit, 8)) {
                    if(hit.collider.gameObject.tag == "Obstacle") {
                        float distance = Vector3.Distance(CameraMovement.embodiedDrone.transform.position, hit.point);
                        int intensity = (int)MathF.Max(10 - 3*distance, 0);

                        actuator.dutyIntensity = intensity;
                        actuator.frequency = 2;
                    }
                }else {
                    actuator.dutyIntensity = 0;
                    actuator.frequency = 1;
                }
            }
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
