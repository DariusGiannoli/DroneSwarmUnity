using System.Text;
using Unity.VisualScripting;
using UnityEngine;

public class MigrationPointController : MonoBehaviour
{
    public Camera mainCamera; // Assign your main camera in the Inspector
    public LayerMask groundLayer; // Layer mask for the ground
    public LayerMask droneLayer; // Layer mask for the drones
    public float spawnHeight = 10f; // Height at which drones operate

    public Vector2 migrationPoint = new Vector2(0, 0);

    private int lastSelectedChild = 0;
    private GameObject selectedDrone = null;

    public Material normalMaterial;
    public Material selectedMaterial;

    void Update()
    {
        UpdateMigrationPoint();
        SelectionUpdate();  
    }

    void SelectionUpdate()
    {        
        if(Input.GetKeyDown("joystick button " + 5) || Input.GetKeyDown("joystick button " + 4)) // Assuming up to 20 buttons (adjust if needed)
        {
            if(selectedDrone == null)
            {
                //select the first child
                if(this.GetComponent<swarmModel>().swarmHolder.transform.childCount > 0)
                {
                    selectedDrone = this.GetComponent<swarmModel>().swarmHolder.transform.GetChild(0).gameObject;
                    selectedDrone.GetComponent<Renderer>().material = selectedMaterial;
                }
            }
            else
            {
                int increment = Input.GetKeyDown("joystick button " + 5) ? 1 : -1;
                //change material
                selectedDrone.GetComponent<Renderer>().material = normalMaterial;
                lastSelectedChild = (lastSelectedChild + increment) % this.GetComponent<swarmModel>().swarmHolder.transform.childCount;
                if(lastSelectedChild < 0)
                {
                    lastSelectedChild = this.GetComponent<swarmModel>().swarmHolder.transform.childCount - 1;
                }

                selectedDrone = this.GetComponent<swarmModel>().swarmHolder.transform.GetChild(lastSelectedChild).gameObject;
                selectedDrone.GetComponent<Renderer>().material = selectedMaterial;
            }
        }

        // bvutton 0
         if(Input.GetKeyDown("joystick button " + 0)) // Assuming up to 20 buttons (adjust if needed)
        {
            if(this.GetComponent<CameraMovement>().embodiedDrone != null)
            {
                if(selectedDrone != this.GetComponent<CameraMovement>().embodiedDrone)
                {
                    this.GetComponent<CameraMovement>().nextEmbodiedDrone = selectedDrone;
                }
                else
                {
                    this.GetComponent<CameraMovement>().embodiedDrone.GetComponent<Camera>().enabled = false;                
                    this.GetComponent<CameraMovement>().embodiedDrone = null;  
                }
            }
            else if(selectedDrone != null)
            {
                this.GetComponent<CameraMovement>().embodiedDrone = selectedDrone;
            }
        }
    }

    void UpdateMigrationPoint()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        if(this.GetComponent<CameraMovement>().embodiedDrone == null)
        {
            horizontal = horizontal * Time.deltaTime * 5;
            vertical = vertical * Time.deltaTime * 5;

            migrationPoint = new Vector2(migrationPoint.x + horizontal, migrationPoint.y + vertical);

            DroneController.migrationPoint = new Vector3(migrationPoint.x, spawnHeight, migrationPoint.y);

            Debug.DrawRay(new Vector3(migrationPoint.x, 0, migrationPoint.y), Vector3.up*10, Color.green, 0.01f);
        }else{
            //move toward the forward direction of the drone
            Vector3 forward = this.GetComponent<CameraMovement>().embodiedDrone.transform.forward;
            Vector3 right = this.GetComponent<CameraMovement>().embodiedDrone.transform.right;

            Vector3 verti = vertical * forward/8;
            Vector3 hori = horizontal * right/8;

            Vector3 final = verti + hori;
            migrationPoint = new Vector2(migrationPoint.x + final.x, migrationPoint.y + final.z);

            DroneController.migrationPoint = new Vector3(migrationPoint.x, spawnHeight, migrationPoint.y);

            Debug.DrawRay(DroneController.migrationPoint, Vector3.up*10, Color.green, 0.01f);


        }
    }
    void UpdateMigrationEmbodiementMouse()
    {


        if (Input.GetMouseButtonDown(2))
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
