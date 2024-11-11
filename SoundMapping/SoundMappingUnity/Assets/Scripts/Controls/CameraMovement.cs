using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using FischlWorks_FogWar;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public static Camera cam;
    public Transform swarmHolder;
    private int FOVDrones = 5;

    public float heightCamera = 35;

    public GameObject fogWarManager;

    public string state = "TDView";
    // Start is called before the first frame update

    public const float animationTime = 2f;

    public static GameObject embodiedDrone = null;
    public static GameObject nextEmbodiedDrone = null;
    public Quaternion intialCamRotation;

    void Start()
    {
        cam = Camera.main;
        intialCamRotation = cam.transform.rotation;
        for(int i = 0; i < swarmHolder.childCount; i++)
        {
            fogWarManager.GetComponent<csFogWar>().AddFogRevealer(swarmHolder.GetChild(i).gameObject.transform, FOVDrones, true);
        }

        print(fogWarManager.GetComponent<csFogWar>().fogRevealers.Count);

        this.GetComponent<sendInfoGameObject>().setupCallback(getCameraPositionDE);
        this.GetComponent<sendInfoGameObject>().setupCallback(getEmbodiedDrone);

        StartCoroutine(TDView());
        
    }

    public Vector3 getCameraPosition()
    {
        return cam.transform.position;
    }
    // Update is called once per frame
    void updateTDView()
    {
        //if scolling up
        float rightStickVertical = Input.GetAxis("JoystickRightVertical");

        heightCamera += rightStickVertical * Time.deltaTime * 10;

        float rightStickHorizontal = Input.GetAxis("JoystickRightHorizontal");

        // applz rotation to the camera with lerp
        cam.transform.Rotate(-Vector3.forward, rightStickHorizontal * Time.deltaTime * 40);
    }

    void updateDroneView()
    {
        float rightStickHorizontal = Input.GetAxis("JoystickRightHorizontal");

        // applz rotation to the embodied drone with lerp
        embodiedDrone.transform.Rotate(Vector3.up, rightStickHorizontal * Time.deltaTime * 40);

    }


    public IEnumerator TDView()
    {
        state = "TDView";
        if (swarmHolder.childCount > 0)
        {
            Vector3 center = Vector3.zero;
            for(int i = 0; i < swarmHolder.childCount; i++)
            {
                center += swarmHolder.GetChild(i).position;
            }
            center /= swarmHolder.childCount;

            center.y = heightCamera;
            cam.transform.position = Vector3.Lerp(cam.transform.position, center, Time.deltaTime * 2);

            yield return new WaitForSeconds(0.01f);
        }
    
        updateTDView();
        if(embodiedDrone != null)
        {
            StartCoroutine(goAnimation());
        }
        else
        {
            StartCoroutine(TDView());
        }
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
                cam.transform.rotation = intialCamRotation;
                StartCoroutine(TDView());
                yield break;
            }
            
            
            cam.transform.position = Vector3.Lerp(startingPos, embodiedDrone.transform.position, elapsedTime / _animationTime);
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        embodiedDrone.GetComponent<Camera>().enabled = true;
        cam.enabled = false;

        StartCoroutine(droneView());
    }

    public IEnumerator droneView()
    {
        state = "droneView";
        Vector3 lastPosition = embodiedDrone.transform.position;
        while (embodiedDrone != null)
        {
            lastPosition = embodiedDrone.transform.position;
            updateDroneView();
            if(nextEmbodiedDrone != null)
            {
                embodiedDrone.GetComponent<Camera>().enabled = false;
                cam.enabled = true;
                cam.transform.position = embodiedDrone.transform.position;

                
                
                cam.transform.LookAt(nextEmbodiedDrone.transform.position);
                
                


                embodiedDrone = nextEmbodiedDrone;
                nextEmbodiedDrone = null;

                StartCoroutine(goAnimation(0.5f));
                yield break;
            }
            yield return new WaitForSeconds(0.01f);
        }

        cam.transform.position = new Vector3(lastPosition.x, heightCamera, lastPosition.z);
        cam.transform.rotation = intialCamRotation;
        cam.enabled = true;
        StartCoroutine(TDView());

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
