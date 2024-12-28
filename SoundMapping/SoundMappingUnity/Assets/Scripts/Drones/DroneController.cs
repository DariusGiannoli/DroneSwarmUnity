using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DroneController : MonoBehaviour
{

#region Parameters
    private swarmModel swarm;

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
            return HapticAudioManager.GetDroneNetworkScore(this.gameObject);
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

//        swarm.GetComponent<sendInfoGameObject>().setupCallback(() =>
//        {
//            return new DataEntry(this.transform.name + "_position", transform.position.ToString());
//       });

//        swarm.GetComponent<sendInfoGameObject>().setupCallback(() =>
//        {
//            return new DataEntry(this.transform.name + "_velocity", velocity.ToString());
//        });
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
    }

    #endregion


    #region HapticAudio
    void updateColor()
    {
        if(droneFake.embodied)
        {
            this.GetComponent<Renderer>().material = embodiedColor;
            return;
        }

        if (MigrationPointController.selectedDrone == this.gameObject)
        {
            this.GetComponent<Renderer>().material = selectedColor;
            this.droneFake.selected = true;
            return;
        }

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

    void updateSound()
    {
        float score = realScore;

        if (score < -0.9f)
        {
            timeSeparated += Time.deltaTime;
            this.GetComponent<AudioSource>().enabled = HapticAudioManager.GetAudioSourceCharacteristics(timeSeparated);
        }
        else
        {
            timeSeparated = 0;
            this.GetComponent<AudioSource>().enabled = false;
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