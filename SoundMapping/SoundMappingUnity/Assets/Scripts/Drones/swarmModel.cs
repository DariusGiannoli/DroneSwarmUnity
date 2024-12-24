using System.Collections;
using System.Collections.Generic;
using FischlWorks_FogWar;
using Unity.VisualScripting;
using UnityEngine;

public class swarmModel : MonoBehaviour
{
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
            return desiredSeparation + extraDistanceNeighboor;
        }
    }

    public static float desiredSeparation = 3f;
    public float alpha = 1.5f; // Separation weight
    public float beta = 1.0f;  // Alignment weight
    public float delta = 1.0f; // Migration weight

    public float avoidanceRadius = 2f;     // Radius for obstacle detection
    public float avoidanceForce = 10f;     // Strength of the avoidance force

    public float droneRadius = 1.0f;      // Radius of the drone
    public LayerMask obstacleLayer;        // Layer mask for obstacles

    public csFogWar fogWar;

    public const int PRIORITYWHENEMBODIED = 2;
    public float dampingFactor = 0.98f;


    public List<DroneFake> drones = new List<DroneFake>();

    void Awake()
    {
        swarmHolder = GameObject.FindGameObjectWithTag("Swarm");
        spawn();
    }

    void Start()
    {
        Application.targetFrameRate = 30; // Set the target frame rate to 30 FPS
        
        this.GetComponent<sendInfoGameObject>().setupCallback(getAverageCohesion);
        this.GetComponent<sendInfoGameObject>().setupCallback(getAverageAlignment);
        this.GetComponent<sendInfoGameObject>().setupCallback(getAverageSeparation);
        this.GetComponent<sendInfoGameObject>().setupCallback(getAverageMigration);
        this.GetComponent<sendInfoGameObject>().setupCallback(getAverageObstacleAvoidance);
        this.GetComponent<sendInfoGameObject>().setupCallback(getDeltaAverageObstacle);
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

        foreach (DroneFake drone in drones)
        {
            drone.ComputeForces(drones, MigrationPointController.alignementVector);
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
            Vector3 spawnPosition = new Vector3(spawnRadius * Mathf.Cos(i * 2 * Mathf.PI / numDrones), spawnHeight, spawnRadius * Mathf.Sin(i * 2 * Mathf.PI / numDrones));
            
            GameObject drone = Instantiate(dronePrefab, spawnPosition, Quaternion.identity);

            drone.GetComponent<DroneController>().droneFake = new DroneFake(spawnPosition, Vector3.zero, false);

            fogWar.AddFogRevealer(drone.transform, 5, true);

            drones.Add(drone.GetComponent<DroneController>().droneFake);

            drone.transform.parent = swarmHolder.transform;
            drone.name = "Drone"+i.ToString();
        }

        this.GetComponent<HapticAudioManager>().Reset();
        this.GetComponent<DroneNetworkManager>().Reset();
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

    DataEntry getAverageCohesion()
    {
        Vector3 averageCohesion = Vector3.zero;
        int numDrones = 0;

        foreach (Transform drone in swarmHolder.transform)
        {
            averageCohesion += drone.GetComponent<DroneController>().cohesionForce;
            numDrones++;
        }

        if (numDrones > 0)
        {
            averageCohesion /= numDrones;
        }

        return new DataEntry("averageCohesion", averageCohesion.magnitude.ToString(), fullHistory: true);
    }

    DataEntry getDeltaAverageObstacle()
    {
        Vector3 averageObstacle = Vector3.zero;
        int numDrones = 0;

        foreach (Transform drone in swarmHolder.transform)
        {
            averageObstacle += drone.GetComponent<DroneController>().obstacleAvoidanceForce;
            numDrones++;
        }

        if (numDrones > 0)
        {
            averageObstacle /= numDrones;
        }




        if(lastObstacleAvoidance < 0)
        {
            lastObstacleAvoidance = averageObstacle.magnitude;
            return new DataEntry("deltaObstacle", "0", fullHistory: true);
        }

        float delta = (averageObstacle.magnitude - lastObstacleAvoidance)*Time.deltaTime;
        lastObstacleAvoidance = averageObstacle.magnitude;


        return new DataEntry("deltaObstacle", delta.ToString(), fullHistory: true);
    }

    DataEntry getAverageAlignment()
    {
        Vector3 averageAlignment = Vector3.zero;
        int numDrones = 0;

        foreach (Transform drone in swarmHolder.transform)
        {
            averageAlignment += drone.GetComponent<DroneController>().alignmentForce;
            numDrones++;
        }

        if (numDrones > 0)
        {
            averageAlignment /= numDrones;
        }

        return new DataEntry("averageAlignment", averageAlignment.magnitude.ToString(), fullHistory: true);
    }

    DataEntry getAverageSeparation()
    {
        Vector3 averageSeparation = Vector3.zero;
        int numDrones = 0;

        foreach (Transform drone in swarmHolder.transform)
        {
            averageSeparation += drone.GetComponent<DroneController>().separationForce;
            numDrones++;
        }

        if (numDrones > 0)
        {
            averageSeparation /= numDrones;
        }

        return new DataEntry("averageSeparation", averageSeparation.magnitude.ToString(), fullHistory: true);
    }

    DataEntry getAverageMigration()
    {
        Vector3 averageMigration = Vector3.zero;
        int numDrones = 0;

        foreach (Transform drone in swarmHolder.transform)
        {
            averageMigration += drone.GetComponent<DroneController>().migrationForce;
            numDrones++;
        }

        if (numDrones > 0)
        {
            averageMigration /= numDrones;
        }

        return new DataEntry("averageMigration", averageMigration.magnitude.ToString(), fullHistory: true);
    }

    DataEntry getAverageObstacleAvoidance()
    {
        Vector3 averageObstacleAvoidance = Vector3.zero;
        int numDrones = 0;

        foreach (Transform drone in swarmHolder.transform)
        {
            averageObstacleAvoidance += drone.GetComponent<DroneController>().obstacleAvoidanceForce;
            numDrones++;
        }

        if (numDrones > 0)
        {
            averageObstacleAvoidance /= numDrones;
        }

        return new DataEntry("averageObstacleAvoidance", averageObstacleAvoidance.magnitude.ToString(), fullHistory: true);
    }

}


public class DroneFake
{
    #region Paramters Classes
    public Vector3 position;
    public Vector3 acceleration;
    public Vector3 velocity;
    
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

    public static int PRIORITYWHENEMBODIED = 2;

    public bool hasCrashed = false;

    public static LayerMask obstacleLayer;

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

    public Vector3 ComputeObstacleAvoidanceForce()
    {
        Vector3 avoidanceForceVector = Vector3.zero;
        List<Vector3> obstacles = ClosestPointCalculator.ClosestPointsWithinRadius(position, avoidanceRadius);

        foreach (Vector3 obstacle in obstacles)
        {
            // Calculate a force away from the obstacle
            Vector3 awayFromObstacle = position - obstacle;
            float distance = awayFromObstacle.magnitude - droneRadius;

            if (distance > 0)
            {
                Vector3 repulsion = awayFromObstacle.normalized * (avoidanceForce / (distance * distance));
                repulsion.y = 0; // Keep movement in the XZ plane
                avoidanceForceVector += repulsion;
            }
            else
            {
                hasCrashed = true;
            }

        }
        return avoidanceForceVector;
    }
    
    public void startPrediction(List<DroneFake> allDrones, Vector3 alignementVector)
    {
        ComputeForces(allDrones, alignementVector);
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


    public void ComputeForces(List<DroneFake> allDrones, Vector3 alignmentVector)
    {
        List<DroneFake> neighbors = GetNeighbors(allDrones);

        // Constants
        float dRef = desiredSeparation;
        float dRefObs = avoidanceRadius;

        float a = alpha;
        float b = beta;
        float c = (b - a) / (2 * Mathf.Sqrt(a * b));

        float r0Coh = neighborRadius;
        float r0Obs = avoidanceRadius;

        float cVm = 1.0f; // Velocity matching coefficient
        float cPmObs = 4.3f;

                // Reference velocity
        Vector3 vRef = alignmentVector;

        // Velocity matching
        Vector3 accVel = cVm * (vRef - velocity);
        Vector3 accCoh = Vector3.zero;

        foreach (DroneFake neighbour in neighbors)
        {
            Vector3 posRelD = neighbour.position - position;
            float distD = posRelD.magnitude - 2*droneRadius;
            if (distD <= Mathf.Epsilon)
            {
                hasCrashed = true;
            }
            accCoh += GetCohesionForce(distD, dRef, a, b, c, r0Coh, delta, posRelD);
        }

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
            Vector3 force = accVel;
            force = Vector3.ClampMagnitude(force, maxForce/3);
            acceleration = force;
            return;
        }

        Vector3 fo = accCoh + accObs + accVel;
       // Debug.Log("accCoh: " + accCoh + " accObs: " + accObs + " accVel: " + accVel);
        fo = Vector3.ClampMagnitude(fo, maxForce);
        
        acceleration = fo;
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
            position.y = spawnHeight;
        }

        acceleration = Vector3.zero;
    }
}
