using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(SceneSelectorScript))]
public class SceneSelectorScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields (any public fields, etc.)
        DrawDefaultInspector();

        // Reference to the actual script on the GameObject
        SceneSelectorScript myScript = (SceneSelectorScript)target;

        // Label for clarity
        EditorGUILayout.LabelField("Dynamic Scenes from 'Assets/Scenes/Training':", EditorStyles.boldLabel);

        // Find all scenes in "Assets/Scenes/Training"
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes/Training" });
        
        // Create a button for each scene found
        foreach (string guid in sceneGuids)
        {
            // Convert GUID to path, then extract the filename without extension
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (GUILayout.Button(sceneName))
            {
                // Tell our script to load this scene (unload the previous one if any)
                myScript.SelectTraining(sceneName);
            }
        }
    }
}
