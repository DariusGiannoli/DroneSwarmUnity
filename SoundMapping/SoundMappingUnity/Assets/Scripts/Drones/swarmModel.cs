using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FischlWorks_FogWar;
using Unity.VisualScripting;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public class swarmModel : MonoBehaviour
{

    public bool saveData = false;
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
            return Mathf.Max(desiredSeparation * 1.2f, desiredSeparation + extraDistanceNeighboor);
        }
    }

    public static float desiredSeparation = 3f;
    public float alpha = 1.5f; // Separation weight
    public float beta = 1.0f;  // Alignment weight
    public float delta = 1.0f; // Migration weight

    public float avoidanceRadius = 2f;     // Radius for obstacle detection
    public float avoidanceForce = 10f;     // Strength of the avoidance force

    public static float droneRadius = 0.17f;      // Radius of the drone
    public LayerMask obstacleLayer;        // Layer mask for obstacles

    public csFogWar fogWar;

    public int PRIORITYWHENEMBODIED = 15;
    public float dampingFactor = 0.98f;

    public static NetworkCreator network;

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


    public List<DroneFake> drones = new List<DroneFake>();

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
    }

    void FixedUpdate()
    {
        refreshParameters();
        if (Input.GetKeyDown(KeyCode.R))
        {
            spawn();
            this.GetComponent<Timer>().Restart();
        }

        network = new NetworkCreator(drones);
        network.refreshNetwork();
        Dictionary<int, int> layers = network.getLayersConfiguration();

        //update the network representation
        this.GetComponent<NetworkRepresentation>().UpdateNetworkRepresentation(layers);

        ClosestPointCalculator.selectObstacle(drones);
        //draw gizmos for each obstacleInRange

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

        if(CameraMovement.embodiedDrone != null)
        {
            if (saveData)
            {
                dataSave.saveData(CameraMovement.embodiedDrone.GetComponent<DroneController>().droneFake);
            }
        }
    }

    void spawn()
    {
        fogWar.ResetMapAndFogRevealers();

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
            Vector3 spawnPosition = new Vector3(spawnRadius * Mathf.Cos(i * 2 * Mathf.PI / numDrones), spawnHeight + Random.Range(-0.5f, 0.5f), spawnRadius * Mathf.Sin(i * 2 * Mathf.PI / numDrones));
            
            GameObject drone = Instantiate(dronePrefab, spawnPosition, Quaternion.identity);

            drone.GetComponent<DroneController>().droneFake = new DroneFake(spawnPosition, Vector3.zero, false);

            fogWar.AddFogRevealer(drone.transform, 5, true);

            drones.Add(drone.GetComponent<DroneController>().droneFake);

            drone.transform.parent = swarmHolder.transform;
            drone.name = "Drone"+i.ToString();
        }

        this.GetComponent<HapticAudioManager>().Reset();
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
            spawn();
        }

        drones.Remove(drone.GetComponent<DroneController>().droneFake);
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

        foreach (Obstacle obstacle in obstacles)
        {
            Gizmos.DrawWireSphere(obstacle.centerObs, 0f);
        }


        //draw the network
        if (network == null)
        {
            return;
        }

        foreach (DroneFake drone in network.drones)
        {
            foreach (DroneFake neighbour in network.GetNeighbors(drone))
            {
                if(neighbour.layer == 1 || drone.layer == 1)
                {
                    Gizmos.color = Color.red;
                }else if(neighbour.layer == 2 || drone.layer == 2)
                {
                    Gizmos.color = Color.green;
                }
                else if (neighbour.layer == 3 || drone.layer == 3)
                {
                    Gizmos.color = Color.blue;
                }
                else
                {
                    Gizmos.color = Color.black;
                }
                Gizmos.DrawLine(drone.position, neighbour.position);
            }
        }
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


public class DataSave
{
    public List<Vector3> olfatiData = new List<Vector3>();
    public List<Vector3> obstacleData = new List<Vector3>();

    public List<Vector3> velocityData = new List<Vector3>();
    public List<Vector3> positionData = new List<Vector3>();
    public List<Vector3> forceData = new List<Vector3>();


    public void saveData(DroneFake data)
    {
        if(data.embodied)
        {
            olfatiData.Add(data.lastOlfati);
            obstacleData.Add(data.lastObstacle);
            velocityData.Add(data.velocity);
            positionData.Add(data.position);
            forceData.Add(data.acceleration);
        }
    }
}

public class DroneFake
{
    #region Paramters Classes
    public Vector3 position;
    public Vector3 acceleration;
    public Vector3 velocity;

    public int layer = 0;
    
    public static float maxSpeed;
    public static float maxForce;
    public static float desiredSeparation = 3f;
    public static float neighborRadius = 10f;
    public static float alpha = 1.5f; // c
    public static float beta = 1.0f;  // c
    public static float delta = 1.0f; // c
    public static float avoidanceRadius = 2f;     // Radius for obstacle detection
    public static float avoidanceForce = 10f;     // Strength of the avoidance force
    public static float droneRadius = 0.17f;

    public static float dampingFactor = 0.96f;

    public static float lastDT = 0.02f;

    public static float spawnHeight = 0.5f;

    public bool embodied = false;
    public bool selected = false;

    public float score = 1.0f;

    public static int PRIORITYWHENEMBODIED = 1;

    public bool hasCrashed = false;

    public static LayerMask obstacleLayer;

    public Vector3 lastOlfati = Vector3.zero;
    public Vector3 lastObstacle = Vector3.zero;


    public List<float> olfatiForce = new List<float>();
    public List<float> obstacleForce = new List<float>();

    public List<Vector3> olfatiForceVec = new List<Vector3>();
    public List<Vector3> obstacleForceVec = new List<Vector3>();

    #endregion

    public DroneFake(Vector3 position, Vector3 velocity, bool embodied)
    {
        this.position = position;
        this.velocity = velocity;
        this.embodied = embodied;
    }
    public List<DroneFake> GetNeighbors(List<DroneFake> allDrones)
    {
        List<DroneFake> neighbors = new List<DroneFake>();
        foreach (DroneFake drone in allDrones)
        {
            if (drone == this) continue;

            if (Vector3.Distance(this.position, drone.position) < neighborRadius)
            {
                if(drone.hasCrashed)
                {
                    continue;
                }
                neighbors.Add(drone);
            }
        }
        return neighbors;
    }

    public void startPrediction(Vector3 alignementVector, NetworkCreator network)
    {
        ComputeForces(alignementVector, network);
    }

    private float GetCohesionIntensity(float r, float dRef, float a, float b, float c)
    {
        float diff = r - dRef;
        return ((a + b) / 2) * (Mathf.Sqrt(1 + Mathf.Pow(diff + c, 2)) - Mathf.Sqrt(1 + c * c)) + ((a - b) * diff / 2);
    }

    // Calculate cohesion intensity derivative
    private float GetCohesionIntensityDer(float r, float dRef, float a, float b, float c)
    {
        float diff = r - dRef;
        return ((a + b) / 2) * (diff + c) / Mathf.Sqrt(1 + Mathf.Pow(diff + c, 2)) + ((a - b) / 2);
    }

    // Calculate neighbor weight
    private float GetNeighbourWeight(float r, float r0, float delta)
    {
        float rRatio = r / r0;

        if (rRatio < delta)
            return 1;
        else if (rRatio < 1)
            return 0.25f * Mathf.Pow(1 + Mathf.Cos(Mathf.PI * (rRatio - delta) / (1 - delta)), 2);
        else
            return 0;
    }

    // Calculate neighbor weight derivative
    private float GetNeighbourWeightDer(float r, float r0, float delta)
    {
        float rRatio = r / r0;

        if (rRatio < delta)
            return 0;
        else if (rRatio < 1)
        {
            float arg = Mathf.PI * (rRatio - delta) / (1 - delta);
            return -0.5f * (Mathf.PI / (1 - delta)) * (1 + Mathf.Cos(arg)) * Mathf.Sin(arg);
        }
        else
            return 0;
    }

    // Calculate cohesion force
    private Vector3 GetCohesionForce(float r, float dRef, float a, float b, float c, float r0, float delta, Vector3 posRel)
    {
        float weightDer = GetNeighbourWeightDer(r, r0, delta);
        float intensity = GetCohesionIntensity(r, dRef, a, b, c);
        float intensityDer = GetCohesionIntensityDer(r, dRef, a, b, c);
        float weight = GetNeighbourWeight(r, r0, delta);

        return (weightDer * intensity / r0 + weight * intensityDer) * (posRel / r);
    }


    public void ComputeForces(Vector3 alignmentVector, NetworkCreator network)
    {
        List<DroneFake> allDrones = network.drones;
        List<DroneFake> neighbors = network.GetNeighbors(this);

        // Constants
        float dRef = desiredSeparation;
        float dRefObs = avoidanceRadius;

        float a = alpha;
        float b = beta;
        float c = (b - a) / (2 * Mathf.Sqrt(a * b));

        float r0Coh = neighborRadius;
        float r0Obs = avoidanceRadius;

        float cVm = 1.0f; // Velocity matching coefficient
        float cPmObs = 10f;

                // Reference velocity
        Vector3 vRef = alignmentVector;

        Vector3 accCoh = Vector3.zero;
        Vector3 accVel = Vector3.zero;

        float basePriority = 1;
        DroneFake embodiedDrone = allDrones.Find(d => d.embodied);
        if (embodiedDrone != null)
        {
            basePriority = 0;
        }

        foreach (DroneFake neighbour in neighbors)
        {
            float neighborPriority = getPriority(basePriority, neighbour);

            Vector3 posRelD = neighbour.position - position;
            float distD = posRelD.magnitude - 2*droneRadius;
            if (distD <= Mathf.Epsilon)
            {
                hasCrashed = true;
            }
            accCoh += GetCohesionForce(distD, dRef, a, b, c, r0Coh, delta, posRelD) * neighborPriority;


            if(neighbour.layer == 1)
            {
                accVel += cVm * (neighbour.velocity - velocity) * neighborPriority;
            }
        }

        accVel = Vector3.zero;

        // Obstacle avoidance
        Vector3 accObs = Vector3.zero;
        List<Vector3> obstacles = ClosestPointCalculator.ClosestPointsWithinRadius(position, avoidanceRadius);

        foreach (Vector3 obsPos in obstacles)
        {

            Vector3 posRel = position - obsPos;
            float dist = posRel.magnitude - droneRadius;
            if (dist <= Mathf.Epsilon)
            {
                hasCrashed = true;
            }

            // Apply forces similar to your original logic
            accObs += cPmObs * GetNeighbourWeight(dist / r0Obs, r0Coh, delta) *(
                        GetCohesionForce(dist, dRefObs, a, b, c, r0Coh, delta, obsPos - position)
                        // + GetCohesionForce(dAg, dRefObs, a, b, c, r0Coh, delta, posGamma - position)
                    );
        }

        if (embodied)
        {

            lastOlfati = accCoh;
            lastObstacle = accObs;

            addDataEmbodied(accCoh, accObs);

            accVel = cVm * (vRef - velocity);

            Vector3 force = accVel;
            force = Vector3.ClampMagnitude(force, maxForce/3);
            acceleration = force;
            return;
        }

        if (embodiedDrone == null)
        {
            accVel = cVm * (vRef - velocity);
        }

        if(!network.IsInMainNetwork(this))
        {
            accVel = Vector3.zero;
        }

        Vector3 fo = accCoh + accObs + accVel;
        fo = Vector3.ClampMagnitude(fo, maxForce);
        
        acceleration = fo;
    }

    float getPriority(float basePriority, DroneFake neighbour)
    {
        float neighborPriority = basePriority;
        if (neighbour.layer == 1)
        {
            neighborPriority = Mathf.Max((int)(PRIORITYWHENEMBODIED/3),5);
        }else if(neighbour.layer == 2)
        {
            neighborPriority = Mathf.Max((int)(PRIORITYWHENEMBODIED/10),3);
        }else if(neighbour.layer == 3)
        {
            neighborPriority = Mathf.Max((int)(PRIORITYWHENEMBODIED/20), 2);
        }else if(neighbour.layer == 4)
        {
            neighborPriority = 1f;
        }

        return neighborPriority;
    }

    public void resetEmbodied()
    {
        olfatiForce.Clear();
        obstacleForce.Clear();

        olfatiForceVec.Clear();
        obstacleForceVec.Clear();
    }

    public void addDataEmbodied(Vector3 olfati, Vector3 obstacle)
    {
        olfatiForce.Add(olfati.magnitude);
        obstacleForce.Add(obstacle.magnitude);

        olfatiForceVec.Add(olfati);
        obstacleForceVec.Add(obstacle);

        if (olfatiForce.Count > 20)
        {
            olfatiForce.RemoveAt(0);
            obstacleForce.RemoveAt(0);

            olfatiForceVec.RemoveAt(0);
            obstacleForceVec.RemoveAt(0);
        }
    }

    public float getHaptic()
    {
        //takje the last 10 and average them
        float olfati = 0;
        float obstacle = 0;
        int count = Mathf.Min(olfatiForce.Count, 12);

        if (count < 2)
        {
            return 0;
        }

        for (int i = 0; i < count-1; i++)
        {
            float diffOlfati = olfatiForce[olfatiForce.Count - 1 - i] - olfatiForce[olfatiForce.Count - 2 - i];
            olfati += diffOlfati;

            float diffObstacle = obstacleForce[obstacleForce.Count - 1 - i] - obstacleForce[obstacleForce.Count - 2 - i];
            obstacle += diffObstacle;
        }

        olfati /= count;
        obstacle /= count;

        olfati = Mathf.Max(olfati, 0) / 0.3f * 10;
        obstacle = Mathf.Max(obstacle, 0) * 10;

        return olfati + obstacle;

    }

    public Vector3 getHapticVector()
    {
        Vector3 olfatiVec = Vector3.zero;
        Vector3 obstacleVec = Vector3.zero;

        int count = Mathf.Min(olfatiForce.Count, 12);

        if (count < 2)
        {
            return Vector3.zero;
        }

       // return olfatiForceVec[olfatiForceVec.Count - 1];



        for (int i = 0; i < count-1; i++)
        {
            Vector3 diffOlfati = olfatiForceVec[olfatiForceVec.Count - 1 - i] - olfatiForceVec[olfatiForceVec.Count - 2 - i];
            olfatiVec += diffOlfati;

            Vector3 diffObstacle = obstacleForceVec[obstacleForceVec.Count - 1 - i] - obstacleForceVec[obstacleForceVec.Count - 2 - i];
            obstacleVec += diffObstacle;
        }

        olfatiVec /= count;
        obstacleVec /= count;

        olfatiVec *= 10;


        return olfatiVec;
    }

    public bool isNeighboor(DroneFake drone)
    {
        return Vector3.Distance(position, drone.position) < neighborRadius;
    }

    public void UpdatePositionPrediction(int numberOfTimeApplied)
    {
        for (int i = 0; i < numberOfTimeApplied; i++)
        {
            velocity += acceleration * 0.02f;
            velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

            // Apply damping to reduce the velocity over time
            velocity *= dampingFactor;

            position += velocity * 0.02f;
            //position.y = spawnHeight;
        }

        acceleration = Vector3.zero;
    }

}

public class NetworkCreator
{

    public List<DroneFake> drones = new List<DroneFake>();
    public Dictionary<DroneFake, List<DroneFake>> adjacencyList = new Dictionary<DroneFake, List<DroneFake>>();
    public HashSet<DroneFake> largestComponent = new HashSet<DroneFake>();

    bool hasEmbodied = false;

    public NetworkCreator(List<DroneFake> dr)
    {
        drones = dr;

        foreach (DroneFake drone in drones)
        {
            adjacencyList[drone] = new List<DroneFake>();
            if (drone.embodied)
            {
                hasEmbodied = true;
            }
        }
    }
    public void refreshNetwork()
    {
        BuildNetwork(drones);
        FindLargestComponent(drones);
        AssignLayers();
    }
    void BuildNetwork(List<DroneFake> drones)
    {
        try
        {
            // Clear previous connections
            foreach (var drone in adjacencyList.Keys)
            {
                adjacencyList[drone].Clear();
            }
            // Build new connections
            foreach (DroneFake drone in drones)
            {
                foreach (DroneFake otherDrone in drones)
                {
                    if (drone == otherDrone) continue;

                    if (IsDistanceNeighbor(drone, otherDrone))
                    {
                        adjacencyList[drone].Add(otherDrone);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            return;
        }
    }

    void FindLargestComponent(List<DroneFake> drones)
    {
        largestComponent.Clear();

        HashSet<DroneFake> visited = new HashSet<DroneFake>();
        List<HashSet<DroneFake>> components = new List<HashSet<DroneFake>>();

        foreach (DroneFake drone in drones)
        {
            if (!visited.Contains(drone))
            {
                HashSet<DroneFake> component = new HashSet<DroneFake>();
                Queue<DroneFake> queue = new Queue<DroneFake>();
                queue.Enqueue(drone);
                visited.Add(drone);

                while (queue.Count > 0)
                {
                    DroneFake current = queue.Dequeue();
                    component.Add(current);

                    foreach (DroneFake neighbor in adjacencyList[current])
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
        foreach (HashSet<DroneFake> component in components)
        {
            // Check if the component contains an embodied drone
            bool containsEmbodied = false;
            bool containsSelected = false;
            foreach (DroneFake drone in component)
            {
                if (drone.embodied)
                {
                    containsEmbodied = true;
                    break;
                }

                if (drone.selected)
                {
                    containsSelected = true;
                }
            }

            if (containsEmbodied || containsSelected)
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
    }

    public bool IsInMainNetwork(DroneFake drone)
    {
        return largestComponent.Contains(drone);
    }
    bool IsDistanceNeighbor(DroneFake a, DroneFake b)
    {
        float distance = Vector3.Distance(a.position, b.position);
        if (distance > DroneFake.neighborRadius) return false;

        bool visible = ClosestPointCalculator.IsLineIntersecting(a.position, b.position);
        return !visible;
    }

    public List<DroneFake> GetNeighbors(DroneFake drone)
    {
        if (!adjacencyList.ContainsKey(drone))
        {
            return new List<DroneFake>();
        }
        return adjacencyList[drone];
    }

    public void AssignLayers()
    {
        DroneFake mainDrone = drones.Find(d => d.embodied);

        foreach (var drone in adjacencyList.Keys)
        {
            drone.layer = 0;
        }

        if (mainDrone == null)
        {
            return;
        }

        // If the main drone does not exist in the adjacency list, just return.
        if (!adjacencyList.ContainsKey(mainDrone))
        {
            return;
        }

        // Perform BFS starting from the main drone.
        Queue<DroneFake> queue = new Queue<DroneFake>();
        
        mainDrone.layer = 1;
        queue.Enqueue(mainDrone);

        while (queue.Count > 0)
        {
            var currentDrone = queue.Dequeue();
            int currentLayer = currentDrone.layer;

            // Get neighbors of the current drone
            if (adjacencyList.ContainsKey(currentDrone))
            {
                foreach (var neighbor in adjacencyList[currentDrone])
                {
                    // If the neighbor hasn't been assigned a layer yet (still 0)
                    if (neighbor.layer == 0)
                    {
                        neighbor.layer = currentLayer + 1;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }
    }

    public Dictionary<int, int> getLayersConfiguration()
    {
        Dictionary<int, int> layers = new Dictionary<int, int>();

        foreach (var drone in drones)
        {
            if (!layers.ContainsKey(drone.layer))
            {
                layers[drone.layer] = 0;
            }

            layers[drone.layer]++;
        }
        string message = "";
        //order the layers by key
        var ordered = layers.OrderBy(x => x.Key);

        foreach (var layer in ordered)
        {
            message += "[" + layer.Key + " : " + layer.Value + "] ";
        }

        Debug.Log(message);

        return layers;
    }

}
