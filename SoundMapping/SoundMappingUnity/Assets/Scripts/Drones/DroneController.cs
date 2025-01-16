using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DroneController : MonoBehaviour
{

#region Parameters
    private swarmModel swarm;

    public GameObject droneModel;

    public Vector3 separationForce = Vector3.zero;
    public Vector3 alignmentForce = Vector3.zero;
    public Vector3 cohesionForce = Vector3.zero;
    public Vector3 migrationForce = Vector3.zero;
    public Vector3 obstacleAvoidanceForce = Vector3.zero;

    public Material connectedColor;
    public Material farColor;
    public Material notConnectedColor;
    public Material selectedColor;
    public Material embodiedColor;
    public bool showGuizmos = false;
    public bool prediction = false;
    const float distanceToHeigth = 3f;


    private GameObject gm;
    private float timeSeparated = 0;
    public GameObject fireworkParticle;

    public DroneFake droneFake;

    float realScore
    {
        get
        {
            return 0.5f;
        }
    }
    
    #endregion
    
    
    void Start()
    {
        StartNormal();
        Application.targetFrameRate = 30; // Set the target frame rate to 30 FPS
    }

    public void crash()
    {
        if (CameraMovement.embodiedDrone == this.gameObject)
        {
            CameraMovement.desembodiedDrone(this.gameObject);
            CameraMovement.nextEmbodiedDrone = null;
            this.droneFake.embodied = false;
            this.droneFake.selected = false;
            MigrationPointController.selectedDrone = null;   
        }
        gm.GetComponent<swarmModel>().RemoveDrone(this.gameObject);

        gm.GetComponent<HapticsTest>().crash(CameraMovement.embodiedDrone == this.gameObject);

        GameObject firework = Instantiate(fireworkParticle, transform.position, Quaternion.identity);
        firework.transform.position = transform.position;
        Destroy(firework, 0.5f);
    }

    void FixedUpdate()
    {
        if (!prediction)
        {
            UpdateNormal();
        }
    }


    #region NormalMode
    void StartNormal()
    {
        gm = GameObject.FindGameObjectWithTag("GameManager");

        swarm = gm.GetComponent<swarmModel>();
    }

    void UpdateNormal()
    {
        Vector3 positionDrome = droneFake.position;
        //make a ray to check the height of the drone with the obstacle under it
      //  Ray ray = new Ray(positionDrome, Vector3.down);
       // RaycastHit hit;
      //  if (Physics.Raycast(ray, out hit, 1000))
      //  {
     //       positionDrome.y = hit.point.y + distanceToHeigth;
     //   }

        //update the position of the drone
        transform.position = positionDrome;
        updateColor();
        updateSound();
        droneAnimate();
    }

    #endregion


    #region HapticAudio

    void updateColor()
    {
        if(droneFake.embodied)
        {
            this.GetComponent<Renderer>().material = embodiedColor;
        }else{
            if (MigrationPointController.selectedDrone == this.gameObject)
            {
                this.GetComponent<Renderer>().material = selectedColor;
                this.droneFake.selected = true;
                return;
            }else{
                this.droneFake.selected = false;

                if (droneFake.score >= 0.9f)
                {
                    this.GetComponent<Renderer>().material = connectedColor;
                }
                else 
                {
                    this.GetComponent<Renderer>().material = notConnectedColor;
                }
            }
        }
    }

    void updateSound()
    {
        if(CameraMovement.embodiedDrone == this)
        {
            this.GetComponent<AudioSource>().enabled = false;
            return;
        }



        if (swarmModel.dronesInMainNetwork.Contains(this.droneFake))
        {
            timeSeparated += Time.deltaTime;
            this.GetComponent<AudioSource>().enabled = false;
        }
        else
        {
            timeSeparated = 0;
            this.GetComponent<AudioSource>().enabled = true;
        }

    }


    void droneAnimate()
    {
        //look at the same direction as velocity
        if (CameraMovement.embodiedDrone == this.gameObject)
        {
            return;
        }
        if (droneFake.velocity.magnitude > 0.5)
        {
            Vector3 forwardDrone = new Vector3(droneFake.velocity.x, 0, droneFake.velocity.z);
            //lerp the rotation
            transform.forward = Vector3.Lerp(transform.forward, forwardDrone, Time.deltaTime * 5);

        }

    }
    #endregion

}


public class ObstacleInRange
{
    public Vector3 position;
    public float distance;

    public ObstacleInRange(Vector3 position, float distance)
    {
        this.position = position;
        this.distance = distance;
    }
}