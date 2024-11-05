using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using FischlWorks_FogWar;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Camera cam;
    public Transform swarmHolder;
    private int FOVDrones = 5;

    public float heightCamera = 35;

    public GameObject fogWarManager;

    public string state = "TDView";
    // Start is called before the first frame update

    public float animationTime = 2f;

    public GameObject embodiedDrone = null;

    void Start()
    {
        cam = Camera.main;
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
        if (Input.GetAxis("Mouse ScrollWheel") > 0)
        {
            heightCamera -= 5;
        }else if (Input.GetAxis("Mouse ScrollWheel") < 0)
        {
            heightCamera += 5;
        }
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

    public IEnumerator goAnimation()
    {
        state = "goAnimation";
        float elapsedTime = 0;
        Vector3 startingPos = cam.transform.position;
        while (elapsedTime < animationTime)
        {
            if(embodiedDrone == null)
            {
                StartCoroutine(TDView());
                yield break;
            }
            cam.transform.position = Vector3.Lerp(startingPos, embodiedDrone.transform.position, elapsedTime / animationTime);
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
        while (embodiedDrone != null)
        {
            yield return new WaitForSeconds(0.01f);
        }

        cam.enabled = true;
        StartCoroutine(TDView());

    }

    DataEntry getCameraPositionDE()
    {
        return new DataEntry("camera", cam.transform.position.ToString());
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
