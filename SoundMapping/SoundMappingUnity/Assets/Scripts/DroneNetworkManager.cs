using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class DroneNetworkManager : MonoBehaviour
{
    public float neighborRadius = DroneController.neighborRadius*1.2f;
    public LayerMask obstacleMask;

    public List<GameObject> drones
    {
        get
        {
            List<GameObject> drones = new List<GameObject>();
            for (int i = 0; i < swarmModel.swarmHolder.transform.childCount; i++)
            {
                drones.Add(swarmModel.swarmHolder.transform.GetChild(i).gameObject);
            }
            return drones;
        }
    }

    private Dictionary<GameObject, List<GameObject>> adjacencyList = new Dictionary<GameObject, List<GameObject>>();
    public static HashSet<GameObject> largestComponent = new HashSet<GameObject>();

    public static List<GameObject> dronesInMainNetwork;

    public List<float> droneScores = new List<float>();

    void Start()
    {
        StartCoroutine(LateStart());
    }

    IEnumerator LateStart()
    {
        yield return new WaitForEndOfFrame();
        
        // Initialize the adjacency list
        foreach (GameObject drone in drones)
        {
            adjacencyList[drone] = new List<GameObject>();
        }

        StartCoroutine(updateNetwork());
    }


    IEnumerator updateNetwork()
    {
        BuildNetwork();
        FindLargestComponent();
        GetDronesScores(out droneScores);
        TimerStart();
        yield return new WaitForSeconds(GlobalConstants.NETWORK_REFRESH_RATE);
        StartCoroutine(updateNetwork());
    }

    void TimerStart()
    {
        //if any score is -1, start the timer
        foreach (var score in droneScores)
        {
            if(score < -0.5f)
            {
                this.GetComponent<Timer>().StartTimerNetwork();
                return;
            }
        }

        this.GetComponent<Timer>().StopTimerNetwork();
    }

    void OnDrawGizmos()
    {
        if (adjacencyList == null) return;

        Gizmos.color = Color.green;
        foreach (var drone in adjacencyList.Keys)
        {
            foreach (var neighbor in adjacencyList[drone])
            {
                Gizmos.DrawLine(drone.transform.position, neighbor.transform.position);
            }
        }
    }

    void BuildNetwork()
    {
        // Clear previous connections
        foreach (var drone in adjacencyList.Keys)
        {
            adjacencyList[drone].Clear();
        }

        // Build new connections
        foreach (GameObject drone in drones)
        {
            foreach (GameObject otherDrone in drones)
            {
                if (drone == otherDrone) continue;

                if (IsNeighbor(drone, otherDrone))
                {
                    adjacencyList[drone].Add(otherDrone);
                }
            }
        }
    }

    bool IsNeighbor(GameObject a, GameObject b)
    {
        float distance = Vector3.Distance(a.transform.position, b.transform.position);
        if (distance > neighborRadius) return false;

        Vector3 direction = b.transform.position - a.transform.position;
        Ray ray = new Ray(a.transform.position, direction);
        float rayDistance = direction.magnitude;

        if (Physics.Raycast(ray, rayDistance, obstacleMask))
        {
            // Obstacle in the way
            return false;
        }

        return true;
    }

    void FindLargestComponent()
    {
        HashSet<GameObject> visited = new HashSet<GameObject>();
        List<HashSet<GameObject>> components = new List<HashSet<GameObject>>();

        foreach (GameObject drone in drones)
        {
            if (!visited.Contains(drone))
            {
                HashSet<GameObject> component = new HashSet<GameObject>();
                Queue<GameObject> queue = new Queue<GameObject>();
                queue.Enqueue(drone);
                visited.Add(drone);

                while (queue.Count > 0)
                {
                    GameObject current = queue.Dequeue();
                    component.Add(current);

                    foreach (GameObject neighbor in adjacencyList[current])
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                components.Add(component);
            }
        }
        

        // Find the largest component
        largestComponent.Clear();
        int maxCount = 0;
        foreach (var component in components)
        {
            if(component.Contains(CameraMovement.embodiedDrone))
            {
                largestComponent = component;
                break;
            }

            if (component.Count > maxCount)
            {
                maxCount = component.Count;
                largestComponent = component;
            }
        }

        dronesInMainNetwork = new List<GameObject>();
        foreach (var drone in largestComponent)
        {
            dronesInMainNetwork.Add(drone.gameObject);
        }
    }

    public bool IsInMainNetwork(GameObject drone)
    {
        return largestComponent.Contains(drone);
    }

    public void GetDronesScores(out List<float> scores)
    {
        scores = new List<float>();

        foreach (GameObject drone in drones)
        {
            scores.Add(ComputeDroneNetworkScore(drone));
            HapticAudioManager.SetDroneNetworkScore(drone, scores[scores.Count - 1]);
        }
    }
    
    public float ComputeDroneNetworkScore(GameObject drone)
    {
        if (largestComponent.Contains(drone))
        {
            // Drone is part of the main network
            return 1.0f;
        }

        float minDistance = float.MaxValue;
        bool foundVisibleDroneInLargestComponent = false;

        foreach (GameObject otherDrone in largestComponent)
        {
            if (drone == otherDrone) continue;

            if (IsVisible(drone, otherDrone))
            {
                foundVisibleDroneInLargestComponent = true;                
                float distance = Vector3.Distance(drone.transform.position, otherDrone.transform.position);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }

        if (!foundVisibleDroneInLargestComponent)
        {
            // No visible drones in the main network
            return -1.0f;
        }

        if (minDistance <= neighborRadius)
        {
            return 1.0f;
        }
        else if (minDistance >= 3 * neighborRadius)
        {
            return -1.0f;
        }
        else
        {
            // Linear interpolation between neighborRadius and 3*neighborRadius
            float score = 1.0f - ((minDistance - neighborRadius) / (2 * neighborRadius));
            if (score < 0.0f) score = 0.0f;
            return score;
        }
    }


    // Helper function to check if two drones have an unobstructed line of sight
    bool IsVisible(GameObject a, GameObject b)
    {
        Vector3 direction = b.transform.position - a.transform.position;
        float distance = direction.magnitude;
        Ray ray = new Ray(a.transform.position, direction.normalized);

        if (Physics.Raycast(ray, distance, obstacleMask))
        {
            // Obstacle in the way
            return false;
        }

        return true;
    }
}
