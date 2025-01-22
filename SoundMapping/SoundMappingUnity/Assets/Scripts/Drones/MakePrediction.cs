using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.Scripting;

public class MakePrediction : MonoBehaviour
{
    Material defaultMaterial;
    public Transform allPredictionsHolder;
    public Prediction longPred, shortPred;


    public Transform longPredictionLineHolder, shortPredictionLineHolder;

    Thread predictionThread;    


    void Start()
    {
        defaultMaterial = new Material(Shader.Find("Unlit/Color"));

        shortPred = new Prediction(true, 20, 2, 0, shortPredictionLineHolder);

        launchPreditionThread(shortPred);
    }

    void StartPrediction(Prediction pred)
    {
        Vector3 alignementVector = pred.alignementVector;   

        for(int i = 0; i < pred.deep; i++)
        {
            NetworkCreator network = new NetworkCreator(pred.dronesPrediction);
            network.refreshNetwork();
            foreach (DroneFake drone in pred.dronesPrediction)
            {
                drone.startPrediction(alignementVector, network);
            }


            for(int j = 0; j < pred.dronesPrediction.Count; j++)
            {
                pred.dronesPrediction[j].UpdatePositionPrediction(pred.step);
                pred.allData[j].positions.Add(pred.dronesPrediction[j].position);
                pred.allData[j].crashed.Add(pred.dronesPrediction[j].hasCrashed);

                if (pred.dronesPrediction[j].hasCrashed && !pred.allData[j].crashedPrediction)
                {
                    pred.allData[j].crashedPrediction = true;
                    pred.allData[j].idFirstCrash = i;
                }
            }
        }

        pred.donePrediction = true;
    }


    void launchPreditionThread(Prediction pred)
    {
        pred.alignementVector = MigrationPointController.alignementVector;

        shortPred.directionOfMigration = this.GetComponent<MigrationPointController>().deltaMigration;
        spawnPrediction(shortPred);
        lock (shortPred)
        {
            predictionThread = new Thread(() => StartPrediction(pred));
            predictionThread.Start();
        }
    }
    void spawnPrediction(Prediction pred)
    {
        pred.allData = new List<DroneDataPrediction>();
        pred.dronesPrediction = new List<DroneFake>();

        foreach (DroneFake child in swarmModel.drones)
        {
            DroneDataPrediction data = new DroneDataPrediction();
            DroneFake copy = new DroneFake(child.position, child.velocity, false, child.id);
            copy.embodied = child.embodied;
            copy.selected = child.selected;
            
            pred.dronesPrediction.Add(copy);
            pred.allData.Add(data);
        }
    }

    void Update()
    {   
        if(shortPred.donePrediction)
        {
            this.GetComponent<HapticsTest>().HapticsPrediction(shortPred);
            UpdateLines(shortPred);
            shortPred.donePrediction = false;
            launchPreditionThread(shortPred);
        }
    }

    //on exit
    void OnDisable()
    {
        StopAllCoroutines();
        // stop the prediction thread
        if (predictionThread != null)
        {
            predictionThread.Abort();
        }

        print("Prediction thread stopped");

    }

    void UpdateLines(Prediction pred)
{
    if (pred.allData == null || pred.allData.Count == 0)
        return;

    // Destroy all existing line renderers
    foreach (LineRenderer lr in pred.LineRenderers)
    {
        Destroy(lr.material);
        Destroy(lr.gameObject);
    }
    pred.LineRenderers.Clear();

    //single lineRenderer for each drone
    foreach (DroneDataPrediction data in pred.allData)
    {
        GameObject lineObj = new GameObject("DronePredictionLine");
        lineObj.transform.SetParent(pred.lineHolder);

        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        line.positionCount = data.positions.Count;
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.material = defaultMaterial;
        line.gameObject.layer = 10;

        bool hasCrashed = false;

        // Fill positions in a single pass
        for (int i = 0; i < data.positions.Count; i++)
        {
            hasCrashed = hasCrashed || data.crashed[i];
            line.SetPosition(i, data.positions[i]);
        }

        Color segmentColor = hasCrashed ? Color.red : Color.grey;
        line.material.color = segmentColor; 

        pred.LineRenderers.Add(line);
    }
}

}



public class Prediction
{
    public bool donePrediction = false;
    public bool shortPrediction;
    public int deep;
    public int current;

    public Vector3 directionOfMigration;

    public int step = 1;

    public Transform lineHolder;    

    public List<DroneFake> dronesPrediction;

    public List<DroneDataPrediction> allData;
    public List<LineRenderer> LineRenderers;

    public Vector3 alignementVector;

    public Prediction(bool prediction, int deep, int step, int current,  Transform lineHolder)
    {
        this.shortPrediction = prediction;
        this.deep = deep;
        this.step = step;
        this.current = current;
        this.lineHolder = lineHolder;
        this.allData = new List<DroneDataPrediction>();
        this.LineRenderers = new List<LineRenderer>();
        directionOfMigration = Vector3.zero;
    }

}

public class DroneDataPrediction
{
    public List<Vector3> positions;
    public List<bool> crashed;
    public bool crashedPrediction = false;
    public int idFirstCrash = 0;

    public DroneDataPrediction()
    {
        positions = new List<Vector3>();
        crashed = new List<bool>();
    }
}
