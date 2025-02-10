using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSelectorScript : MonoBehaviour
{
    // Track which scene is currently loaded (by name)
    private string lastLoadedScene = null;

    private string setupScene = "Setup";


    /// <summary>
    /// Loads the specified scene (by name) additively, 
    /// and unloads any previously loaded scene.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load (without .unity)</param>
    public void SelectTraining(string sceneName)
    {
        // If a scene is already loaded, unload it
        if (!string.IsNullOrEmpty(lastLoadedScene))
        {
            SceneManager.UnloadSceneAsync(lastLoadedScene);
        }

        // unload the setup scene if it is loaded
        if (SceneManager.GetSceneByName(setupScene).isLoaded)
        {
            SceneManager.UnloadSceneAsync(setupScene);
        }

        // Load the new scene additively
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

        // Remember the new scene
        lastLoadedScene = sceneName;

        Debug.Log($"Loaded scene: {sceneName}, unloaded scene: {lastLoadedScene}");


        //swarmModel.restart();

        //load the setup scene
        SceneManager.LoadScene(setupScene, LoadSceneMode.Additive);
    }
}
