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

    public static float _distanceDetection
    {
        get
        {
            return GameObject.FindGameObjectWithTag("GameManager").GetComponent<HapticsTest>().distanceDetection;
        }
    }
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

    private Coroutine hapticsCoroutine = null;

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

    public static bool send = false;

    #endregion
    
    public int sendEvery = 1000;
    // Update is called once per frame

    public static void lateStart()
    {
        // launch start function
        GameObject.FindGameObjectWithTag("GameManager").GetComponent<HapticsTest>().Start();
    }

    void Start()
    {
        print("HapticsTest Start");
        finalList = new List<Actuators>();
        actuatorsRange = new List<Actuators>();
        actuatorsVariables = new List<Actuators>();
        actuatorNetwork = new List<Actuators>();
        actuatorsMovingPlane = new List<Actuators>();
        crashActuators = new List<Actuators>();
        lastDefined = new List<Actuators>();
        animatedActuators = new Dictionary<AnimatedActuator, IEnumerator>();



        //
        int[] mappingOlfati = Haptics_Forces ? new int[] {0,1,2,3,120,121,122,123} : new int[] {}; 
    //    int[] mappingOlfati = Haptics_Forces ? new int[] {90,91,92,93,180,181,182,183} : new int[] {}; 
        
        int [] velocityMapping = {}; //relative mvt of the swarm

        Dictionary<int, int> angleMappingDict = new Dictionary<int, int> {
            {0, 160},{1, 115},{2, 65},{3, 20}, {120, 200}, {121, 245},{122, 295},{123, 340},
            {90, 160},{91, 115},{92, 65},{93, 20}, {210, 200}, {211, 245},{212, 295},{213, 340},
             {30, 160},{31, 115},{32, 65},{33, 20}, {150, 200}, {151, 245},{152, 295},{153, 340},
        };


        //obstacle in Range mapping
        int[] angleMapping =  Haptics_Obstacle ? new int[] {30,31,32,33,150,151,152,153}  : new int[] {};

        //drone crash mapping
        int[] crashMapping =  Haptics_Crash ? new int[] {4,5,124,125}  : new int[] {};
        print("Crash Mapping: " + crashMapping.Length);
        
        
        //layers movement on arm mapping
        int[] movingPlaneMapping =  Haptics_Network ? new int[] {60,61,62,63, 64, 65, 66, 67, 68, 69,
                                                                    180,181, 182, 183, 184, 185, 186, 187, 188, 189}
                                                                        : new int[] {};

        for (int i = 0; i < angleMapping.Length; i++)
        {
            int adresse = angleMapping[i];
            int angle = angleMappingDict.ContainsKey(adresse) ? angleMappingDict[adresse] : 0; 
            actuatorsRange.Add(new PIDActuator(adresse:adresse, angle:angleMappingDict[adresse],
                                                    kp:0f, kd:160, referencevalue:0, 
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
            actuatorsMovingPlane.Add(new RefresherActuator(adresse:adresse, angle:adresse%10, refresh:movingPlaneRefresher));
        }

        finalList.AddRange(actuatorsRange);
        finalList.AddRange(crashActuators);
        finalList.AddRange(actuatorNetwork);

        finalList.AddRange(actuatorsVariables);
        finalList.AddRange(actuatorsMovingPlane);

        if(hapticsCoroutine != null) {
            StopCoroutine(hapticsCoroutine);
        }


        hapticsCoroutine = StartCoroutine(HapticsCoroutine());

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
       // hapticsThread.Abort();
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

          //  sendCommands();

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
            if(diff < 45) {
                actuator.dutyIntensity = Mathf.Max(actuator.dutyIntensity, (int)(forcesDir.magnitude * 2));
                actuator.frequency = 1;
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
            if(actuator.Angle >= 0){
                float angle = Vector3.SignedAngle(forcesDir, CameraMovement.forward, -CameraMovement.up)-180;
                if(angle < 0) {
                    angle += 360;
                }


                
                float diff = Math.Abs(actuator.Angle - angle);
         //   print("Diff: " + diff); 


                if(diff < 40 || diff > 320) 
                {
                    actuator.UpdateValue(forcesDir.magnitude);
                    return;
                }
            }else{
                //gte the y component
                float y = forcesDir.y;
                if(Mathf.Abs(y) > 0) {
                    actuator.UpdateValue(y);
                    return;
                }
            }


        }

        actuator.UpdateValue(0);
    }

    #endregion

    #region crashActuators 
    void DroneCrashrefresher(RefresherActuator actuator)
    {
        return;
    }

    public void crash(bool reset )
    {
        print("Crash and reset " + reset);
        if(reset) {
            foreach(Actuators actuator in crashActuators) {
                actuator.dutyIntensity = 0;
                actuator.frequency = 1;

                actuator.sendValue();
            }
        }

        
        StartCoroutine(crashCoroutine());
    }

    public IEnumerator crashCoroutine()
    {

        foreach(Actuators actuator in crashActuators) {
            actuator.dutyIntensity = 10;
            actuator.frequency = 1;
            actuator.sendValue();
            print("Actuator: " + actuator.Adresse + " Duty: " + actuator.duty + " Frequency: " + actuator.frequency);
        }

        yield return new WaitForSeconds(1);

        foreach(Actuators actuator in crashActuators) {
            actuator.dutyIntensity = 0;
            actuator.frequency = 1;
            actuator.sendValue();
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


    int step = 4;
    IEnumerator hapticAnimation(int oldActIntensity, Actuators newAct)
    {
        int startIntensity = oldActIntensity;
        int endIntensity = newAct.dutyIntensity;

        int currentIntensity = startIntensity;

        while(currentIntensity != endIntensity) {
            if(currentIntensity < endIntensity) {
                currentIntensity = currentIntensity + step > endIntensity ? endIntensity : currentIntensity + step;
            }else {
                currentIntensity = currentIntensity - step < endIntensity ? endIntensity : currentIntensity - step;
            }

            VibraForge.SendCommand(newAct.Adresse, (int)currentIntensity == 0 ? 0:1, (int)currentIntensity, (int)newAct.frequency);
            yield return new WaitForSeconds(0.1f);
        }

    }

    IEnumerator hapticAnimation(Actuators newAct)
    {
        int startIntensity = 0;
        int endIntensity = newAct.dutyIntensity;

        int currentIntensity = startIntensity;

        while(currentIntensity != endIntensity) {
            if(currentIntensity < endIntensity) {
                currentIntensity = currentIntensity + step > endIntensity ? endIntensity : currentIntensity + step;
            }else {
                currentIntensity = currentIntensity - step < endIntensity ? endIntensity : currentIntensity - step;
            }
            VibraForge.SendCommand(newAct.Adresse, (int)currentIntensity == 0 ? 0:1, (int)currentIntensity, (int)newAct.frequency);
            yield return new WaitForSeconds(0.1f);
        }

    }
    void movingPlaneRefresher(RefresherActuator actuator)
    {

        float score = swarmModel.swarmConnectionScore;
        int resol = 10;

        score*=resol;
        int angleToMove = (int)score;


        if(score >= 9f)
        {
            if(actuator.Angle >= 8 )
            {
                actuator.dutyIntensity = 13;
                actuator.frequency = 3;
                return;
            }
         }

        if(score <= 0)
        {
            actuator.dutyIntensity = 0;
            actuator.frequency = 1;
            return;
        }

        if(actuator.Angle == angleToMove) {
            actuator.dutyIntensity = (int)Mathf.Min(14, Mathf.Max(8, score));
            actuator.frequency = 1;
            return;
        }





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
        sendValue();
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
        float error = newValue - referenceValue;
        float derivative = newValue - lastValue;

        lastValue = newValue;
        dutyIntensity = Mathf.Max((int)(Kp * error + Kd * derivative), dutyIntensity);

        frequency = 2;
    }

    override public void update()
    {
        refresherFunction(this);
        sendValue();
    }
}

public class Actuators
{
    public int Adresse { get; set; }
    public float Angle { get; set; }

    public int dutyIntensity = 0;
    public int frequency = 1;

    public int lastSendDuty = 0;
    public int lastSendFrequency = 0;


    public int duty
    {
        get{
            if(dutyIntensity > 14) {
                return 14;
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
        sendValue();
        return;
    }

    public virtual void sendValue()
    {
        if( lastSendFrequency != frequency || lastSendDuty != duty) {
            VibraForge.SendCommand(Adresse, (int)duty == 0 ? 0:1, (int)duty, (int)frequency);
            lastSendDuty = duty;
            lastSendFrequency = frequency;
      //      Debug.Log("Send Command: " + Adresse + " Duty: " + duty + " Frequency: " + frequency);
        }
    }


    public IEnumerator sendDelayedVal(float delay)
    {
        yield return new WaitForSeconds(delay);
        sendValue();

        yield return new WaitForSeconds(0.1f);
        HapticsTest.send = false;
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