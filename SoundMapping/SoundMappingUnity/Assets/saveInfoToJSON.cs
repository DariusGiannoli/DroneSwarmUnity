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

    private void OnApplicationQuit()
    {
        string json = JsonUtility.ToJson(swarmData, true); // 'true' for pretty-print
        File.WriteAllText(Path.Combine(Application.dataPath, "data/swarmData.json"), json);

        Debug.Log("Data saved to JSON file");
    }
}

[System.Serializable]
public class SwarmState
{
    // We replace Dictionary with a list of (string, DroneState) pairs
    public List<DroneStateEntry> swarmState = new List<DroneStateEntry>();

    // Just as an example, let's initialize these
    public List<string> networks = new List<string>();
    public List<Vector3> alignment = new List<Vector3>();
    public List<float> desiredSeparation = new List<float>();

    // Constants or static fields do not get serialized by default by JsonUtility
    // If you need them in the JSON, make them non-static. 
    public float maxSpeed = 0f; 
    public float maxForce = 0f; 
    public float neighborRadius = 10f; 
    public float alpha = 1.5f; 
    public float beta = 1.0f;  
    public float delta = 1.0f; 
    public float avoidanceRadius = 2f;    
    public float avoidanceForce = 10f;     
    public float droneRadius = 0.17f; 
    public float dampingFactor = 0.96f; 

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

        Debug.Log("Data point saved");
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
