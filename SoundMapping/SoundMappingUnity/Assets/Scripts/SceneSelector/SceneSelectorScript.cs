using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSelectorScript : MonoBehaviour
{
    private string lastLoadedScene = null;
    public static int experimentNumber = 0;

    [HideInInspector]public bool isLoading = false;
    
    [HideInInspector] public List<string> scenes = new List<string>();
    [HideInInspector] public List<string> scenesPlayed = new List<string>();

    [HideInInspector] private string setupScene = "Setup";
    [HideInInspector] public string ObstacleFPV = "101 DemoFPV";
    [HideInInspector] public string ObstacleTPV = "100 DemoTDV";
    [HideInInspector] public string CollectibleFPV = "103 CollectiblesFPV";
    [HideInInspector] public string CollectibleTPV = "102 CollectiblesTDV";

    [HideInInspector] public string assetPathTraining = "Assets/Scenes/TrainingFinal";


    public static string pid = "default";
    public static bool _order = false;
    public static bool _haptics = true;

    public bool hapticsEnabled = true;

    void Start()
    {
        // For initial cleanup.
        StartCoroutine(UnloadAllScenesExcept("Scene Selector"));

        // If using the UnityEditor to populate scenes:
        #if UNITY_EDITOR
        string[] sceneGuids = UnityEditor.AssetDatabase.FindAssets("t:Scene", new[] { assetPathTraining });
        foreach (string guid in sceneGuids)
        {
            string scenePath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            scenes.Add(sceneName);
        }
        #endif
    }

    public void OnHapticsChanged()
    {
    }

    IEnumerator UnloadAllScenesExcept(string sceneToKeep)
    {
        isLoading = true;
        List<Scene> loadedScenes = new List<Scene>();
        print(SceneManager.sceneCount);
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            loadedScenes.Add(SceneManager.GetSceneAt(i));
        }

        foreach (Scene scene in loadedScenes)
        {
            if (scene.name != sceneToKeep)
            {
                AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
                if (op != null)
                {
                    yield return new WaitUntil(() => op.isDone);
                    Debug.Log($"Unloaded scene: {scene.name}");
                }
                else
                {
                    Debug.LogWarning($"UnloadSceneAsync returned null for scene {scene.name}");
                }
            }
        }
        isLoading = false;
    }

    IEnumerator LoadTrainingScene(string sceneName)
    {
        if (isLoading)
        {
            Debug.LogWarning("Scene loading already in progress.");
            yield break;
        }
        // Unload all scenes except the persistent one.
        yield return StartCoroutine(UnloadAllScenesExcept("Scene Selector"));

        // Load the new training scene additively.
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return new WaitUntil(() => loadOp.isDone);
        lastLoadedScene = sceneName;
        Debug.Log($"Loaded training scene: {sceneName}");

        
        // Load the setup scene additively.
        AsyncOperation setupLoadOp = SceneManager.LoadSceneAsync(setupScene, LoadSceneMode.Additive);
        yield return new WaitUntil(() => setupLoadOp.isDone);
        Debug.Log($"Loaded setup scene: {setupScene}");
    }

    public void SelectTraining(string sceneName)
    {
        StartCoroutine(LoadTrainingScene(sceneName));
    }

    public void SelectTrainingFromButton(string sceneName)
    {
        _haptics = hapticsEnabled;
        this.GetComponent<ExperimentSetupS>().GUIIDisable();
        
        scenesPlayed = new List<string>(scenes);
        addStudyScene();

        experimentNumber = scenesPlayed.IndexOf(sceneName) - 1;
        
        NextScene();
    }

    public void StartTraining(string PID)
    {

        hapticsEnabled = _haptics;

        // Set up your experiment order.
        scenesPlayed = new List<string>(scenes);

        //scenesPlayed.Clear();

        addStudyScene();


        experimentNumber = -1;
//        print("Haptics: " + Haptics + " Order: " + Order + " PID: " + PID);
        NextScene();
    }

    public void addStudyScene()
    {
        
        if (!_order)
        {
            scenesPlayed.Add(ObstacleFPV);
          //  scenesPlayed.Add(ObstacleFPV);
            scenesPlayed.Add(ObstacleTPV);
         //   scenesPlayed.Add(ObstacleTPV);
            scenesPlayed.Add(CollectibleFPV);
        //    scenesPlayed.Add(CollectibleFPV);
            scenesPlayed.Add(CollectibleTPV);
        //    scenesPlayed.Add(CollectibleTPV);
        }
        else
        {
            scenesPlayed.Add(ObstacleTPV);
           // scenesPlayed.Add(ObstacleTPV);
            scenesPlayed.Add(ObstacleFPV);
          //  scenesPlayed.Add(ObstacleFPV);
            scenesPlayed.Add(CollectibleTPV);
          //  scenesPlayed.Add(CollectibleTPV);
            scenesPlayed.Add(CollectibleFPV);
         //   scenesPlayed.Add(CollectibleFPV);
        }
    }

    public static void nextScene()
    {
        GameObject.FindObjectOfType<SceneSelectorScript>().NextScene();
    }

    public void NextScene()
    {
        experimentNumber++;
        if (experimentNumber >= scenesPlayed.Count)
        {
            // End of experiment; unload all non-persistent scenes.
            StartCoroutine(UnloadAllScenesExcept("Scene Selector"));
            return;
        }

        SelectTraining(scenesPlayed[experimentNumber]);
    }

    public static List<int> tutorialPlayed = new List<int>();
    public static bool needToWatchTutorial()
    {
        //print("Experiment Number: " + tutorialPlayed.Count);
        if(tutorialPlayed.Contains(experimentNumber))
        {
            return false;
        }
        else
        {
            tutorialPlayed.Add(experimentNumber);
            return true;
        }
    }

    public void ResetScene()
    {
        if (experimentNumber >= 0 && experimentNumber < scenesPlayed.Count)
        {
            // Reload the current scene.
            SelectTraining(scenesPlayed[experimentNumber]);
        }
    }

    public static void reset()
    {
        GameObject.FindObjectOfType<SceneSelectorScript>().ResetScene();
    }
}
