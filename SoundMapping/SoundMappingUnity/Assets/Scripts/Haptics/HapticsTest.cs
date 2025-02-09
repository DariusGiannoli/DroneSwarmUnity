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
    public bool Haptics_Obstacle
    {
        get
        {
            return LevelConfiguration._Haptics_Obstacle;
        }
    }
    public bool Haptics_Network
    {
        get
        {
            return LevelConfiguration._Haptics_Network;
        }
    }
    public bool Haptics_Forces
    {
        get
        {
            return LevelConfiguration._Haptics_Forces;
        }
    }
    public bool Haptics_Crash
    {
        get
        {
            return LevelConfiguration._Haptics_Crash;
        }
    }
    public bool Haptics_Controller
    {
        get
        {
            return LevelConfiguration._Haptics_Controller;
        }
    }
    
    
    
    List<Actuators> actuatorsBelly = new List<Actuators>();

    List<Actuators> lastDefined = new List<Actuators>();

    public List<Actuators> crashActuators = new List<Actuators>();

    public List<Actuators> actuatorsVariables = new List<Actuators>();

    public List<Actuators> actuatorNetwork = new List<Actuators>();

    public List<Actuators> actuatorsMovingPlane = new List<Actuators>();


    List<Actuators> finalList = new List<Actuators>();

    Dictionary<AnimatedActuator, IEnumerator> animatedActuators = new Dictionary<AnimatedActuator, IEnumerator>();


    public static bool gamePadConnected {
        get
        {
            return currentGamepad != null;
        }
    }

    Coroutine gamnePadCoroutine;

    #region HapticsGamePad

    private static Gamepad currentGamepad;


    #endregion
    
    public int sendEvery = 1000;
    // Update is called once per frame


    void Start()
    {

        //
        int[] mappingOlfati = Haptics_Forces ? new int[] {60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72} : new int[] {}; 
        
        int [] velocityMapping = {}; //relative mvt of the swarm

        Dictionary<int, int> angleMappingDict = new Dictionary<int, int> {
            {0, 30},
            {1, 80},
            {2, 130},
            {3, 180},
            {4, 230},
            {5, 280},
            {6, 350},
            {30, 210},
            {31, 210},
            {33, 270},
            {32, 270},
            {35, 330},
            {34, 330},
            {60, 15},
            {61, 45},
            {62, 75},
            {63, 100},
            {64, 125},
            {65, 150},
            {66, 175},
            {67, 200},
            {68, 230},
            {69, 255},
            {70, 285},
            {71, 315},
            {72, 345},
        };


        //obstacle in Range mapping
        int[] angleMapping =  Haptics_Obstacle ? new int[] {0,1,2,3,4,5,6}  : new int[] {};

        //drone crash mapping
        int[] crashMapping =  Haptics_Crash ? new int[] {0,1,2,3,4,5,6}  : new int[] {};;
        
        
        //layers movement on arm mapping
        int[] movingPlaneMapping =  Haptics_Network ? new int[] {90, 91, 92, 93, 94, 95, 96, 97, 98, 99} : new int[] {};

        for (int i = 0; i < angleMapping.Length; i++)
        {
            int adresse = angleMapping[i];
            int angle = angleMappingDict[adresse];
            actuatorsRange.Add(new PIDActuator(adresse:adresse, angle:angle,
                                                    kp:0, kd:150, referencevalue:0, 
                                                    refresh:CloseToWallrefresherFunction));
        }

        for (int i = 0; i < mappingOlfati.Length; i++)
        {
            int adresse = mappingOlfati[i];
            actuatorsVariables.Add(new RefresherActuator(adresse:adresse, angle:angleMappingDict[adresse], refresh:ForceActuator));
        }

        for (int i = 0; i < crashMapping.Length; i++)
        {
            int adresse = crashMapping[i];
            crashActuators.Add(new Actuators(adresse, 0));
        }

        for (int i = 0; i < velocityMapping.Length; i++)
        {
            int adresse = velocityMapping[i];
            actuatorsVariables.Add(new RefresherActuator(adresse:adresse, angle:angleMappingDict[adresse], refresh:SwarmVelocityRefresher));
        }

        for (int i = 0; i < movingPlaneMapping.Length; i++)
        {
            int adresse = movingPlaneMapping[i];
            actuatorsMovingPlane.Add(new AnimatedActuator(adresse:adresse, angle:adresse%10, refresh:movingPlaneRefresher));
        }

        finalList.AddRange(actuatorsRange);
       // finalList.AddRange(actuatorsBelly);
       // finalList.AddRange(actuatorsVariables);
        finalList.AddRange(crashActuators);
        finalList.AddRange(actuatorNetwork);

        finalList.AddRange(actuatorsVariables);
        finalList.AddRange(actuatorsMovingPlane);



        StartCoroutine(HapticsCoroutine());

        currentGamepad = Gamepad.current;
        if (currentGamepad == null)
        {
            Debug.LogWarning("No gamepad connected.");
        }else {
            currentGamepad.SetMotorSpeeds(0.0f, 0.0f);
        }
    }

 
    void Disable()
    {
        hapticsThread.Abort();
        currentGamepad.SetMotorSpeeds(0, 0);
    }
    
    #region Gamepad Crash Prediction
    
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        currentGamepad = Gamepad.current; // Store the currently connected gamepad (if any)
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad gamepad)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    Debug.Log("Controller Connected: " + gamepad.name);
                    currentGamepad = gamepad;
                    break;

                case InputDeviceChange.Removed:
                    Debug.Log("Controller Disconnected!");
                    
                    // Check if the removed device was the active gamepad
                    if (currentGamepad == gamepad)
                    {
                        currentGamepad = null;
                    }
                    break;
            }
        }
    }
    public void VibrateController(float leftMotor, float rightMotor, float duration)
    {
        if (gamePadConnected == false)
        {
            currentGamepad = Gamepad.current;
            return;
        }

        if (gamnePadCoroutine != null)
        {
            StopCoroutine(gamnePadCoroutine);
        }
        gamnePadCoroutine = StartCoroutine(vibrateControllerForTime(leftMotor, rightMotor, duration));
    }

    public IEnumerator vibrateControllerForTime(float leftMotor, float rightMotor, float duration)
    {
        if(!Haptics_Controller)
        {
            yield break;
        }
        currentGamepad.SetMotorSpeeds(leftMotor, rightMotor);
        yield return new WaitForSeconds(duration);
        currentGamepad.SetMotorSpeeds(0, 0);
        gamnePadCoroutine = null;
    }


    public void HapticsPrediction(Prediction pred)
    {
        if (currentGamepad == null)
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
            if(Haptics_Controller) {
                currentGamepad.SetMotorSpeeds(bestFractionOfPath, bestFractionOfPath);
            }
        }else {
            if(gamnePadCoroutine == null) {
                currentGamepad.SetMotorSpeeds(0, 0);
            }
        }
    } 

    #endregion

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

                                                //check if it is a AnimatedActuator
                        if(actuator is AnimatedActuator) {
                            animationHandler(last.dutyIntensity, (AnimatedActuator)actuator);
                        }

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
                if(actuator is AnimatedActuator) {
                    animationHandler(0,(AnimatedActuator)actuator);

                }
            }
        }
      //  print("FinalList: " + finalListNoDouble.Count + " toSendList: " + toSendList.Count + " lastDefined: " + lastDefined.Count);


        foreach(Actuators actuator in toSendList) {
            if(actuator is AnimatedActuator) {
                continue;
            }
            VibraForge.SendCommand(actuator.Adresse, (int)actuator.duty == 0 ? 0:1, (int)actuator.duty, (int)actuator.frequency);
        }
    }

    void animationHandler(int start, AnimatedActuator actuator)
    {
        if(animatedActuators.ContainsKey(actuator)) {
            StopCoroutine(animatedActuators[actuator]);
            actuator.stopAnimation();
        }

        actuator.defineAnimation(start, actuator.dutyIntensity);
        animatedActuators[actuator] = hapticAnimation(start, actuator);
        StartCoroutine(animatedActuators[actuator]);
    }


    // #region NetworkActuators
    
    // void getActuatorNetwork(RefresherActuator actuator)
    // {
    //     if(CameraMovement.embodiedDrone == null) {
    //         actuator.dutyIntensity = 0;
    //         actuator.frequency = 1;
    //         return;
    //     }

    //     Dictionary<int, int> neighbors = swarmModel.network.getLayersConfiguration();

    //     int totalNeighbors = 0;
    //     int firstOrder = 0;
    //     foreach (KeyValuePair<int, int> neighbor in neighbors)
    //     {
    //         totalNeighbors += neighbor.Value;
    //     }

    //     // first order is the key = 1
    //     if (neighbors.ContainsKey(2))
    //     {
    //         firstOrder = neighbors[2];
    //     }

    //     float proportion = (float)firstOrder / (float)totalNeighbors;

    //     if(actuator.Angle == 0)
    //     {
    //         if(proportion < 0.65f) { 
    //             actuator.dutyIntensity = 5;
    //             actuator.frequency = 1;
    //             return;
    //         }
    //     }
    //     else if(actuator.Angle == 1)
    //     {
    //         if(proportion < 0.4f) {
    //             actuator.dutyIntensity = 5;
    //             actuator.frequency = 1;
    //             return;
    //         }
    //     }
    //     else
    //     {
    //         if(proportion < 0.01f) {
    //             actuator.dutyIntensity = 5;
    //             actuator.frequency = 1;
    //             return;
    //         }
    //     }

    //     actuator.dutyIntensity = 0;
    //     actuator.frequency = 1;


    // }
    
    // #endregion

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
        
        List<Vector3> forces = swarmModel.swarmOlfatiForces;
        actuator.dutyIntensity = 0;
        actuator.frequency = 1;
        foreach(Vector3 forcesDir in forces) {
            float angle = Vector3.SignedAngle(forcesDir, CameraMovement.forward, -CameraMovement.up)-180;
            if(angle < 0) {
                angle += 360;
            }
            
            float diff = Math.Abs(actuator.Angle - angle);
            if(diff < 30) {
                actuator.dutyIntensity = Mathf.Max(actuator.dutyIntensity, (int)(forcesDir.magnitude / 2));
                actuator.frequency = 2;
            }
        }
    }

    #endregion

    #region ObstacleInRange
    void CloseToWallrefresherFunction(PIDActuator actuator)
    {
        List<Vector3> forces = swarmModel.swarmObstacleForces;

        actuator.dutyIntensity = 0;
        actuator.frequency = 1;

        foreach(Vector3 forcesDir in forces) {
            float angle = Vector3.SignedAngle(forcesDir, CameraMovement.forward, -CameraMovement.up)-180;
            if(angle < 0) {
                angle += 360;
            }
            
            float diff = Math.Abs(actuator.Angle - angle);
            if(diff < 35) {
                actuator.UpdateValue(forcesDir.magnitude);
            }
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

    #region swarmVelocityActuators
    void SwarmVelocityRefresher(RefresherActuator actuator)
    {
        Vector3 velDir  = swarmModel.swarmVelocityAvg;
        if(velDir.magnitude < 1) {
            actuator.dutyIntensity = 0;
            actuator.frequency = 1;
            return;
        }
        if(CameraMovement.embodiedDrone != null) { //carefull change only return the Vector


       
            float angle = Vector3.SignedAngle(velDir, CameraMovement.embodiedDrone.transform.forward, Vector3.up);
            if(angle < 0) {
                angle += 360;
            }
            
            float diff = Math.Abs(actuator.Angle - angle);
            if(diff < 30) {
                actuator.dutyIntensity = 4;
                actuator.frequency = 2;
                return;
            }
        }else{
       
            float angle = Vector3.SignedAngle(velDir, CameraMovement.cam.transform.up, -Vector3.up);
            if(angle < 0) {
                angle += 360;
            }
            
            float diff = Math.Abs(actuator.Angle - angle);
            if(diff < 30) {
                actuator.dutyIntensity = 4;
                actuator.frequency = 2;
                return;
            }
        }


        actuator.dutyIntensity = 0;
        actuator.frequency = 1;
    }

    #endregion

    #region NetworkActuators

    IEnumerator hapticAnimation(int oldActIntensity, Actuators newAct)
    {
        int startIntensity = oldActIntensity;
        int endIntensity = newAct.dutyIntensity;

        int currentIntensity = startIntensity;

        while(currentIntensity != endIntensity) {
            if(currentIntensity < endIntensity) {
                currentIntensity++;
            }else {
                currentIntensity--;
            }

            VibraForge.SendCommand(newAct.Adresse, (int)currentIntensity == 0 ? 0:1, (int)currentIntensity, (int)newAct.frequency);
            yield return new WaitForSeconds(0.1f);
        }

    }

    int delta = 3;
    IEnumerator hapticAnimation(Actuators newAct)
    {
        int startIntensity = 0;
        int endIntensity = newAct.dutyIntensity;

        int currentIntensity = startIntensity;

        while(currentIntensity != endIntensity) {
            if(currentIntensity < endIntensity) {
                currentIntensity = currentIntensity + delta > endIntensity ? endIntensity : currentIntensity + delta;
            }else {
                currentIntensity = currentIntensity - delta < endIntensity ? endIntensity : currentIntensity - delta;
            }
            VibraForge.SendCommand(newAct.Adresse, (int)currentIntensity == 0 ? 0:1, (int)currentIntensity, (int)newAct.frequency);
            yield return new WaitForSeconds(0.1f);
        }

    }
    void movingPlaneRefresher(RefresherActuator actuator)
    {
        // if(CameraMovement.embodiedDrone == null) {
        //     actuator.dutyIntensity = 0;
        //     actuator.frequency = 1;
        //     return;
        // }

        float score = swarmModel.swarmConnectionScore;
        int resol = 10;

        score*=resol;
        int angleToMove = (int)score;

        if(score <= 0)
        {
            actuator.dutyIntensity = 0;
            actuator.frequency = 1;
            return;
        }

        if(actuator.Angle == angleToMove) {
            actuator.dutyIntensity = (int)Mathf.Min(10, Mathf.Max(6, score));
            actuator.frequency = 3;
            return;
        }


        // if(score >= 10)
        // {
        //     actuator.dutyIntensity = 8;
        //     actuator.frequency = 3;
        //     return;
        // }


        actuator.dutyIntensity = 0;
        actuator.frequency = 1;

    }


    #endregion
}

public class AnimatedActuator: RefresherActuator
{
    int animationEnd = 0;
    int animationStart = 0;

    public void defineAnimation(int start, int end)
    {
        animationStart = start;
        animationEnd = end;
    }

    public void stopAnimation()
    {
        VibraForge.SendCommand(Adresse, 0, 0, 1);
    }
    public AnimatedActuator(int adresse, float angle, updateFunction refresh) : base(adresse, angle, refresh)
    {
    }
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

public class PIDActuator : Actuators // creae Ki
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
        dutyIntensity = Mathf.Max((int)(Kp * error - Kd * derivative), dutyIntensity);
        frequency = 4;
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


public class GamepadMonitor : MonoBehaviour
{
    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                    Debug.Log("Gamepad Connected: " + device.name);
                    break;
                case InputDeviceChange.Removed:
                    Debug.Log("Gamepad Disconnected!");
                    break;
            }
        }
    }
}