using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ObstacleGeneration : MonoBehaviour
{
    public GameObject[] floorObjects;    // Array of floor GameObjects
    public GameObject obstaclePrefab;    // The obstacle prefab to instantiate
    public Transform floorParent;        // The parent object for the instantiated obstacles
    public float density = 0.1f;         // Obstacles per unit area

    void Awake()
    {
        PlaceObstacles();
    }

    void Start()
    {
        AddObstacles();
    }

    void PlaceObstacles()
    {
        floorObjects = GameObject.FindGameObjectsWithTag("FloorCylinder");

        foreach (GameObject floor in floorObjects)
        {
            Mesh mesh = floor.GetComponent<MeshFilter>().sharedMesh;

            if (mesh == null)
            {
                Debug.LogWarning($"No mesh found on {floor.name}");
                continue;
            }

            // Calculate the surface area of the mesh
            float area = CalculateMeshArea(mesh, floor.transform);
            int obstacleCount = Mathf.RoundToInt(area * density);

            // Place obstacles randomly on the mesh surface
            for (int i = 0; i < obstacleCount; i++)
            {
                Vector3 position = GetRandomPointOnMesh(mesh, floor.transform);
                position.y += obstaclePrefab.transform.localScale.y / 4f;
                Instantiate(obstaclePrefab, position, Quaternion.identity, floorParent);
            }
        }
    }

    float CalculateMeshArea(Mesh mesh, Transform transform)
    {
        float totalArea = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Iterate over each triangle
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 p0 = transform.TransformPoint(vertices[triangles[i]]);
            Vector3 p1 = transform.TransformPoint(vertices[triangles[i + 1]]);
            Vector3 p2 = transform.TransformPoint(vertices[triangles[i + 2]]);

            // Calculate the area of the triangle
            float triangleArea = Vector3.Cross(p1 - p0, p2 - p0).magnitude * 0.5f;
            totalArea += triangleArea;
        }

        return totalArea;
    }

    Vector3 GetRandomPointOnMesh(Mesh mesh, Transform transform)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        // Select a random triangle
        int triangleIndex = Random.Range(0, triangles.Length / 3) * 3;
        Vector3 p0 = vertices[triangles[triangleIndex]];
        Vector3 p1 = vertices[triangles[triangleIndex + 1]];
        Vector3 p2 = vertices[triangles[triangleIndex + 2]];

        // Generate random barycentric coordinates
        float r1 = Random.value;
        float r2 = Random.value;

        // Ensure the point lies within the triangle
        if (r1 + r2 > 1f)
        {
            r1 = 1f - r1;
            r2 = 1f - r2;
        }

        Vector3 randomPoint = p0 + r1 * (p1 - p0) + r2 * (p2 - p0);
        return transform.TransformPoint(randomPoint);
    }
    
    void AddObstacles()
    {
        GameObject[] obstacleObjects = GameObject.FindGameObjectsWithTag("Obstacle");
        List<Obstacle> obstacles = new List<Obstacle>();

        foreach (GameObject obstacleObject in obstacleObjects)
        {
            if (obstacleObject.layer != 6)
            {
                continue;
            }


            Obstacle obstacle = null;
            if (obstacleObject.GetComponent<SphereCollider>() != null)
            {
                SphereCollider sphereCollider = obstacleObject.GetComponent<SphereCollider>();
                obstacle = new SphereObstacle(obstacleObject.transform.position, sphereCollider.radius);
            }
            else if (obstacleObject.GetComponent<BoxCollider>() != null)
            {
                BoxCollider boxCollider = obstacleObject.GetComponent<BoxCollider>();
                obstacle = new BoxObstacle(obstacleObject.transform.position, obstacleObject.transform.lossyScale, obstacleObject.transform.rotation);
            }
            else if (obstacleObject.GetComponent<CapsuleCollider>() != null)
            {
                CapsuleCollider capsuleCollider = obstacleObject.GetComponent<CapsuleCollider>();
                obstacle = new CylinderObstacle(obstacleObject.transform.position, obstacleObject.transform.lossyScale.x, obstacleObject.transform.lossyScale.y, obstacleObject.transform.rotation);
            }
            else
            {
                Debug.LogWarning(obstacleObject.name + " has no supported collider type.");
                continue;
            }

            obstacles.Add(obstacle);
        }


        ClosestPointCalculator.obstacles = obstacles;

        print("Obstacles: " + obstacles.Count);
    }

}
