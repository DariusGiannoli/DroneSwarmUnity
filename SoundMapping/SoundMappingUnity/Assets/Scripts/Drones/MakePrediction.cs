using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class MakePrediction : MonoBehaviour
{
    public Transform allPredictionsHolder;
    public Prediction longPred, shortPred;


    public Transform longPredictionLineHolder, shortPredictionLineHolder;

    List<Vector3> allPredictions = new List<Vector3>();

    private Gamepad gamepad;
    public LayerMask obstacleLayer;

    public GameObject testObject;

    bool refresh = false;


    void Start()
    {
        gamepad = Gamepad.current;
        if (gamepad == null)
        {
            Debug.LogWarning("No gamepad connected.");
        }

        shortPred = new Prediction(true, 30, 1, 0, shortPredictionLineHolder);
        longPred = new Prediction(false, 15, 3, 1, longPredictionLineHolder);

        launchPreditionThread(shortPred);
    }

    void StartPrediction(Prediction pred)
    {
        Vector3 centerOfSwarm = Vector3.zero;
        foreach (DroneFake drone in pred.dronesPrediction)
        {
            centerOfSwarm += drone.position;
        }
        centerOfSwarm /= pred.dronesPrediction.Count;
        //take into accouht network ?
        Vector3 migrationPointPredict = centerOfSwarm + pred.directionOfMigration;
        for(int i = 0; i < pred.deep; i++)
        {
            foreach (DroneFake drone in pred.dronesPrediction)
            {
                drone.startPrediction(pred.dronesPrediction,migrationPointPredict);
            }


            for(int j = 0; j < pred.dronesPrediction.Count; j++)
            {
                pred.dronesPrediction[j].UpdatePositionPrediction(pred.step);
                pred.allData[j].positions.Add(pred.dronesPrediction[j].position);
                pred.allData[j].crashed.Add(pred.dronesPrediction[j].hasCrashed);

                if (pred.dronesPrediction[j].hasCrashed && !pred.allData[j].crashedPrediction)
                {
                    pred.allData[j].crashedPrediction = true;
                    pred.allData[j].idFirstCrash = i;
                }
            }
        }

        pred.donePrediction = true;
    }

    void spawnPredictions()
    {
        spawnPrediction(longPred);
        spawnPrediction(shortPred);
    }

    void launchPreditionThread(Prediction pred)
    {
        GameObject drone = swarmModel.swarmHolder.transform.GetChild(0).gameObject;
        DroneController scrip = drone.GetComponent<DroneController>();
        DroneFake.maxForce = scrip.maxForce;
        DroneFake.maxSpeed = scrip.maxSpeed;
        DroneFake.desiredSeparation = DroneController.desiredSeparation;
        DroneFake.alpha = scrip.alpha;
        DroneFake.beta = scrip.beta;
        DroneFake.gamma = scrip.gamma;
        DroneFake.delta = scrip.delta;
        DroneFake.avoidanceRadius = scrip.avoidanceRadius;
        DroneFake.avoidanceForce = scrip.avoidanceForce;
        DroneFake.droneRadius = scrip.droneRadius;
        DroneFake.neighborRadius = DroneController.neighborRadius;
        DroneFake.obstacleLayer = scrip.obstacleLayer;
        DroneFake.PRIORITYWHENEMBODIED = DroneController.PRIORITYWHENEMBODIED;
        DroneFake.dampingFactor = scrip.dampingFactor;
        DroneFake.spawnHeight = swarmModel.spawnHeight;


        //start a thread with short prediction
        shortPred.directionOfMigration = MigrationPointController.deltaMigration;
        spawnPredictions();
        lock (shortPred)
        {
            new Thread(() => StartPrediction(shortPred)).Start();
        }
    }
    void spawnPrediction(Prediction pred)
    {
        pred.allData = new List<DroneDataPrediction>();
        pred.dronesPrediction = new List<DroneFake>();

        foreach (Transform child in swarmModel.swarmHolder.transform)
        {
            DroneDataPrediction data = new DroneDataPrediction();
            pred.dronesPrediction.Add(new DroneFake(child.transform.position, child.GetComponent<DroneController>().velocity, false));
            pred.allData.Add(data);
        }
    }

    void Update()
    {   
        if(shortPred.donePrediction)
        {
            UpdateLines(shortPred);
            shortPred.donePrediction = false;
            launchPreditionThread(shortPred);
        }
    }

    //on exit
    void OnDisable()
    {
        StopAllCoroutines();
        gamepad.SetMotorSpeeds(0.0f, 0.0f);
    }

   /* void OnDrawGizmos()
    {
        if (allDataRecap == null || allDataRecap.Count == 0)
            return; // Exit if no data to draw


        float currentTime = Time.time;

        foreach (DroneDataPrediction data in allDataRecap)
        {
            for (int i = 0; i < data.positions.Count - 1; i++)
            {
                // Only draw if the timestamp is within the last 1 second
                if (currentTime - data.timestamps[i] <= 1f)
                {
                    Gizmos.color = data.crashed[i] ? Color.red : Color.blue;
                    Gizmos.DrawLine(data.positions[i], data.positions[i + 1]);
                }
            }
        }
    }*` */

    void UpdateLines(Prediction pred)
    {
    if (pred.allData == null || pred.allData.Count == 0)
    {
        print("No data to draw");
        return; // Exit if no data to draw

    }

    // Destroy all existing line renderers
    foreach (LineRenderer line in pred.LineRenderers)
    {
        Destroy(line.gameObject);
    }
    pred.LineRenderers.Clear();

    int downsampleRate = 1; // Select 1 point every 5 data points

    foreach (DroneDataPrediction data in pred.allData)
    {
        float fractionOfPath = (float)data.idFirstCrash / data.positions.Count;
        Color purpleColor = new Color(0.5f, 0f, 0.5f, 1f); // Purple
        Color greyColor = new Color(0.5f, 0.5f, 0.5f, 0.2f); // Grey
        Color colorPath = Color.Lerp(greyColor, purpleColor, fractionOfPath);

        for(int i = 0; i < data.positions.Count - 1; i++)
        {
            if (i % downsampleRate == 0)
            {

                bool isCrashed = data.crashed[i];
                Color segmentColor = isCrashed ? Color.red : colorPath;
                LineRenderer line = new GameObject().AddComponent<LineRenderer>();
                line.transform.SetParent(pred.lineHolder);
                line.positionCount = 2;
                line.SetPosition(0, data.positions[i]);
                line.SetPosition(1, data.positions[i + 1]);
                line.startWidth = 0.1f;
                line.endWidth = 0.1f;
                line.material = new Material(Shader.Find("Unlit/Color"));
                line.material.color = segmentColor;

                pred.LineRenderers.Add(line);                    
            } 
        }   
    }
}
    
    void vibrateForShortPrediction(Prediction pred)
    {
        //if any drone is predicted to crash, vibrate the controller
        if (pred.allData.Exists(data => data.crashedPrediction))
        {
            gamepad.SetMotorSpeeds(0.5f, 0.5f);
        }
        else
        {
            gamepad.SetMotorSpeeds(0.0f, 0.0f);
        }
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
    public static float alpha = 1.5f; // Separation weight
    public static float beta = 1.0f;  // Alignment weight
    public static float gamma = 1.0f; // Cohesion weight
    public static float delta = 1.0f; // Migration weight
    public static float avoidanceRadius = 2f;     // Radius for obstacle detection
    public static float avoidanceForce = 10f;     // Strength of the avoidance force
    public static float droneRadius = 1.0f;

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
    
    public void startPrediction(List<DroneFake> allDrones, Vector3 migrationPointPredict)
    {
        ComputeForces(allDrones, migrationPointPredict);
    }

    void ComputeForces(List<DroneFake> allDrones, Vector3 migrationPointPredict)
    {
        List<DroneFake> neighbors = GetNeighbors(allDrones);

        int neighborCount = 0;
        int realNeighborCount = 0;

        Vector3 separationForce = Vector3.zero;
        Vector3 alignmentForce = Vector3.zero;
        Vector3 cohesionForce = Vector3.zero;

        foreach (DroneFake neighbor in neighbors)
        {
            int neighborPriority = CameraMovement.embodiedDrone == neighbor.embodied ? PRIORITYWHENEMBODIED : 1;

            Vector3 toNeighbor = neighbor.position - position;
            float distance = toNeighbor.magnitude - 2 * droneRadius;

            // Separation (repulsion)
            if (distance > 0)
            {
                if (distance < desiredSeparation)
                {

                    Vector3 repulsion = -alpha * (distance - desiredSeparation * 0.9f) * (distance - desiredSeparation * 0.9f) * toNeighbor.normalized;
                    separationForce += repulsion * neighborPriority;
                }

            }
            else
            {
                float realDistance = distance + 2 * droneRadius;
                Vector3 repulsion = -alpha * (toNeighbor.normalized / (realDistance * realDistance));
                separationForce += repulsion * neighborPriority;
            }

            cohesionForce += neighbor.position * neighborPriority;

            neighborCount += neighborPriority;
            realNeighborCount++;
        }

        if (neighborCount > 0 && realNeighborCount > 0)
        {
            cohesionForce /= neighborCount;
            cohesionForce = (cohesionForce - position) * gamma;
        }

        // Migration Force towards the migrationPoint
        Vector3 migrationForce = delta * (migrationPointPredict - position).normalized;
        migrationForce.y = 0; // Keep the migration force in XZ plane

        // Obstacle Avoidance Force
        Vector3 obstacleAvoidanceForce = ComputeObstacleAvoidanceForce();

        Vector3 fo = separationForce + cohesionForce + migrationForce;
        fo = Vector3.ClampMagnitude(fo + obstacleAvoidanceForce, maxForce);
        
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
        }

        position += velocity * lastDT * numberOfTimeApplied;
        position.y = spawnHeight;

        acceleration = Vector3.zero;
    }
}

public class Prediction
{
    public bool donePrediction = false;
    public bool shortPrediction;
    public int deep;
    public int current;

    public Vector3 directionOfMigration;

    public int step = 1;

    public Transform lineHolder;    

    public List<DroneFake> dronesPrediction;

    public List<DroneDataPrediction> allData;
    public List<LineRenderer> LineRenderers;

    public Prediction(bool prediction, int deep, int step, int current,  Transform lineHolder)
    {
        this.shortPrediction = prediction;
        this.deep = deep;
        this.step = step;
        this.current = current;
        this.lineHolder = lineHolder;
        this.allData = new List<DroneDataPrediction>();
        this.LineRenderers = new List<LineRenderer>();
        directionOfMigration = Vector3.zero;
    }

}

public class DroneDataPrediction
{
    public List<Vector3> positions;
    public List<bool> crashed;
    public bool crashedPrediction = false;
    public int idFirstCrash = 0;

    public DroneDataPrediction()
    {
        positions = new List<Vector3>();
        crashed = new List<bool>();
    }
}
