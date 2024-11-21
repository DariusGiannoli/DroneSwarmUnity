using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    // Existing parameters
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    public static float neighborRadius
    {
        get
        {
            return desiredSeparation + 1;
        }
    }

    public static float desiredSeparation = 2f;
    public float alpha = 1.5f; // Separation weight
    public float beta = 1.0f;  // Alignment weight
    public float gamma = 1.0f; // Cohesion weight

    // Migration parameters
    public float delta = 1.0f; // Migration weight
    public static Vector3 migrationPoint = Vector3.zero;

    // Obstacle avoidance parameters
    public float avoidanceRadius = 2f;     // Radius for obstacle detection
    public float avoidanceForce = 10f;     // Strength of the avoidance force
    public LayerMask obstacleLayer;        // Layer mask for obstacles

    private Vector3 velocity;
    private Vector3 acceleration;
    private swarmModel swarm;

    private float droneRadius = 1.0f;

    public Vector3 separationForce = Vector3.zero;
    public Vector3 alignmentForce = Vector3.zero;
    public Vector3 cohesionForce = Vector3.zero;
    public Vector3 migrationForce = Vector3.zero;
    public Vector3 obstacleAvoidanceForce = Vector3.zero;

    public float distanceToSwarmCenter = 0;

    public List<ObstacleInRange> obstaclesInRange = new List<ObstacleInRange>();

    public Material connectedColor;
    public Material farColor;
    public Material notConnectedColor;
    public Material embodiedColor;

    public bool showGuizmos = false;
    

    
    const int PRIORITYWHENEMBODIED = 2;

    private GameObject gm;
    private float timeSeparated = 0;


    float realScore 
    {
        get
        {
            return HapticAudioManager.GetDroneNetworkScore(this.gameObject);
        }
    }

    void Start()
    {
        gm = GameObject.FindGameObjectWithTag("GameManager");

        swarm = gm.GetComponent<swarmModel>();

        swarm.GetComponent<sendInfoGameObject>().setupCallback(() =>
        {
            return new DataEntry(this.transform.name+"_position", transform.position.ToString());
        });

        swarm.GetComponent<sendInfoGameObject>().setupCallback(() =>
        {
            return new DataEntry(this.transform.name+"_velocity", velocity.ToString());
        });

        
        velocity = new Vector3(0, 0, 0);
        acceleration = Vector3.zero;

        droneRadius = this.transform.localScale.x / 2;
    }


    void Update()
    {
        ComputeForces();
        UpdatePosition();
        updateColor();
        updateSound();
    }

    void ComputeForces()
    {
        List<Transform> neighbors = GetNeighbors();

        int neighborCount = 0;
        int realNeighborCount = 0;

        separationForce = Vector3.zero;
        alignmentForce = Vector3.zero;
        cohesionForce = Vector3.zero;

        foreach (Transform neighbor in neighbors)
        {
            int neighborPriority = CameraMovement.embodiedDrone == neighbor.gameObject ? PRIORITYWHENEMBODIED : 1;

            Vector3 toNeighbor = neighbor.position - transform.position;
            float distance = toNeighbor.magnitude - 2*droneRadius;

            // Separation (repulsion)
            if (distance > 0)
            {
                if(distance < desiredSeparation)
                {
                    Vector3 repulsion = - alpha * (distance - desiredSeparation*0.9f) * (distance - desiredSeparation*0.9f) * toNeighbor.normalized;
                    separationForce += repulsion * neighborPriority;
                }

            }else{
                float realDistance = distance + 2*droneRadius;
                Vector3 repulsion = -alpha * (toNeighbor.normalized / (realDistance*realDistance));
                separationForce += repulsion * neighborPriority;
            }

            // Alignment (velocity matching)
            DroneController neighborController = neighbor.GetComponent<DroneController>();
            if (neighborController != null)
            {
                alignmentForce += neighborController.velocity * neighborPriority;
            }

            cohesionForce += neighbor.position * neighborPriority;

            neighborCount += neighborPriority;
            realNeighborCount++;
        }

        if (neighborCount > 0 && realNeighborCount > 0)
        {
            // Alignment
            alignmentForce /= neighborCount;
            alignmentForce = (alignmentForce - velocity) * beta;

            // Cohesion
            cohesionForce /= neighborCount;
            cohesionForce = (cohesionForce - transform.position) * gamma;

            distanceToSwarmCenter = Vector3.Distance(transform.position, cohesionForce);
        }

        // Migration Force towards the migrationPoint
        migrationForce = delta * (migrationPoint - transform.position).normalized;
        migrationForce.y = 0; // Keep the migration force in XZ plane

        // Obstacle Avoidance Force
        obstacleAvoidanceForce = ComputeObstacleAvoidanceForce();

        acceleration = computeAllForcesAccordingToControlRules();

    }

    Vector3 computeAllForcesAccordingToControlRules()
    {
        if (CameraMovement.embodiedDrone == this.gameObject)
        {
            Vector3 force = migrationForce;
            return Vector3.ClampMagnitude(force, maxForce);
        }
        Vector3 fo = separationForce + alignmentForce + cohesionForce + migrationForce;
        fo = Vector3.ClampMagnitude(fo + obstacleAvoidanceForce, maxForce);
        return fo;

    }

    void UpdatePosition()
    {
        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        // Apply damping to reduce the velocity over time
        float dampingFactor = 0.96f; // Adjust this value between 0 and 1
        velocity *= dampingFactor;

        Vector3 newPosition = transform.position + velocity * Time.deltaTime;

        // Keep the drone at the same height
        newPosition.y = swarm.spawnHeight;

        transform.position = newPosition;

        acceleration = Vector3.zero;
    }

    List<Transform> GetNeighbors()
    {
        List<Transform> neighbors = new List<Transform>();
        foreach (Transform drone in swarmModel.swarmHolder.transform)
        {
            if (drone == transform) continue;

            if (Vector3.Distance(transform.position, drone.position) < neighborRadius)
            {
                neighbors.Add(drone);
            }
        }
        return neighbors;
    }

    Vector3 ComputeObstacleAvoidanceForce()
    {
        Vector3 avoidanceForceVector = Vector3.zero;

        // Find all colliders within the avoidance radius on the obstacle layer
        Collider[] obstacles = Physics.OverlapSphere(transform.position, avoidanceRadius+droneRadius, obstacleLayer);
        obstaclesInRange.Clear();

        foreach (Collider obstacle in obstacles)
        {
            // Calculate a force away from the obstacle
            Vector3 awayFromObstacle = transform.position - obstacle.ClosestPoint(transform.position);
            float distance = awayFromObstacle.magnitude - droneRadius;

            if (distance > 0)
            {
                // The force magnitude decreases with distance
                Vector3 repulsion = awayFromObstacle.normalized * (avoidanceForce / (distance*distance));
                //apply friction with speed
                Vector3 friction = -velocity * 0.3f;


                repulsion.y = 0; // Keep movement in the XZ plane
                avoidanceForceVector += repulsion + friction;

                obstaclesInRange.Add(new ObstacleInRange(obstacle.ClosestPoint(transform.position), obstacle.gameObject, distance));
            }
            else
            {
                if(CameraMovement.embodiedDrone == this.gameObject)
                {
                    CameraMovement.embodiedDrone = null;
                    CameraMovement.nextEmbodiedDrone = null;
                }
                gm.GetComponent<swarmModel>().RemoveDrone(this.gameObject);                     
            }
                
        }

        // Limit the avoidance force to maxForce
        
        
        //avoidanceForceVector = Vector3.ClampMagnitude(avoidanceForceVector, maxForce);

        return avoidanceForceVector;
    }

    void OnDrawGizmos()
    {

    }

    void OnDrawGizmosSelected()
    {
        if(showGuizmos)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + obstacleAvoidanceForce);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + separationForce);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + alignmentForce);

            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + cohesionForce);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position);
        }
    }

    void updateColor()
    {
        if (CameraMovement.embodiedDrone == this.gameObject || CameraMovement.nextEmbodiedDrone == this.gameObject || MigrationPointController.selectedDrone == this.gameObject)
        {
            this.GetComponent<Renderer>().material = embodiedColor;
            return;
        }

        float score = realScore;

        if(score < -0.9f)
        {
            this.GetComponent<Renderer>().material = notConnectedColor;
        }else if(score < 1)
        {
            this.GetComponent<Renderer>().material.Lerp(farColor, connectedColor, score);
        }else // == 1
        {
            this.GetComponent<Renderer>().material = connectedColor;
        }
    }

    void updateSound()
    {
        float score = realScore;

        if(score < -0.9f)
        {
            timeSeparated += Time.deltaTime;
            this.GetComponent<AudioSource>().enabled = HapticAudioManager.GetAudioSourceCharacteristics(timeSeparated);
        }else{
            timeSeparated = 0;
            this.GetComponent<AudioSource>().enabled = false;
        }

    }
}


public class ObstacleInRange
{
    public Vector3 position;
    public GameObject obstacle;
    public float distance;

    public bool insideObstacle;

    public ObstacleInRange(Vector3 position, GameObject obstacle, float distance, bool insideObstacle = false)
    {
        this.position = position;
        this.obstacle = obstacle;
        this.distance = distance;
        this.insideObstacle = insideObstacle;

    }
}