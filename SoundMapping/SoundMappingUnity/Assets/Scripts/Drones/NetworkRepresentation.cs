using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRepresentation : MonoBehaviour
{
    public Image firstOrderNeighbors;
    public Image secondOrderNeighbors;
    public Image thirdOrderNeighbors;
    public Image leftBehindNeighbors;

    public static Dictionary<int, int> neighborsRep = new Dictionary<int, int>();

    public static float networkScore = 0;
    public static bool hasLeftBehind = false;
    
        // Start is called before the first frame update
    public void UpdateNetworkRepresentation(Dictionary<int, int> neighbors)
    {
        // update the dictionary


        int totalNeighbors = 0;
        int firstOrder = 0;
        foreach (KeyValuePair<int, int> neighbor in neighbors)
        {
            totalNeighbors += neighbor.Value;
        }

        // first order is the key = 1
        if (neighbors.ContainsKey(2))
        {
            firstOrder = neighbors[2];
        }

        // second order is the key = 2
        int secondOrder = 0;
        if (neighbors.ContainsKey(3))
        {
            secondOrder = neighbors[3];
        }

        //lefrt behind is the key = 0
        int leftBehind = 0;
        if (neighbors.ContainsKey(0))
        {
            leftBehind = neighbors[0];
        }

        // third order is the rest
        int thirdOrder = totalNeighbors - firstOrder - secondOrder - leftBehind - 1; //embodied drone

        // define the height of the bars
        float firstOrderHeight = firstOrder / (float)totalNeighbors;
        float secondOrderHeight = secondOrder / (float)totalNeighbors;
        float thirdOrderHeight = thirdOrder / (float)totalNeighbors;
        float leftBehindHeight = leftBehind / (float)totalNeighbors;

        // update the bars
        firstOrderNeighbors.rectTransform.sizeDelta = new Vector2(firstOrderNeighbors.rectTransform.sizeDelta.x, firstOrderHeight * 100);
        secondOrderNeighbors.rectTransform.sizeDelta = new Vector2(secondOrderNeighbors.rectTransform.sizeDelta.x, secondOrderHeight * 100);
        thirdOrderNeighbors.rectTransform.sizeDelta = new Vector2(thirdOrderNeighbors.rectTransform.sizeDelta.x, thirdOrderHeight * 100);
        leftBehindNeighbors.rectTransform.sizeDelta = new Vector2(leftBehindNeighbors.rectTransform.sizeDelta.x, leftBehindHeight * 100);

        //create the dictionary
        neighborsRep = new Dictionary<int, int>();
        neighborsRep.Add(1, firstOrder);
        neighborsRep.Add(2, secondOrder);
        neighborsRep.Add(3, thirdOrder);
        neighborsRep.Add(0, leftBehind);

        float a1 = firstOrder * 100 / (float)totalNeighbors;
        float a2 = Mathf.Abs(Math.Min((a1 - 40)/40,0));
        
        float b1 = totalNeighbors - firstOrder - 1;
        b1*=100/(float)totalNeighbors;
        float b2 = Math.Min(Mathf.Max((b1 - 10)/60,0),1);


        hasLeftBehind = leftBehind > 0;
        networkScore = Mathf.Max(a2, b2);


    }



}
