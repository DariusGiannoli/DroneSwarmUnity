using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakePrediction : MonoBehaviour
{
    int current = 0;
    public Transform allPredictionsHolder;
    public List<DroneDataPrediction> allDataRecap = new List<DroneDataPrediction>();
    private List<float> deltaTimes = new List<float>();
    private const int maxDeltaTimes = 100;

    private List<LineRenderer> activeLineRenderers = new List<LineRenderer>();
    private List<LineRenderer> availableLineRenderers = new List<LineRenderer>();

    int maxPredictions = 25;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(makePrediction(swarmModel.swarmHolder.transform, current));
        }
        UpdateDeltaTimes();
    }

    void UpdateDeltaTimes()
    {
        if (deltaTimes.Count >= maxDeltaTimes)
        {
            deltaTimes.RemoveAt(0);
        }
        deltaTimes.Add(Time.deltaTime);
    }

    float GetAverageDeltaTime()
    {
        float sum = 0f;
        foreach (float deltaTime in deltaTimes)
        {
            sum += deltaTime;
        }
        return deltaTimes.Count > 0 ? sum / deltaTimes.Count : Time.deltaTime;
    }

    IEnumerator makePrediction(Transform predictionHolder, int current)
    {
        // Clear all the children of the prediction holder
        foreach (Transform child in allPredictionsHolder)
        {
            Destroy(child.gameObject);
        }

        GameObject prediction = new GameObject("Prediction" + current.ToString());
        prediction.transform.parent = allPredictionsHolder.transform;

        //remov ethe sphere collider
        if (prediction.GetComponent<SphereCollider>() != null)
        {
            Destroy(prediction.GetComponent<SphereCollider>());
        }

        List<DroneDataPrediction> allData = new List<DroneDataPrediction>();
        List<GameObject> allPredictions = new List<GameObject>();
        foreach (Transform child in predictionHolder)
        {
            GameObject newChild = Instantiate(child.gameObject, child.position, child.rotation);
            newChild.transform.parent = prediction.transform;

            newChild.GetComponent<DroneController>().prediction = true;
            newChild.GetComponent<DroneController>().velocity = child.GetComponent<DroneController>().velocity;

            newChild.layer = 0;
            newChild.name = "Prediction" + child.name;
            newChild.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            allPredictions.Add(newChild);

            DroneDataPrediction data = new DroneDataPrediction(newChild.gameObject);
            allData.Add(data);
        }

        for (int i = 0; i < maxPredictions; i++)
        {
            foreach (GameObject child in allPredictions)
            {
                child.GetComponent<DroneController>().PredictForce();
            }

            foreach (GameObject child in allPredictions)
            {
                float averageDeltaTime = GetAverageDeltaTime();
                child.GetComponent<DroneController>().PredictMovement(4);
                allData[allPredictions.IndexOf(child)].positions.Add(child.transform.position);
                allData[allPredictions.IndexOf(child)].crashed.Add(child.GetComponent<DroneController>().crashedPrediction);
                //allData[allPredictions.IndexOf(child)].timestamps.Add(Time.time); 

            }
        }

        allDataRecap = allData;
        UpdateLines();
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(makePrediction(predictionHolder, 0));
    }

    void OnDrawGizmos()
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
    }

    void UpdateLines()
    {
        if (allDataRecap == null || allDataRecap.Count == 0)
            return; // Exit if no data to draw

        // Move all active LineRenderers to available for reuse
        availableLineRenderers.AddRange(activeLineRenderers);
        activeLineRenderers.Clear();

        foreach (DroneDataPrediction data in allDataRecap)
        {
            for (int i = 0; i < data.positions.Count - 1; i++)
            {
                // Only draw if the timestamp is within the last 1 second
                LineRenderer lr;

                // Reuse a LineRenderer if available, else create a new one
                if (availableLineRenderers.Count > 0)
                {
                    lr = availableLineRenderers[0];
                    availableLineRenderers.RemoveAt(0);
                    lr.gameObject.SetActive(true);
                }
                else
                {
                    GameObject lineObj = new GameObject("LineRenderer");
                    lineObj.transform.SetParent(this.transform);
                    lr = lineObj.AddComponent<LineRenderer>();
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    lr.startWidth = lr.endWidth = 0.05f;
                }

                // Set positions
                lr.positionCount = 2;
                lr.SetPosition(0, data.positions[i]);
                lr.SetPosition(1, data.positions[i + 1]);

                // Set color based on crashed status
                Color lineColor = data.crashed[i] ? Color.red : Color.blue;
                lr.startColor = lr.endColor = lineColor;

                activeLineRenderers.Add(lr);
            }
        }
    }
}

public class DroneDataPrediction
{
    public List<Vector3> positions;
    public List<bool> crashed;
    public List<float> timestamps; 
    public GameObject drone;

    public DroneDataPrediction(GameObject drone)
    {
        this.drone = drone;
        positions = new List<Vector3>();
        crashed = new List<bool>();
    }
}
