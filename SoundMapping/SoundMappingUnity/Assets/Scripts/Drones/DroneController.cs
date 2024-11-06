using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    // Existing parameters
    public float maxSpeed = 5f;
    public float maxForce = 10f;
    public float neighborRadius = 5f;
    public float desiredSeparation = 2f;
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

    Vector3 separationForce = Vector3.zero;
    Vector3 alignmentForce = Vector3.zero;
    Vector3 cohesionForce = Vector3.zero;
    Vector3 migrationForce = Vector3.zero;
    Vector3 obstacleAvoidanceForce = Vector3.zero;
    
    const int PRIORITYWHENEMBODIED = 2;

    void Start()
    {
        GameObject gm = GameObject.FindGameObjectWithTag("GameManager");

        swarm = gm.GetComponent<swarmModel>();

        swarm.GetComponent<sendInfoGameObject>().setupCallback(() =>
        {
            return new DataEntry(this.transform.name+"_position", transform.position.ToString());
        });

        swarm.GetComponent<sendInfoGameObject>().setupCallback(() =>
        {
            return new DataEntry(this.transform.name+"_velocity", velocity.ToString());
        });

        
        velocity = new Vector3(Random.Range(-maxSpeed, maxSpeed), 0, Random.Range(-maxSpeed, maxSpeed));
        acceleration = Vector3.zero;
    }


    void Update()
    {
        ComputeForces();
        UpdatePosition();
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
            print(neighborPriority);

            Vector3 toNeighbor = neighbor.position - transform.position;
            float distance = toNeighbor.magnitude;

            // Separation (repulsion)
            if (distance > 0 && distance < desiredSeparation)
            {
                Vector3 repulsion = -alpha * (toNeighbor.normalized / distance);
                separationForce += repulsion * neighborPriority;
            }

            // Alignment (velocity matching)
            DroneController neighborController = neighbor.GetComponent<DroneController>();
            if (neighborController != null)
            {
                alignmentForce += neighborController.velocity * neighborPriority;
            }

            // Cohesion (attraction) neighborrd priority applied here ???
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
        }

        // Migration Force towards the migrationPoint
        migrationForce = delta * (migrationPoint - transform.position);
        migrationForce.y = 0; // Keep the migration force in XZ plane

        // Obstacle Avoidance Force
        obstacleAvoidanceForce = ComputeObstacleAvoidanceForce();

        // Sum up all forces
        

        acceleration = computeAllForcesAccordingToControlRules();
        acceleration = Vector3.ClampMagnitude(acceleration, maxForce);


    }

    Vector3 computeAllForcesAccordingToControlRules()
    {
                // Debug logging for forces
        Debug.Log("drone: " + this.gameObject.name + 
                  " separation: " + separationForce + 
                  " alignment: " + alignmentForce + 
                  " cohesion: " + cohesionForce + 
                  " migration: " + migrationForce + 
                  " obstacle: " + obstacleAvoidanceForce);


        if (CameraMovement.embodiedDrone == this.gameObject)
        {
            return migrationForce;
        }

        if (CameraMovement.embodiedDrone != null)
        {
            return separationForce + alignmentForce + cohesionForce + migrationForce + obstacleAvoidanceForce;
        }

        return separationForce + alignmentForce + cohesionForce + migrationForce + obstacleAvoidanceForce;
    }

    void UpdatePosition()
    {
        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        // Apply damping to reduce the velocity over time
        float dampingFactor = 0.95f; // Adjust this value between 0 and 1
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
        foreach (Transform drone in swarm.swarmHolder.transform)
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
        Collider[] obstacles = Physics.OverlapSphere(transform.position, avoidanceRadius, obstacleLayer);

        foreach (Collider obstacle in obstacles)
        {
            // Calculate a force away from the obstacle
            Vector3 awayFromObstacle = transform.position - obstacle.ClosestPoint(transform.position);
            float distance = awayFromObstacle.magnitude - this.transform.localScale.x/3;

            if (distance > 0)
            {
                // The force magnitude decreases with distance
                Vector3 repulsion = awayFromObstacle.normalized * (avoidanceForce / (distance*distance));
                repulsion.y = 0; // Keep movement in the XZ plane
                avoidanceForceVector += repulsion;
            }
            else
            {
                Debug.LogWarning("Obstacle avoidance: Drone is inside the obstacle!");
            }
                
        }

        // Limit the avoidance force to maxForce
        
        
        //avoidanceForceVector = Vector3.ClampMagnitude(avoidanceForceVector, maxForce);

        return avoidanceForceVector;
    }
}
