using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public static Camera cam;
    public GameObject camMinimap;
    public GameObject minimap;
    public Transform swarmHolder;
    private int FOVDrones = 5;

    public float heightCamera = 20;

    public static string state = "TDView";
    // Start is called before the first frame update

    public const float animationTime = 0.1f;

    public static GameObject embodiedDrone = null;
    public static GameObject nextEmbodiedDrone = null;
    public GameObject lastEmbodiedDrone = null;
    public Quaternion intialCamRotation;
    const float DEFAULT_HEIGHT_CAMERA = 20;

    public float rotationSpeed = 80;

    public bool minimapActive
    {
        get
        {
            return LevelConfiguration._MiniMap;
        }
    }


    public static Vector3 forward = Vector3.forward;
    public static Vector3 right = Vector3.right;
    public static Vector3 up = Vector3.up;
    private bool control_rotation
    {
        get
        {
            return this.GetComponent<MigrationPointController>().control_rotation;
        }
    }

    void Start()
    {
        cam = Camera.main;

        cam.transform.position = new Vector3(0, DEFAULT_HEIGHT_CAMERA, 0);
        heightCamera = DEFAULT_HEIGHT_CAMERA;

        intialCamRotation = cam.transform.rotation;

        StartCoroutine(TDView());
        
    }

    public static void setNextEmbodiedDrone()
    {
        Transform swarmHolder = swarmModel.swarmHolder.transform;
        nextEmbodiedDrone = null;

        if (swarmHolder.childCount > 0)
        {
            // Create a list of indices for the drones
            List<int> indices = new List<int>();
            for (int i = 0; i < swarmHolder.childCount; i++)
            {
                indices.Add(i);
            }

            // Iterate over the list in random order
            while (indices.Count > 0)
            {
                // Pick a random index from the list
                int randomListIndex = UnityEngine.Random.Range(0, indices.Count);
                int droneIndex = indices[randomListIndex];
                // Remove the index from the list so it isn’t tried again
                indices.RemoveAt(randomListIndex);

                GameObject drone = swarmHolder.GetChild(droneIndex).gameObject;
                DroneController droneController = drone.GetComponent<DroneController>();

                if (droneController != null && !droneController.droneFake.hasCrashed)
                {
                    // Mark the drone as embodied and set the camera
                    droneController.droneFake.embodied = true;
                    CameraMovement.embodiedDrone = drone;
                    CameraMovement.nextEmbodiedDrone = drone;
                    return;
                }
            }
        }

        Debug.LogError("No drones to embody. Restart the simulation.");
        // Optionally restart the simulation here:
        swarmModel.restart();
    }


    public Vector3 getCameraPosition()
    {
        if (cam.enabled)
        {
            return cam.transform.position;
        }
        else
        {
            return embodiedDrone.transform.position;
        }
    }
    // Update is called once per frame
    void updateTDView()
    {
        List<DroneFake> drones = swarmModel.dronesInMainNetwork;
        if (drones.Count > 0)
        {
            Vector3 center = Vector3.zero;
            foreach (DroneFake drone in drones)
            {
                center += drone.position;
            }
            center /= drones.Count; 

            center.y = heightCamera;
            cam.transform.position = Vector3.Lerp(cam.transform.position, center, Time.deltaTime * 2);
        }


        float rightStickHorizontal = control_rotation ? Input.GetAxis("JoystickRightHorizontal") : 0;
        // applz rotation to the camera with lerp
        cam.transform.Rotate(-Vector3.forward, rightStickHorizontal * Time.deltaTime * rotationSpeed);

        cam.GetComponent<Camera>().orthographicSize = Mathf.Lerp(cam.GetComponent<Camera>().orthographicSize, Mathf.Max(swarmModel.desiredSeparation * 3, 6), Time.deltaTime * 2);
    }

    void updateDroneView()
    {
        float rightStickHorizontal = control_rotation ? Input.GetAxis("JoystickRightHorizontal") : 0;

        // applz rotation to the embodied drone with lerp
        embodiedDrone.transform.Rotate(Vector3.up, rightStickHorizontal * Time.deltaTime * rotationSpeed);
        

        updateTDView();

        camMinimap.GetComponent<Camera>().orthographicSize = swarmModel.desiredSeparation * 3;
        cam.transform.position = new Vector3(embodiedDrone.transform.position.x, heightCamera, embodiedDrone.transform.position.z);
    }

    public IEnumerator TDView()
    {
        state = "TDView";
        minimap.SetActive(false);
        yield return new WaitForSeconds(0.01f);
      
        while(CameraMovement.embodiedDrone == null)
        {
            updateTDView();
            yield return new WaitForSeconds(0.01f);
        }

        StartCoroutine(goAnimation());
    }

    public IEnumerator goAnimation(float _animationTime = animationTime)
    {
        state = "animation";
        float elapsedTime = 0;
        //position of the active camera
        Vector3 startingPos = cam.transform.position;

        while (elapsedTime < _animationTime)
        {
            if(embodiedDrone == null)
            {
                if(lastEmbodiedDrone != null)
                {
                    Vector3 forwardDroneC = lastEmbodiedDrone.transform.forward;
                    forwardDroneC.y = 0;

                    cam.transform.position = new Vector3(lastEmbodiedDrone.transform.position.x, heightCamera, lastEmbodiedDrone.transform.position.z);
                    cam.transform.up = forwardDroneC;
                    
                    StartCoroutine(TDView());
                    yield break;
                }else // been a crash
                {
                    StartCoroutine(TDView());
                    yield break;
                }
            }
            
            
            cam.GetComponent<Camera>().orthographicSize = Mathf.Lerp(cam.GetComponent<Camera>().orthographicSize, 5, elapsedTime / _animationTime);
            cam.transform.position = Vector3.Lerp(cam.transform.position, embodiedDrone.transform.position, elapsedTime / _animationTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        Vector3 forwardDrone = cam.transform.up;
        forwardDrone.y = 0;

        embodiedDrone.transform.forward = forwardDrone;

        embodiedDrone.GetComponent<Camera>().enabled = true;
        cam.enabled = false;

        StartCoroutine(droneView());
    }

    public IEnumerator goAnimationDoneToDrone(float _animationTime = animationTime)
    {
        state = "animation";
        float elapsedTime = 0;
        float initialFOV = embodiedDrone.GetComponent<Camera>().fieldOfView;

        print("DroneAnimation staart of " + embodiedDrone.name + " " + embodiedDrone.GetComponent<DroneController>().droneFake.embodied);

        if(lastEmbodiedDrone != embodiedDrone)
        {
            lastEmbodiedDrone.GetComponent<DroneController>().droneFake.embodied = false;
        }

        
        while (elapsedTime < _animationTime)
        {           
            lastEmbodiedDrone.GetComponent<Camera>().fieldOfView = Mathf.Lerp(lastEmbodiedDrone.GetComponent<Camera>().fieldOfView, 20, elapsedTime / _animationTime);
            lastEmbodiedDrone.transform.LookAt(embodiedDrone.transform.position);
            
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        lastEmbodiedDrone.GetComponent<Camera>().fieldOfView = initialFOV;
        lastEmbodiedDrone.GetComponent<Camera>().enabled = false;

        embodiedDrone.GetComponent<Camera>().enabled = true;
        Vector3 forwardDrone = new Vector3(lastEmbodiedDrone.transform.forward.x, 0, lastEmbodiedDrone.transform.forward.z);
        embodiedDrone.transform.forward = forwardDrone;

        
        print("DroneAnimation end of " + embodiedDrone.name + " " + embodiedDrone.GetComponent<DroneController>().droneFake.embodied);

        StartCoroutine(droneView());
    }

    public IEnumerator droneView()
    {

        print("DroneView of " + embodiedDrone.name + " " + embodiedDrone.GetComponent<DroneController>().droneFake.embodied);
        state = "droneView";
        Vector3 lastPosition = embodiedDrone.transform.position;
        minimap.SetActive(minimapActive);
        while (embodiedDrone != null)
        {
            lastPosition = embodiedDrone.transform.position;
            updateDroneView();
            if(nextEmbodiedDrone != null)
            {
                lastEmbodiedDrone = embodiedDrone;

                setEmbodiedDrone(nextEmbodiedDrone);
                
                if(lastEmbodiedDrone == embodiedDrone)
                {
                    print("Crash but no worries" + lastEmbodiedDrone.GetComponent<DroneController>().droneFake.embodied + " " + embodiedDrone.name);
                    StartCoroutine(goAnimationDoneToDrone(0.01f));
                }
                else
                {
                    StartCoroutine(goAnimationDoneToDrone(animationTime));
                }
                yield break;
            }
            yield return new WaitForSeconds(0.01f);
        }

        cam.transform.position = new Vector3(lastPosition.x, heightCamera, lastPosition.z);
        
        if(lastEmbodiedDrone != null)
        {
            Vector3 forwardDroneC = lastEmbodiedDrone.transform.forward;
            forwardDroneC.y = 0;

            cam.transform.position = new Vector3(lastEmbodiedDrone.transform.position.x, heightCamera, lastEmbodiedDrone.transform.position.z);
        }
        cam.enabled = true;


        StartCoroutine(TDView());
    }

    public static void setEmbodiedDrone(GameObject drone)
    {
        embodiedDrone = drone;
        drone.GetComponent<DroneController>().droneFake.embodied = true;
        drone.GetComponent<DroneController>().droneFake.resetEmbodied();
        nextEmbodiedDrone = null;
    }

    public static void desembodiedDrone(GameObject drone)
    {
        drone.GetComponent<DroneController>().droneFake.embodied = false;
        embodiedDrone = null;
    }

    DataEntry getCameraPositionDE()
    {
        return new DataEntry("camera", cam.transform.position.x.ToString());
    }

    DataEntry getEmbodiedDrone()
    {
        if(embodiedDrone == null)
        {
            return new DataEntry("embodiedDrone", "null");
        }
        return new DataEntry("embodiedDrone", embodiedDrone.name);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
}
