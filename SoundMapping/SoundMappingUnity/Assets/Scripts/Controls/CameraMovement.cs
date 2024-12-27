using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using FischlWorks_FogWar;
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

    public GameObject fogWarManager;

    public string state = "TDView";
    // Start is called before the first frame update

    public const float animationTime = 1f;

    public static GameObject embodiedDrone = null;
    public static GameObject nextEmbodiedDrone = null;
    public GameObject lastEmbodiedDrone = null;
    public Quaternion intialCamRotation;
    const float DEFAULT_HEIGHT_CAMERA = 20;

    public float rotationSpeed = 80;

    void Start()
    {
        cam = Camera.main;

        cam.transform.position = new Vector3(0, DEFAULT_HEIGHT_CAMERA, 0);
        heightCamera = DEFAULT_HEIGHT_CAMERA;

        intialCamRotation = cam.transform.rotation;

        this.GetComponent<sendInfoGameObject>().setupCallback(getCameraPositionDE);
        this.GetComponent<sendInfoGameObject>().setupCallback(getEmbodiedDrone);

        StartCoroutine(TDView());
        
    }

    public void resetFogExplorers()
    {
        fogWarManager.GetComponent<csFogWar>().fogRevealers.Clear();
        
        for (int i = 0; i < swarmHolder.childCount; i++)
        {
            fogWarManager.GetComponent<csFogWar>().AddFogRevealer(swarmHolder.GetChild(i).gameObject.transform, FOVDrones, true);
        }

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
        List<GameObject> drones = DroneNetworkManager.dronesInMainNetworkDistance;
        if (drones.Count > 0)
        {
            Vector3 center = Vector3.zero;
            foreach (GameObject drone in drones)
            {
                center += drone.transform.position;
            }
            center /= drones.Count; 

            center.y = heightCamera;

            cam.GetComponent<Camera>().orthographicSize = heightCamera;
            cam.transform.position = Vector3.Lerp(cam.transform.position, center, Time.deltaTime * 2);
        }


        float rightStickHorizontal = Input.GetAxis("JoystickRightHorizontal");
        // applz rotation to the camera with lerp
        cam.transform.Rotate(-Vector3.forward, rightStickHorizontal * Time.deltaTime * rotationSpeed);
    }

    void updateDroneView()
    {
        float rightStickHorizontal = Input.GetAxis("JoystickRightHorizontal");
        float heightChange = Input.GetAxis("JoystickRightVertical") * Time.deltaTime * 10;

        // applz rotation to the embodied drone with lerp
        embodiedDrone.transform.Rotate(Vector3.up, rightStickHorizontal * Time.deltaTime * rotationSpeed);
        

        updateTDView();

        camMinimap.GetComponent<Camera>().orthographicSize = Mathf.Clamp(camMinimap.GetComponent<Camera>().orthographicSize + heightChange, swarmModel.desiredSeparation*2, swarmModel.desiredSeparation*5);
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
        state = "goAnimation";
        float elapsedTime = 0;
        //position of the active camera
        Vector3 startingPos = cam.transform.position;
        while (elapsedTime < _animationTime)
        {
            if(embodiedDrone == null)
            {
                Vector3 forwardDroneC = lastEmbodiedDrone.transform.forward;
                forwardDroneC.y = 0;
                cam.transform.position = new Vector3(lastEmbodiedDrone.transform.position.x, heightCamera, lastEmbodiedDrone.transform.position.z);
                cam.transform.forward = forwardDroneC;
                
                StartCoroutine(TDView());
                yield break;
            }
            
            
            cam.GetComponent<Camera>().orthographicSize = Mathf.Lerp(cam.GetComponent<Camera>().orthographicSize, 5, elapsedTime / _animationTime);
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
        state = "goAnimationDroneToDrone";
        float elapsedTime = 0;
        float initialFOV = embodiedDrone.GetComponent<Camera>().fieldOfView;
        
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

        StartCoroutine(droneView());
    }

    public IEnumerator droneView()
    {
        state = "droneView";
        Vector3 lastPosition = embodiedDrone.transform.position;
        minimap.SetActive(true);
        while (embodiedDrone != null)
        {
            lastPosition = embodiedDrone.transform.position;
            updateDroneView();
            if(nextEmbodiedDrone != null)
            {
                lastEmbodiedDrone = embodiedDrone;
                setEmbodiedDrone(nextEmbodiedDrone);
                StartCoroutine(goAnimationDoneToDrone(animationTime));
                yield break;
            }
            yield return new WaitForSeconds(0.01f);
        }

        cam.transform.position = new Vector3(lastPosition.x, heightCamera, lastPosition.z);
        cam.transform.rotation = intialCamRotation;
        cam.enabled = true;
        StartCoroutine(TDView());

    }

    public static void setEmbodiedDrone(GameObject drone)
    {
        embodiedDrone = drone;
        drone.GetComponent<DroneController>().droneFake.embodied = true;
        nextEmbodiedDrone = null;
    }

    public static void desembodiedDrone()
    {
        embodiedDrone.GetComponent<DroneController>().droneFake.embodied = false;
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
}
