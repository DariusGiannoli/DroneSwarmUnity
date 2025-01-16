using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class HapticsTest : MonoBehaviour
{
    #region ObstalceInRange
    public int dutyIntensity = 4;
    public int frequencyInit = 1;
    public float distanceDetection = 3;
    Thread hapticsThread;
    List<ObstacleInRange> obstacles = new List<ObstacleInRange>();
    Vector3 forwardVector = new Vector3(0, 0, 1);   
    Vector3 rightVector = new Vector3(1, 0, 0);

    List<Actuators> actuatorsRange = new List<Actuators>();

    #endregion

    List<Actuators> actuatorsBelly = new List<Actuators>();

    List<Actuators> lastDefined = new List<Actuators>();

    public List<Actuators> crashActuators = new List<Actuators>();

    public List<Actuators> actuatorsVariables = new List<Actuators>();

    public List<Actuators> actuatorNetwork = new List<Actuators>();

    List<Actuators> finalList = new List<Actuators>();


    #region HapticsGamePad

    private Gamepad gamepad;



    #endregion
    
    public int sendEvery = 1000;
    // Update is called once per frame
    void Start()
    {
        int[] mappingOlfati = {1,2,4,5,7,8,10}; 
        int[] mappingObstacle = {0,3,6,9};
        int[] crashMapping = {11,12,13,14,15};
        int[] networkMapping = {};

        for (int i = 0; i < networkMapping.Length; i++)
        {
            int adresse = networkMapping[i];
            actuatorsBelly.Add(new Actuators(adresse, 310/10 * adresse));
        }

        for (int i = 0; i < mappingObstacle.Length; i++)
        {
            int adresse = mappingObstacle[i];
            actuatorsRange.Add(new PIDActuator(adresse:adresse, angle:310/10 * adresse,
                                                    kp:0, kd:150, referencevalue:distanceDetection, 
                                                    refresh:CloseToWallrefresherFunction));
        }

        for (int i = 0; i < mappingOlfati.Length; i++)
        {
            int adresse = mappingOlfati[i];
            actuatorsVariables.Add(new RefresherActuator(adresse:adresse, angle:310/10 * adresse, refresh:ForceActuator));
        }

        for (int i = 0; i < crashMapping.Length; i++)
        {
            int adresse = crashMapping[i];
            crashActuators.Add(new Actuators(adresse, 0));
        }


        finalList.AddRange(actuatorsRange);
        finalList.AddRange(actuatorsBelly);
        finalList.AddRange(actuatorsVariables);
        finalList.AddRange(crashActuators);
        finalList.AddRange(actuatorNetwork);



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
            foreach(Actuators actuator in finalList) {
                actuator.update();
            }

            sendCommands();

            yield return new WaitForSeconds(sendEvery / 1000);
        }
    }

    void sendCommands()
    {

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

    #region NetworkActuators
    void getActuatorNetwork()
    {
        if(swarmModel.network == null ) {
            return;
        }

        foreach(Actuators actuator in actuatorNetwork) {
            actuator.dutyIntensity = 0;
            actuator.frequency = 1;
        }

        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            Dictionary<int, int> networkConnection = NetworkRepresentation.neighborsRep;
            int totalConnections = 0;
            foreach(KeyValuePair<int, int> connection in networkConnection) {
                totalConnections += connection.Value;
            }

            int firstOrder = 0;
            if(networkConnection.ContainsKey(1)) {
                firstOrder = networkConnection[1];
            }

            //map from 0 to 10  
            float proportion = 1 - Mathf.Min(2*(float)firstOrder / (float)totalConnections, 1);
            print("Proportion: " + proportion);

            foreach(Actuators actuator in actuatorNetwork) {
                if(actuator.Angle < proportion * 310) {
                    print("Hello");
                    actuator.dutyIntensity = 4;
                    actuator.frequency = 1;
                }
            }

        }

        
    }

    #endregion

    #region ForceActuators

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
    
    void ForceActuator(RefresherActuator actuator)
    {   
        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector3
            DroneFake main = CameraMovement.embodiedDrone.GetComponent<DroneController>().droneFake;
            Vector3 forcesDir  = main.getHapticVector();
       
            float angle = Vector3.SignedAngle(forcesDir, CameraMovement.embodiedDrone.transform.forward, Vector3.up);
            if(angle < 0) {
                angle += 360;
            }
            
            float diff = Math.Abs(actuator.Angle - angle);
            if(diff < 20) {
                actuator.dutyIntensity = (int)(forcesDir.magnitude / 2);
                actuator.frequency = 1;
                return;
            }
        }


        actuator.dutyIntensity = 0;
        actuator.frequency = 1;

    }

    #endregion

    #region ObstacleInRange
    void CloseToWallrefresherFunction(PIDActuator actuator)
    {
        if(CameraMovement.embodiedDrone == null) { 
            actuator.UpdateValue(distanceDetection);
            return;
        }

        float angle = actuator.Angle;
        //make a ray and check if it hits something at 4m
        Vector3 direction = Quaternion.Euler(0, angle, 0) * CameraMovement.embodiedDrone.transform.forward;
        RaycastHit hit;

        if(Physics.Raycast(CameraMovement.embodiedDrone.transform.position, direction, out hit, distanceDetection + swarmModel.droneRadius)) {
            if(hit.collider.gameObject.tag == "Obstacle") {
                float distance = Vector3.Distance(CameraMovement.embodiedDrone.transform.position, hit.point) - swarmModel.droneRadius;
                actuator.UpdateValue(distance);
            }
        }else {
            actuator.UpdateValue(distanceDetection);
        }
    }

    #endregion

    #region crashActuators 
    void DroneCrashrefresher(RefresherActuator actuator)
    {
        return;
    }

    public void crash(bool reset )
    {
        if(reset) {
            foreach(Actuators actuator in crashActuators) {
                actuator.dutyIntensity = 0;
                actuator.frequency = 1;
            }
            sendCommands();
        }

        
        StartCoroutine(crashCoroutine());
    }

    public IEnumerator crashCoroutine()
    {

        foreach(Actuators actuator in crashActuators) {
            actuator.dutyIntensity = 10;
            actuator.frequency = 1;
        }

        yield return new WaitForSeconds(1);

        foreach(Actuators actuator in crashActuators) {
            actuator.dutyIntensity = 0;
            actuator.frequency = 1;
        }
    }
    
    #endregion
}

public class RefresherActuator: Actuators
{
    public delegate void updateFunction(RefresherActuator actuator);
    public updateFunction refresherFunction { get; set; }

    public RefresherActuator(int adresse, float angle, updateFunction refresh) : base(adresse, angle)
    {
        this.refresherFunction = refresh;
    }

    public override void update()
    {
        refresherFunction(this);
    }
}

public class PIDActuator : Actuators
{
    public float Kp { get; set; }
    public float Kd { get; set; }

    public float referenceValue { get; set; }

    public float lastValue = 0;

    public delegate void updateFunction(PIDActuator actuator);
    public updateFunction refresherFunction { get; set; }

    public PIDActuator(int adresse, float angle, float kp, float kd, float referencevalue, updateFunction refresh) : base(adresse, angle)
    {
        this.Kp = kp;
        this.Kd = kd;
        this.referenceValue = referencevalue;
        this.refresherFunction = refresh;
    }

    public void UpdateValue(float newValue)
    {
        float error = referenceValue - newValue;
        float derivative = newValue - lastValue;

        lastValue = newValue;
        dutyIntensity = (int)(Kp * error - Kd * derivative);
    }

    override public void update()
    {
        refresherFunction(this);
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
            }else if (dutyIntensity < 0) {
                return 0;
            }else {
                return dutyIntensity;
            }
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

    public void forceIntensity(float force)
    {
        dutyIntensity = (int)force;
        frequency = 1;
    }

    public virtual void update()
    {
        return;
    }
}
