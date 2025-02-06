using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FischlWorks_FogWar;
using Unity.VisualScripting;
using UnityEngine;

public class swarmModel : MonoBehaviour
{

    #region Parameters

    public bool saveData = false;
    public bool needToSpawn = false;
    DataSave dataSave = new DataSave();
    public static GameObject swarmHolder;
    public GameObject dronePrefab;
    public int numDrones = 10;
    public float spawnRadius = 10f;
    public static float spawnHeight = 5f;

    public float lastObstacleAvoidance = -1f;


    public float maxSpeed = 5f;
    public float maxForce = 10f;

    public static int extraDistanceNeighboor = 3;
    public static float neighborRadius
    {
        get
        {
            //return 3*desiredSeparation;
            return Mathf.Max(1.2f * desiredSeparation, desiredSeparation + extraDistanceNeighboor);
        }
    }

    public static float desiredSeparation = 3f;
    public float alpha = 1.5f; // Separation weight
    public float beta = 1.0f;  // Alignment weight
    public float delta = 1.0f; // Migration weight

    public float avoidanceRadius = 2f;     // Radius for obstacle detection
    public float desiredSeparationObs = 3f;
    public float avoidanceForce = 10f;     // Strength of the avoidance force

    public static float droneRadius = 0.17f;      // Radius of the drone
    public LayerMask obstacleLayer;        // Layer mask for obstacles

    public csFogWar fogWar;

    public int PRIORITYWHENEMBODIED = 15;
    public float dampingFactor = 0.98f;

    public static NetworkCreator network;

    public static float minDistance = float.MaxValue;

    public static List<DroneFake> dronesInMainNetwork
    {
        get
        {
            List<DroneFake> drones = new List<DroneFake>();
            if (network == null)
            {
                return drones;
            }

            foreach (DroneFake drone in network.largestComponent)
            {
                drones.Add(drone);
            }

            return drones;
        }
    }


    public static Vector3 swarmVelocityAvg = Vector3.zero;

    public static float swarmConnectionScore = 0f;
    public static int numberOfDroneDiscionnected
    {
        get
        {
            return swarmDisconnection.dronesID.Count;
        }
    }
    public static int numberOfDroneCrashed
    {
        get
        {
            return Timer.numberDroneDied;
        }
    }
    public static float swarmAskingSpreadness = 0f;
    
    public static List<DroneFake> drones = new List<DroneFake>();

        [Header("Gizmos")]
        public bool showObstacleGizmos = false;
        public bool showNetworkGizmos = false;
        public bool showNetworkConnexionVulnerability = false;
        public bool showNetworkConnectivity = false;
        public bool showDroneObstacleForces = false;
        public bool showDroneOlfatiForces = false;
        public bool showDroneAllignementForces = false;



        public bool showSwarmObstacleForces = false;
        public bool showSwarmOlfati = false;


    #endregion


    public static List<Vector3> swarmObstacleForces = new List<Vector3>();
    public static List<Vector3> swarmOlfatiForces = new List<Vector3>();

    void Awake()
    {
        PRIORITYWHENEMBODIED = (int)(numDrones/3.5f);
        swarmHolder = GameObject.FindGameObjectWithTag("Swarm");
        spawn();
    }


    void Start()
    {
        Application.targetFrameRate = 30; // Set the target frame rate to 30 FPS

    }

    void refreshParameters()
    {
        DroneFake.maxForce = maxForce;
        DroneFake.maxSpeed = maxSpeed;
        DroneFake.desiredSeparation = desiredSeparation;
        DroneFake.alpha = alpha;
        DroneFake.beta = beta;
        DroneFake.delta = delta;
        DroneFake.avoidanceRadius = avoidanceRadius;
        DroneFake.avoidanceForce = avoidanceForce;
        DroneFake.droneRadius = droneRadius;
        DroneFake.neighborRadius = neighborRadius;
        DroneFake.obstacleLayer = obstacleLayer;
        DroneFake.PRIORITYWHENEMBODIED = PRIORITYWHENEMBODIED;
        DroneFake.dampingFactor = dampingFactor;
        DroneFake.spawnHeight = spawnHeight;
        DroneFake.desiredSeparationObs = desiredSeparationObs;
    }

    void FixedUpdate()
    {

        refreshSwarm();

        if(CameraMovement.embodiedDrone != null)
        {
            if (saveData)
            {
                dataSave.saveData(CameraMovement.embodiedDrone.GetComponent<DroneController>().droneFake);
            }
        }

        UpdateSwarmForces();
        UpdateSwarmInfos();

        swarmSeparation(3); // swarm connexion score
        
        if(!showNetworkConnexionVulnerability)
        {
            swarmDisconnection.CheckDisconnection();
        }

        swarmAskingShrink();
    }


    void refreshSwarm()
    {
        refreshParameters();
        if (Input.GetKeyDown(KeyCode.R))
        {
            fogWar.ResetMapAndFogRevealers();
            spawn();

            this.GetComponent<Timer>().Restart();
        }

        network = new NetworkCreator(drones);
        network.refreshNetwork();
        Dictionary<int, int> layers = network.getLayersConfiguration();

        this.GetComponent<NetworkRepresentation>().UpdateNetworkRepresentation(layers);

        ClosestPointCalculator.selectObstacle(drones); // update list of obstacle 

        foreach (DroneFake drone in drones)
        {
            drone.ComputeForces(MigrationPointController.alignementVector, network);
            drone.score = network.IsInMainNetwork(drone) ? 1.0f : 0.0f;
        }

        foreach (Transform drone in swarmHolder.transform)
        {
            drone.GetComponent<DroneController>().droneFake.UpdatePositionPrediction(1);
            if (drone.GetComponent<DroneController>().droneFake.hasCrashed)
            {
                drone.GetComponent<DroneController>().crash();
            }
        }

    }

    void spwanless()
    {
        drones.Clear();
        int i = 0;
        foreach (Transform drone in swarmHolder.transform)
        {
            fogWar.AddFogRevealer(drone, 1, true);
            drone.GetComponent<DroneController>().droneFake = new DroneFake(drone.transform.position, Vector3.zero, false, i);
            drones.Add(drone.GetComponent<DroneController>().droneFake);
            i++;
        }

        this.GetComponent<HapticAudioManager>().Reset();

        return;
    }

    void spawn()
    {
        desiredSeparation = 3f;
        if(!needToSpawn)
        {
            spwanless();
            return;
        }

        GameObject[] dronesToDelete = GameObject.FindGameObjectsWithTag("Drone");
        //kill all drones
        foreach (GameObject drone in dronesToDelete)
        {
            Destroy(drone.gameObject);
        }

        drones.Clear();


        for (int i = 0; i < numDrones; i++)
        {
            //spawn on a circle
            Vector3 spawnPosition = new Vector3(spawnRadius * Mathf.Cos(i * 2 * Mathf.PI / numDrones), spawnHeight + UnityEngine.Random.Range(-0.5f, 0.5f), spawnRadius * Mathf.Sin(i * 2 * Mathf.PI / numDrones));
            
            GameObject drone = Instantiate(dronePrefab, spawnPosition, Quaternion.identity);

            drone.GetComponent<DroneController>().droneFake = new DroneFake(spawnPosition, Vector3.zero, false, i);

            fogWar.AddFogRevealer(drone.transform, 1, true);

            drones.Add(drone.GetComponent<DroneController>().droneFake);

            drone.transform.parent = swarmHolder.transform;
            drone.name = "Drone"+i.ToString();
        }

        //this.GetComponent<HapticAudioManager>().Reset();
       // this.GetComponent<DroneNetworkManager>().Reset();
    }

    public void RemoveDrone(GameObject drone)
    {
        if (drone.transform.parent == swarmHolder.transform)
        {
            drone.gameObject.SetActive(false);
            drone.transform.parent = null;
            //this.GetComponent<CameraMovement>().resetFogExplorers();
        }

        this.GetComponent<Timer>().DroneDiedCallback();

        if (swarmHolder.transform.childCount == 0)
        {
            this.GetComponent<Timer>().Restart();
            fogWar.ResetMapAndFogRevealers();
            spawn();
        }

        drones.Remove(drone.GetComponent<DroneController>().droneFake);
    }

    public void UpdateSwarmForces()
    {
        List<Vector3> allForcesObstacle = new List<Vector3>();
        List<Vector3> allForcesOlfati = new List<Vector3>();

        swarmObstacleForces.Clear();
        swarmOlfatiForces.Clear();

        if(CameraMovement.embodiedDrone == null)
        {
            foreach (DroneFake drone in network.drones)
            {
                allForcesObstacle.AddRange(drone.lastObstacleForces);
                allForcesOlfati.Add(drone.lastOlfati);
            }

            swarmObstacleForces = ForceClusterer.getForcesObstacle(allForcesObstacle, 40f, (int)drones.Count/3, 2);
            swarmOlfatiForces = ForceClusterer.getOlfatiForces(allForcesOlfati, 35f, (int)drones.Count/5, 1);
        }else{
            //get embodied drone infos
            swarmOlfatiForces.Add(CameraMovement.embodiedDrone.GetComponent<DroneController>().droneFake.lastOlfati);
            swarmObstacleForces = CameraMovement.embodiedDrone.GetComponent<DroneController>().droneFake.lastObstacleForces;
        }
    }
    
    public void UpdateSwarmInfos()
    {
        
        swarmVelocityAvg = Vector3.zero;
        foreach (DroneFake drone in dronesInMainNetwork)
        {
            swarmVelocityAvg += drone.velocity;
        }
        swarmVelocityAvg /= dronesInMainNetwork.Count;


        saveInfoToJSON.saveDataPoint();

        float minDistance = float.MaxValue;

        // Loop through each drone and its neighbors
        foreach (DroneFake drone in dronesInMainNetwork)
        {
            foreach (DroneFake neighbour in network.GetNeighbors(drone))
            {
                // Calculate distance once to avoid repeated calls
                float distance = Vector3.Distance(drone.position, neighbour.position);

                // Update global minimum distance if we find a smaller one
                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }
        }
        // Update the global minimum distance
        swarmModel.minDistance = minDistance;
    }
    //void on Guizmos
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        List<Obstacle> obstacles = ClosestPointCalculator.obstaclesInRange;
        if (obstacles == null)
        {
            return;
        }


        // obstacle loaded for compurtation
        if (showObstacleGizmos)
        {
            foreach (Obstacle obstacle in obstacles)
            {
                Gizmos.DrawWireSphere(obstacle.centerObs, 0.1f);
            }
        }

        if(showNetworkConnexionVulnerability)
        {
            swarmDisconnection.CheckDisconnection();
        }


        //draw the network
        if (network == null)
        {
            return;
        }


        if(CameraMovement.embodiedDrone == null)
        {
            if(showSwarmObstacleForces){
                foreach(Vector3 force in swarmObstacleForces)
                {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawLine(Camera.main.transform.position, Camera.main.transform.position + force * 10);
                }
            }

            if(showSwarmOlfati){
                foreach(Vector3 force in swarmOlfatiForces)
                {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(Camera.main.transform.position, Camera.main.transform.position + force * 10);
                }
            }
        }

    }

    void swarmAskingShrink()
    {
        List<float> scores = new List<float>();
        foreach (DroneFake drone in network.drones)
        {
            if(!network.IsInMainNetwork(drone))
            {
                continue;
            }


            List<DroneFake> neighbors = network.GetNeighbors(drone);

            if (neighbors.Count == 0)
            {
                continue;
            }
            neighbors = neighbors.OrderBy(d => Vector3.Distance(d.position, drone.position)).ToList();
            int number = (int)(network.drones.Count * 0.3f);
            if (number > neighbors.Count)
            {
                number = neighbors.Count;
            }
            List<DroneFake> mostVulnerable = neighbors.GetRange(0, number);

            float ratioAverageDistance = mostVulnerable.Average(d => Vector3.Distance(d.position, drone.position))/desiredSeparation;

            float score = ratioAverageDistance;

            scores.Add(score);
        }

        scores = scores.OrderBy(s => s).ToList();
        float finalScore = scores.GetRange(0, (int)(scores.Count * 0.3f)).Average();
        //0.75 max 0.55 min
        //min Max
        finalScore = (finalScore - 0.55f) / (0.75f - 0.55f);
        finalScore = Mathf.Min(finalScore, 1f);
        finalScore = 1-Mathf.Max(finalScore, 0f);


        BrownToBlueNoise.AnalyseShrinking(finalScore);
        swarmAskingSpreadness = finalScore;
    }

    void swarmSeparation(int numberOfCut)
    {
            List<DroneFake> dronesInDirection = network.drones.OrderBy(d => Vector3.Dot(d.position, MigrationPointController.alignementVector.normalized)).ToList();

            // Split the drones into the specified number of groups
            List<List<DroneFake>> droneGroups = new List<List<DroneFake>>();
            int groupSize = dronesInDirection.Count / numberOfCut;
            for (int i = 0; i < numberOfCut; i++)
            {
                int start = i * groupSize;
                int count = (i == numberOfCut - 1) ? dronesInDirection.Count - start : groupSize;
                droneGroups.Add(dronesInDirection.GetRange(start, count));
            }

            // Get average position and velocity of each group
            List<(Vector3 position, Vector3 velocity)> averages = new List<(Vector3 position, Vector3 velocity)>();
            foreach (var group in droneGroups)
            {
                var avg = group.Aggregate((Vector3.zero, Vector3.zero), (acc, d) => (acc.Item1 + d.position / group.Count, acc.Item2 + d.velocity / group.Count));
                averages.Add(avg);
            }

            // Draw Gizmos for each group
            List<float> scores = new List<float>();
            for (int i = 0; i < averages.Count; i++)
            {
                float diffPos = (i < averages.Count - 1) ? Vector3.Dot(averages[i+1].position, MigrationPointController.alignementVector.normalized) - Vector3.Dot(averages[i].position, MigrationPointController.alignementVector.normalized) : 0;   
                
                float score = 2f*diffPos/desiredSeparation - 1;
                scores.Add(score);

                if(showNetworkConnexionVulnerability)
                {
                    Gizmos.color = (i < averages.Count - 1) ? Color.Lerp(Color.green, Color.red,  score) : Color.green;
                    Gizmos.DrawSphere(averages[i].position, 0.5f);
                }
            }

            swarmConnectionScore = Mathf.Max(Mathf.Min(scores.Max(), 1f), 0f);

    }

    void OnApplicationQuit()
    {   
        if (saveData)
        {

            print("Saving data");
            //convert dataSave into JSON
            string json = JsonUtility.ToJson(dataSave);
            string fileName = System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json";
            System.IO.File.WriteAllText("./Assets/Data/"+fileName, json);
        }

    }
}
