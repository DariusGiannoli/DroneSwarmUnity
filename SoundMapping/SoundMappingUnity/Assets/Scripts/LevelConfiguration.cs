using UnityEngine;

public class LevelConfiguration : MonoBehaviour
{
    public bool SoftStart = true;


    [Header("Control Settings")]
    [SerializeField] private bool controlMovement = true;
    [SerializeField] private bool controlSpreadness = true;
    [SerializeField] private bool controlEmbodiement = true;
    [SerializeField] private bool controlDesembodiement = false;
    [SerializeField] private bool controlSelection = true;
    [SerializeField] private bool controlRotation = true;

    [Header("Haptics Settings")]
    [SerializeField] private bool hapticsObstacle = true;
    [SerializeField] private bool hapticsNetwork = true;
    [SerializeField] private bool hapticsForces = true;
    [SerializeField] private bool hapticsCrash = true;
    [SerializeField] private bool hapticsController = true;

    [Header("Start Configuration")]
    [SerializeField] private bool startEmbodied = false;
    [SerializeField] private int droneID = 0;


    [Header("Audio Settings")]
    [SerializeField] private bool audioIsolation = true;
    [SerializeField] private bool audioSpreadness = true;

    [Header("Spawn Settings")]
    [SerializeField] private bool needToSpawn = true;
    [SerializeField] private int numDrones = 20;
    [SerializeField] private float spawnRadius = 3f;
    [SerializeField] private float startSperation = 1f;

    [Header("Other")]
    [SerializeField] private bool saveData = false;
    [SerializeField] private bool miniMap = false;
    [SerializeField] private bool showText = false;

    // Corresponding static variables
    public static bool _control_movement;
    public static bool _control_spreadness;
    public static bool _control_embodiement;
    public static bool _control_desembodiement;
    public static bool _control_selection;
    public static bool _control_rotation;

    public static bool _Haptics_Obstacle;
    public static bool _Haptics_Network;
    public static bool _Haptics_Forces;
    public static bool _Haptics_Crash;
    public static bool _Haptics_Controller;

    public static bool _startEmbodied;
    public static int _droneID;



    public static bool _Audio_isolation;
    public static bool _Audio_spreadness;

    public static bool _NeedToSpawn;
    public static int _NumDrones;
    public static float _SpawnRadius;
    public static float _StartSperation;

    public static bool _SaveData;
    public static bool _MiniMap;
    public static bool _ShowText;





    void OnValidate()
    {
        if (Time.timeSinceLevelLoad < 2.9f && SoftStart)
        {
            return;
        }


        _control_movement = controlMovement;
        _control_spreadness = controlSpreadness;
        _control_embodiement = controlEmbodiement;
        _control_desembodiement = controlDesembodiement;
        _control_selection = controlSelection;
        _control_rotation = controlRotation;

        _Haptics_Obstacle = hapticsObstacle;
        _Haptics_Network = hapticsNetwork;
        _Haptics_Forces = hapticsForces;
        _Haptics_Crash = hapticsCrash;
        _Haptics_Controller = hapticsController;

        _startEmbodied = startEmbodied;
        _droneID = droneID;

        _Audio_isolation = audioIsolation;
        _Audio_spreadness = audioSpreadness;

        _NeedToSpawn = needToSpawn;
        _NumDrones = numDrones;
        _SpawnRadius = spawnRadius;
        _StartSperation = startSperation;

        _SaveData = saveData;
        _MiniMap = miniMap;
        _ShowText = showText;
    }

    void SoftStartFunc()
    {
        //put all the hapticd and ausdio to false
        _Haptics_Obstacle = false;
        _Haptics_Network = false;
        _Haptics_Forces = false;
        _Haptics_Crash = false;
        _Haptics_Controller = false;
        
        _Audio_isolation = false;
        _Audio_spreadness = false;

        _control_movement = controlMovement;
        _control_spreadness = controlSpreadness;
        _control_embodiement = controlEmbodiement;
        _control_desembodiement = controlDesembodiement;
        _control_selection = controlSelection;
        _control_rotation = controlRotation;

        _startEmbodied = startEmbodied;
        _droneID = droneID;

        _NeedToSpawn = needToSpawn;
        _NumDrones = numDrones;
        _SpawnRadius = spawnRadius;
        _StartSperation = startSperation;

        _SaveData = saveData;
        _MiniMap = miniMap;
        _ShowText = showText;

        //call the onvalidate 3 seconds later
        Invoke("lateStart", 3f);
    }

    void lateStart()
    {
        OnValidate();
        HapticsTest.lateStart();
    }

    void Awake()
    {
        if(!SceneSelectorScript._haptics)
        {
            print("Haptics is off");
            hapticsObstacle = false;
            hapticsNetwork = false;
            hapticsForces = false;
            hapticsCrash = false;
            hapticsController = false;

            audioIsolation = false;
            audioSpreadness = false;

            showText = true;
        }


        if(SoftStart)
        {
            SoftStartFunc();
        }else
        {
            OnValidate();
        }
    }

    public static GameObject swarmHolder
    {
        get
        {
            return GameObject.FindGameObjectWithTag("SpawnLess");
        }
    }

    void Update()
    {
    }

}
