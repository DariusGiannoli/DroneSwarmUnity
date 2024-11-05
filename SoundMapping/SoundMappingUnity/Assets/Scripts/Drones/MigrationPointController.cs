using UnityEngine;

public class MigrationPointController : MonoBehaviour
{
    public Camera mainCamera; // Assign your main camera in the Inspector
    public LayerMask groundLayer; // Layer mask for the ground
    public LayerMask droneLayer; // Layer mask for the drones
    public float spawnHeight = 10f; // Height at which drones operate

    void Update()
    {
        UpdateMigrationPoint();
    }

    void UpdateMigrationPoint()
    {
        // Check if the left mouse button is pressed
        if (Input.GetMouseButton(0))
        {
            // Get the mouse position in screen coordinates
            Vector3 mousePosition = Input.mousePosition;

            // Create a ray from the camera through the mouse position
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);

            RaycastHit hit;

            //draws a line from the camera to the mouse position
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2f);

            // Perform the raycast and check if it hits the ground
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                // Set the migration point to the hit point
                Vector3 point = hit.point;
                point.y = spawnHeight; // Ensure the height matches the drones' height

                print("Migration point set to: " + point);

                DroneController.migrationPoint = point;
            }
        }
        else if (Input.GetMouseButtonDown(2))
        {
            if(this.GetComponent<CameraMovement>().embodiedDrone != null)
            {
                this.GetComponent<CameraMovement>().embodiedDrone.GetComponent<Camera>().enabled = false;
                this.GetComponent<CameraMovement>().embodiedDrone = null;
            }
             // Get the mouse position in screen coordinates
            Vector3 mousePosition = Input.mousePosition;
            // Create a ray from the camera through the mouse position
            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            RaycastHit hit;
            //draws a line from the camera to the mouse position
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.green, 2f);

            // Perform the raycast and check if it hits the ground
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, droneLayer))
            {
              //  point.y = spawnHeight; // Ensure the height matches the drones' height
                print("embodied to : " + hit.collider.gameObject.name);
                this.GetComponent<CameraMovement>().embodiedDrone = hit.collider.gameObject;
            }
        }

    }
}
