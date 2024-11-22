using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MakePrediction : MonoBehaviour
{
    public Transform allPredictionsHolder;

    public Prediction longPred, shortPred;

    public Transform longPredictionLineHolder, shortPredictionLineHolder;


    private Gamepad gamepad;

    void Start()
    {
        gamepad = Gamepad.current;
        if (gamepad == null)
        {
            Debug.LogWarning("No gamepad connected.");
        }


        shortPred = new Prediction(true, 10, 0, shortPredictionLineHolder);
        longPred = new Prediction(false, 15, 1, longPredictionLineHolder);

        StartCoroutine(makePrediction(longPred));
        StartCoroutine(makePrediction(shortPred));
    }

    //on exit
    void OnDisable()
    {
        StopAllCoroutines();
        gamepad.SetMotorSpeeds(0.0f, 0.0f);
    }

    void Update()
    {
    }


    IEnumerator makePrediction(Prediction pred)
    {
        try
        {
            //find all the GameObject("Prediction" + pred.current.ToString()); and destroy them
            GameObject predOBJ = GameObject.Find("Prediction" + pred.current.ToString());
            Destroy(predOBJ);
        }
        catch (Exception e)
        {
            Debug.Log("Predictions not found");
        }
        
        GameObject prediction = new GameObject("Prediction" + pred.current.ToString());
        prediction.transform.parent = allPredictionsHolder.transform;

        pred.allData = new List<DroneDataPrediction>();

        List<GameObject> allPredictions = new List<GameObject>();
        foreach (Transform child in swarmModel.swarmHolder.transform)
        {
            GameObject newChild = Instantiate(child.gameObject, child.position, child.rotation);
            newChild.transform.parent = prediction.transform;

            newChild.GetComponent<DroneController>().prediction = true;
            newChild.GetComponent<DroneController>().velocity = child.GetComponent<DroneController>().velocity;

            newChild.layer = 0;
            newChild.name = "Prediction" + child.name;
            newChild.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
            newChild.tag = "Untagged";

            allPredictions.Add(newChild);

            DroneDataPrediction data = new DroneDataPrediction(newChild.gameObject);
            pred.allData.Add(data);
        }

        for (int i = 0; i < pred.deep; i++)
        {
            foreach (GameObject child in allPredictions)
            {
                child.GetComponent<DroneController>().PredictForce();
            }

            foreach (GameObject child in allPredictions)
            {
                child.GetComponent<DroneController>().PredictMovement(pred.shortPrediction ? 2 : 4);
                pred.allData[allPredictions.IndexOf(child)].positions.Add(child.transform.position);
                pred.allData[allPredictions.IndexOf(child)].crashed.Add(child.GetComponent<DroneController>().crashedPrediction);
            }

            yield return new WaitForSeconds(0.01f);
        }

        //crash prediction
        foreach (DroneDataPrediction data in pred.allData)
        {
            if (data.crashed.Contains(true))
            {
                data.crashedPrediction = true;
                data.idFirstCrash = data.crashed.IndexOf(true);
            }
        }

        if (pred.shortPrediction)
        {
            vibrateForShortPrediction(pred);
        }
        UpdateLines(pred);

        if (!pred.shortPrediction)
            yield return new WaitForSeconds(0.2f);
        
        
        StartCoroutine(makePrediction(pred));
    }

   /* void OnDrawGizmos()
    {
        if (allDataRecap == null || allDataRecap.Count == 0)
            return; // Exit if no data to draw


        float currentTime = Time.time;

        foreach (DroneDataPrediction data in allDataRecap)
        {
            for (int i = 0; i < data.positions.Count - 1; i++)
            {
                // Only draw if the timestamp is within the last 1 second
                if (currentTime - data.timestamps[i] <= 1f)
                {
                    Gizmos.color = data.crashed[i] ? Color.red : Color.blue;
                    Gizmos.DrawLine(data.positions[i], data.positions[i + 1]);
                }
            }
        }
    }*` */

    void UpdateLines(Prediction pred)
{
    if (pred.allData == null || pred.allData.Count == 0)
        return; // Exit if no data to draw

    // Destroy all existing line renderers
    foreach (LineRenderer line in pred.LineRenderers)
    {
        Destroy(line.gameObject);
    }
    pred.LineRenderers.Clear();

    int downsampleRate = 5; // Select 1 point every 5 data points

    foreach (DroneDataPrediction data in pred.allData)
    {
        float fractionOfPath = (float)data.idFirstCrash / data.positions.Count;
        Color purpleColor = new Color(0.5f, 0f, 0.5f, 1f); // Purple
        Color greyColor = new Color(0.5f, 0.5f, 0.5f, 0.2f); // Grey
        Color colorPath = Color.Lerp(greyColor, purpleColor, fractionOfPath);

        int startIndex = 0;
        while (startIndex < data.positions.Count - 1)
        {
            bool isCrashed = data.crashed[startIndex];
            Color segmentColor = isCrashed ? Color.red : colorPath;

            // Find the end of the segment with the same color
            int endIndex = startIndex + 1;
            while (endIndex < data.positions.Count && data.crashed[endIndex - 1] == isCrashed)
            {
                endIndex++;
            }

            // Downsample the positions
            List<Vector3> downsampledPositions = new List<Vector3>();
            for (int i = startIndex; i < endIndex; i += downsampleRate)
            {
                downsampledPositions.Add(data.positions[i]);
            }
            // Ensure the last point is included
            downsampledPositions.Add(data.positions[endIndex - 1]);

            // Only create a line if we have at least two points
            if (downsampledPositions.Count >= 2)
            {
                GameObject lineObject = new GameObject("LineRenderer");
                lineObject.transform.parent = pred.lineHolder;

                LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = lineRenderer.endColor = segmentColor;
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;

                lineRenderer.positionCount = downsampledPositions.Count;
                lineRenderer.SetPositions(downsampledPositions.ToArray());

                pred.LineRenderers.Add(lineRenderer);
            }

            startIndex = endIndex - 1;
        }
    }
}
    void vibrateForShortPrediction(Prediction pred)
    {
        //if any drone is predicted to crash, vibrate the controller
        if (pred.allData.Exists(data => data.crashedPrediction))
        {
            gamepad.SetMotorSpeeds(0.5f, 0.5f);
        }
        else
        {
            gamepad.SetMotorSpeeds(0.0f, 0.0f);
        }
    }
}

public class Prediction
{
    public bool shortPrediction;
    public int deep;
    public int current;

    public Transform lineHolder;    

    public List<DroneDataPrediction> allData;
    public List<LineRenderer> LineRenderers;

    public Prediction(bool prediction, int deep, int current, Transform lineHolder)
    {
        this.shortPrediction = prediction;
        this.deep = deep;
        this.current = current;
        this.lineHolder = lineHolder;
        this.allData = new List<DroneDataPrediction>();
        this.LineRenderers = new List<LineRenderer>();
    }

}

public class DroneDataPrediction
{
    public List<Vector3> positions;
    public List<bool> crashed;
    public List<float> timestamps; 
    public GameObject drone;
    public bool crashedPrediction = false;
    public int idFirstCrash = 0;

    public DroneDataPrediction(GameObject drone)
    {
        this.drone = drone;
        positions = new List<Vector3>();
        crashed = new List<bool>();
    }
}
