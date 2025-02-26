using System;
using System.Collections.Generic;
using UnityEngine;

public class MigrationPointController : MonoBehaviour
{
    public static int idLeader = -1;
    public Camera mainCamera; // Assign your main camera in the Inspector
    public LayerMask groundLayer; // Layer mask for the ground
    public LayerMask droneLayer; // Layer mask for the drones
    public float spawnHeight = 10f; // Height at which drones operate
    public float radius = 1f; // Radius of the migration point

    public Vector2 migrationPoint = new Vector2(0, 0);

    public static GameObject selectedDrone = null;

    public Material normalMaterial;
    public Material selectedMaterial;

    public Vector3 deltaMigration = new Vector3(0, 0, 0); 
    public static Vector3 alignementVector = new Vector3(0, 0, 0);
    public static Vector3 alignementVectorNonZero = new Vector3(0, 0, 0);

    public static float maxSpreadness = 5f;
    public static float minSpreadness = 1f;

    public static bool InControl = true;

    bool firstTime = true;

    public bool control_movement
    {
        get{
            return LevelConfiguration._control_movement;
        }
    }
    public bool control_spreadness
    {
        get{
            return LevelConfiguration._control_spreadness;
        }
    }
    public bool control_embodiement
    {
        get{
            return LevelConfiguration._control_embodiement;
        }
    }

    public bool _control_desembodiement
    {
        get{
            return LevelConfiguration._control_desembodiement;
        }
    }
    public bool control_selection
    {
        get
        {
            return LevelConfiguration._control_selection;
        }
    }
    public bool control_rotation
    {
        get
        {
            return LevelConfiguration._control_rotation;
        }
    }


    void Update()
    {
        if(!InControl)
        {
            return;
        }
        
        UpdateMigrationPoint();
        SelectionUpdate();  
        SpreadnessUpdate();
    }

    void SelectionUpdate()
    { 
        if(Input.GetKeyDown("joystick button " + 3))
        {
            
            swarmModel.dummyForcesApplied = !swarmModel.dummyForcesApplied;
        }       

        if((Input.GetKeyDown("joystick button " + 5) || Input.GetKeyDown("joystick button " + 4)) && control_selection) //selection
        {
            if(selectedDrone == null && CameraMovement.embodiedDrone == null) // if nothing selected
            {
                if(swarmModel.swarmHolder.transform.childCount > 0)
                {
                    if(LevelConfiguration._startEmbodied)
                    {
                        selectedDrone = swarmModel.swarmHolder.transform.GetChild(0).gameObject;
                        idLeader = selectedDrone.GetComponent<DroneController>().droneFake.id;
                    }
                }
            }
            else
            {
                if(CameraMovement.embodiedDrone != null)
                {
                    Dictionary<GameObject, float> scores = new Dictionary<GameObject, float>();

                    foreach(Transform drone in swarmModel.swarmHolder.transform)
                    {
                        if(drone.gameObject == CameraMovement.embodiedDrone)
                        {
                            continue;
                        }


                        Vector3 diff = drone.position - CameraMovement.embodiedDrone.transform.position;
                        float score = Vector3.Dot(diff, CameraMovement.embodiedDrone.transform.forward);

                        if(score > 0.5)
                        {
                            score /= diff.magnitude;
                            scores.Add(drone.gameObject, score);
                        }

                    }

                    //select the highest score
                    if(scores.Count > 0)
                    {
                        //sort the dictionary
                        List<KeyValuePair<GameObject, float>> sortedScores = new List<KeyValuePair<GameObject, float>>(scores);
                        sortedScores.Sort((x, y) => y.Value.CompareTo(x.Value));

                        //select the highest score
                        CameraMovement.nextEmbodiedDrone = sortedScores[0].Key;
                        print("Next Selected drone: " + CameraMovement.nextEmbodiedDrone.GetComponent<DroneController>().droneFake.id);
                    }
                }
                else
                {
                    int increment = Input.GetKeyDown("joystick button " + 5) ? 1 : -1;
                    if(increment == 0)
                    {
                        return;
                    }

                    List<HashSet<DroneFake>> subnetwork = swarmModel.network.GetSubnetworks();
                   // print("Number of subnetworks: " + subnetwork.Count);
                    //compute average position of each subnetwork
                    Dictionary<HashSet<DroneFake>, Vector3> averagePositions = new Dictionary<HashSet<DroneFake>, Vector3>();
                    foreach(HashSet<DroneFake> sub in subnetwork)
                    {
                        Vector3 averagePosition = new Vector3(0, 0, 0);
                        foreach(DroneFake drone in sub)
                        {
                            averagePosition += drone.position;
                        }
                        averagePosition /= sub.Count;
                        averagePositions.Add(sub, averagePosition);
                    }

                    //find the subnetwork of the selected drone
                    HashSet<DroneFake> selectedSubnetwork = null;
                    foreach(HashSet<DroneFake> sub in subnetwork)
                    {
                        if(sub.Contains(selectedDrone.GetComponent<DroneController>().droneFake)){
                            selectedSubnetwork = sub;
                            break;
                        }
                    }

                    //order the subnetworks by the position.z of their average position
                    List<KeyValuePair<HashSet<DroneFake>, Vector3>> sortedSubnetworks = new List<KeyValuePair<HashSet<DroneFake>, Vector3>>(averagePositions);
                    sortedSubnetworks.Sort((x, y) => y.Value.z.CompareTo(x.Value.z));

                    //find the index of the selected subnetwork
                    int selectedSubnetworkIndex = -1;
                    for(int i = 0; i < sortedSubnetworks.Count; i++)
                    {
                        if(sortedSubnetworks[i].Key == selectedSubnetwork)
                        {
                            selectedSubnetworkIndex = i;
                            break;
                        }
                    }
//                    print("Selected subnetwork index: " + selectedSubnetworkIndex);

                    //select the next subnetwork
                    int nextSubnetworkIndex = (selectedSubnetworkIndex + increment) % sortedSubnetworks.Count;
             //       print("Next subnetwork index: " + nextSubnetworkIndex);
                    if(nextSubnetworkIndex < 0)
                    {
                        nextSubnetworkIndex = sortedSubnetworks.Count - 1;
                    }

                    HashSet<DroneFake> nextSubnetwork = sortedSubnetworks[nextSubnetworkIndex].Key;

                    //get the first droneFake in the subnetwork
                    DroneFake nextDrone = null;
                    foreach (DroneFake drone in nextSubnetwork)
                    {
                        nextDrone = drone;
                        break;
                    }

                    if(nextDrone == null)
                    {
               //         print("No drone in the subnetwork");
                        return;
                    }

           //         print("Next drone: " + nextDrone.id);

                    //select the drone
                    foreach(Transform drone in swarmModel.swarmHolder.transform)
                    {
                        if(drone.gameObject.GetComponent<DroneController>().droneFake == nextDrone)
                        {
                            selectedDrone = drone.gameObject;
                            idLeader = nextDrone.id;
                            break;
                        }
                    }
                }
            }

            this.GetComponent<HapticsTest>().VibrateController(0.3f, 0.3f, 0.2f); // selection vibration
        }

        // button 0
        if(Input.GetKeyDown("joystick button " + 0) && control_embodiement) //embodiement
        {

            if(CameraMovement.embodiedDrone != null)
            {
                if(selectedDrone != CameraMovement.embodiedDrone)//drone 2 drone
                {
                    CameraMovement.nextEmbodiedDrone = selectedDrone; // set next selected drone diff to null to trigger animation to the other drone
                }
            }
            else if(selectedDrone != null)
            {
                CameraMovement.SetEmbodiedDrone(selectedDrone);
            }
        }

        if(Input.GetKeyDown("joystick button " + 1) && _control_desembodiement) //desembodie
        {
            if(CameraMovement.embodiedDrone != null)
            {
                CameraMovement.embodiedDrone.GetComponent<Camera>().enabled = false;                
                CameraMovement.DesembodiedDrone(CameraMovement.embodiedDrone); 
            }
        }
    }

    void SpreadnessUpdate()
    {
        float spreadness = Input.GetAxis("LR");
        if(spreadness != 0 && control_spreadness)
        {
            swarmModel.desiredSeparation+= spreadness * Time.deltaTime * 1.3f;
            swarmModel.desiredSeparation = Mathf.Clamp(swarmModel.desiredSeparation, minSpreadness, maxSpreadness);
        }
    }

    void UpdateMigrationPoint()
    {
        if(!control_movement)
        {
            return;
        }


        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        float heightControl = Input.GetAxis("JoystickRightVertical");
        Transform body = null;
        Vector3 right = new Vector3(0, 0, 0);
        Vector3 forward = new Vector3(0, 0, 0);
        Vector3 up = new Vector3(0, 0, 0);

        Vector3 final = new Vector3(0, 0, 0);

        if(CameraMovement.embodiedDrone == null)
        {
            body = CameraMovement.cam.transform;
            right = body.right;
            forward = body.up;
            up = -body.forward;

        }else{
            body = CameraMovement.embodiedDrone.transform;
            right = body.right;
            forward = body.forward;
            up = body.up;
        }

        CameraMovement.forward = forward;
        CameraMovement.right = right;
        CameraMovement.up = up;

        if(horizontal == 0 && vertical == 0 && heightControl == 0)
        {
            if(firstTime)
            {
                migrationPoint = new Vector2(body.position.x, body.position.z);
                firstTime = false;
            }
            //migrationPoint = new Vector2(body.position.x, body.position.z);
            deltaMigration = new Vector3(0, 0, 0);
        }else{
            firstTime = true;
            Vector3 centerOfSwarm = body.position;
            final = vertical * forward + horizontal * right + heightControl * up;
            final.Normalize();

            float newR = Mathf.Sqrt(horizontal * horizontal + vertical * vertical + heightControl * heightControl);
            Vector3 finalAlignement = final * newR * radius;

            final = final * radius;


            migrationPoint = new Vector2(centerOfSwarm.x + final.x, centerOfSwarm.z + final.z);
            deltaMigration = new Vector3(finalAlignement.x, finalAlignement.y, finalAlignement.z);
        }
        if( deltaMigration.magnitude > 0.1f)
        {
            alignementVectorNonZero = deltaMigration;
        }
        
        alignementVector = deltaMigration;

        Debug.DrawRay(body.position, alignementVector, Color.red, 0.01f);
    }

}