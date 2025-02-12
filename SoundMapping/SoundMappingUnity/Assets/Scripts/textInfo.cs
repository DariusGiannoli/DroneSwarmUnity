using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class textInfo : MonoBehaviour
{
    public TextMeshProUGUI connexionText;
    public TextMeshProUGUI SpreadnessText;
    public TextMeshProUGUI IsolationText;
    public TextMeshProUGUI DroneCrashText;
    public TextMeshProUGUI SpreadnessSwarmScore;
    // Start is called before the first frame update
    // Update is called once per frame
    void Update()
    {
        if(LevelConfiguration._ShowText)
        {
            connexionText.text = "Connexion: " + getOneDecimal(swarmModel.swarmConnectionScore);
            SpreadnessText.text = "Spreadness: " + getOneDecimal(swarmModel.desiredSeparation);
            IsolationText.text = "Isolation : " + swarmModel.numberOfDroneDiscionnected.ToString();
            DroneCrashText.text = "Drone Crash : " + swarmModel.numberOfDroneCrashed.ToString();
            SpreadnessSwarmScore.text = "Swarm spreadness : " + getOneDecimal(swarmModel.swarmAskingSpreadness);
        }else
        {
            connexionText.text = "";
            SpreadnessText.text = "";
            IsolationText.text = "";
            DroneCrashText.text = "";
            SpreadnessSwarmScore.text = "";
        }
    }

    string getOneDecimal(float value)
    {
        return value.ToString("F1");
    }
}
