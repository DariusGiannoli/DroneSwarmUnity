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
        EditorGUILayout.LabelField("Dynamic Scenes from "+myScript.assetPathTraining+  ":", EditorStyles.boldLabel);

        // Find all scenes in "Assets/Scenes/Training"
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { myScript.assetPathTraining });
        
        // Create a button for each scene found
        foreach (string guid in sceneGuids)
        {
            // Convert GUID to path, then extract the filename without extension
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            string sceneName = Path.GetFileNameWithoutExtension(scenePath);

            if (GUILayout.Button(sceneName))
            {
                // Tell our script to load this scene (unload the previous one if any)
                myScript.SelectTrainingFromButton(sceneName);
            }
        }


        //start vertical layout
        EditorGUILayout.BeginVertical();
        //make a tick box stating Haptics 
        bool newHapticsValue = EditorGUILayout.Toggle("Haptics enabled", myScript.haptics);
        if (newHapticsValue != myScript.haptics)
        {
            myScript.haptics = newHapticsValue;
            myScript.OnHapticsChanged();
        }

        EditorGUILayout.BeginHorizontal();

        //make a button for Demo Scene
        if (GUILayout.Button("Obstacles"))
        {
            // Tell our script to load this scene (unload the previous one if any)
            myScript.SelectTrainingFromButton("DemoFPV");
        }

        if (GUILayout.Button("Collectibles"))
        {
            // Tell our script to load this scene (unload the previous one if any)
            myScript.SelectTrainingFromButton("CollectiblesFPV");
        }

        EditorGUILayout.EndHorizontal();

        //button unload all scenes

        //end vertical layout
        EditorGUILayout.EndVertical();



        //


    }
}
