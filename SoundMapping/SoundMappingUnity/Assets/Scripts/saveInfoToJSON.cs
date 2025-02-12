using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class saveInfoToJSON : MonoBehaviour
{
    public static SwarmState swarmData = new SwarmState();

    // Call this method whenever you want to record a new data point
    public static void saveDataPoint()
    {
        swarmData.saveDataPoint();
    }


    public static bool isSaving = false;
    

    public void exportData(bool force)
    {
        if (!isSaving)
        {
            isSaving = true;
            System.Threading.Thread saveThread = new System.Threading.Thread(() =>
            {
                saveDataThread(force);
                isSaving = false;
            });
            saveThread.Start();
        }
    }

    void saveDataThread(bool force)
    {
        string PID = SceneSelectorScript.pid;
        bool haptics = SceneSelectorScript.hapticsEnabled;
        bool order = SceneSelectorScript.order;
        int experimentNumber = SceneSelectorScript.experimentNumber;


        string forceString = force ? "dataForce_" : "";
        string hapticSring = haptics ? "1" : "0";
        string orderString = order ? "1" : "0";

        //convert dataSave into JSON
        string json = JsonUtility.ToJson(swarmData, true);

       // string date = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        string fileName =  forceString+PID+"_"+hapticSring+"_"+orderString+"_"+experimentNumber+".json";

        //Create a folder with the name of the PID
        if (!System.IO.Directory.Exists("./Assets/Data/"+PID))
        {
            System.IO.Directory.CreateDirectory("./Assets/Data/"+PID);
        }
        
        //Write the JSON file
        System.IO.File.WriteAllText("./Assets/Data/"+PID+"/"+fileName, json);

        // wait an extra 1s to make sure the file is written
        System.Threading.Thread.Sleep(500);

        isSaving = false;
    }
}



[System.Serializable]
public class SwarmState
{
    // We replace Dictionary with a list of (string, DroneState) pairs
    public List<DroneStateEntry> swarmState = new List<DroneStateEntry>();

    // Just as an example, let's initialize these
    public List<Vector3> alignment = new List<Vector3>();
    public List<float> desiredSeparation = new List<float>();

    public List<float> time = new List<float>();

    // Constants or static fields do not get serialized by default by JsonUtility
    // If you need them in the JSON, make them non-static. 
    public float maxSpeed = DroneFake.maxSpeed; 
    public float maxForce = DroneFake.maxForce;
    public float alpha = DroneFake.alpha;
    public float beta = DroneFake.beta;
    public float delta = DroneFake.delta;
    public float avoidanceRadius = DroneFake.avoidanceRadius;  
    public float avoidanceForce = DroneFake.avoidanceForce;   
    public float droneRadius = DroneFake.droneRadius;
    public float dampingFactor = DroneFake.dampingFactor;


    public void saveDataPoint()
    {
        // Example: gather your drones
        List<DroneFake> drones = swarmModel.drones;
        NetworkCreator network = swarmModel.network;

        foreach(DroneFake drone in drones)
        {
            // Find or create the DroneStateEntry for this drone
            DroneStateEntry entry = swarmState.Find(x => x.droneId == drone.idS);
            if (entry == null)
            {
                entry = new DroneStateEntry();
                entry.droneId = drone.idS;
                entry.droneState = new DroneState();
                swarmState.Add(entry);
            }

            // Append data to that DroneState
            entry.droneState.add(drone, network.adjacencyList[drone]);
        }

        desiredSeparation.Add(DroneFake.desiredSeparation);
        alignment.Add(MigrationPointController.alignementVector);
        time.Add(Timer.elapsedTime);


    }

}

// This is our "key-value pair" entry
[System.Serializable]
public class DroneStateEntry
{
    public string droneId;
    public DroneState droneState;
}

[System.Serializable]
public class DroneState
{
    public List<Vector3> position = new List<Vector3>();
    public List<Vector3> velocity = new List<Vector3>();
    public List<Vector3> obstacleAvoidance = new List<Vector3>();
    public List<Vector3> olfatiSaber = new List<Vector3>();

    public List<Vector3> alignment = new List<Vector3>();
    public List<bool> embodied = new List<bool>();
    public List<bool> selected = new List<bool>();

    public List<string> network = new List<string>();

    public void add(DroneFake drone, List<DroneFake> connected)
    {

        position.Add(drone.position);
        velocity.Add(drone.velocity);
        embodied.Add(drone.embodied);
        selected.Add(drone.selected);
        obstacleAvoidance.Add(drone.lastObstacle);
        olfatiSaber.Add(drone.lastOlfati);
        alignment.Add(drone.lastAllignement);

        addNetwork(connected);
    }

    private void addNetwork(List<DroneFake> connected)
    {
        // Example
        string networkS = "";
        foreach (DroneFake d in connected)
        {
            networkS += d.id + "-";
        }
        network.Add(networkS);
    }
}


