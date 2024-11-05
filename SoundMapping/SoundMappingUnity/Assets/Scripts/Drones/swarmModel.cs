using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class swarmModel : MonoBehaviour
{
    public GameObject swarmHolder;
    public GameObject dronePrefab;
    public int numDrones = 10;
    public float spawnRadius = 10f;
    public float spawnHeight = 10f;

    void Awake()
    {
        for (int i = 0; i < numDrones; i++)
        {
            Vector3 spawnPosition = new Vector3(Random.Range(-spawnRadius, spawnRadius), spawnHeight, Random.Range(-spawnRadius, spawnRadius));
            GameObject drone = Instantiate(dronePrefab, spawnPosition, Quaternion.identity);
            drone.transform.parent = swarmHolder.transform;
            drone.name = "Drone"+i.ToString();
        }

    }

    private string getDroneInfo(GameObject drone)
    {
        Vector3 position = drone.transform.position;
        //Vector3 velocity = drone.GetComponent<Rigidbody>().velocity;
        Vector3 velocity = new Vector3(0, 0, 0); //velocity is not used in the server


        //make like JSON for position and velocity
        string droneInfo = "{Position: " + position.ToString() + ",";
        droneInfo += "Velocity: " + velocity.ToString() + "}";

        return droneInfo;
    }

    public DataEntry getSwarmInfo()
    {
        string swarmInfo = "[";
        for (int i = 0; i < numDrones; i++)
        {
            GameObject drone = swarmHolder.transform.GetChild(i).gameObject;
            string droneInfo = "{\"Drone"+i.ToString()+"\": " + getDroneInfo(drone) + "}";
            swarmInfo += droneInfo;
            if (i < numDrones - 1)
            {
                swarmInfo += ",";
            }
        }
        swarmInfo += "]";

        return new DataEntry("swarm", swarmInfo);
    }
}
