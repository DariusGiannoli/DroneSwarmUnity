// Name this script "ColorSpacerExample"

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BrownToBlueNoise))]
public class BrownToBlueNoiseInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BrownToBlueNoise myScript = (BrownToBlueNoise)target;
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Shrink"))
        {
            myScript.Shrink();
        }

        if(GUILayout.Button("Expand"))
        {
            myScript.Expand();
        }
        EditorGUILayout.EndHorizontal();
    }
}

//same thing for Pitch
[CustomEditor(typeof(PitchNoise))]
public class PitchNoiseInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PitchNoise myScript = (PitchNoise)target;
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Shrink"))
        {
            myScript.Shrink();
        }

        if(GUILayout.Button("Expand"))
        {
            myScript.Expand();
        }
        EditorGUILayout.EndHorizontal();
    }
}

//same thing for BrownToPinkNoise
[CustomEditor(typeof(BrownToPinkNoise))]
public class buttonInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BrownToPinkNoise myScript = (BrownToPinkNoise)target;
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Shrink"))
        {
            myScript.Shrink();
        }

        if(GUILayout.Button("Expand"))
        {
            myScript.Expand();
        }
        EditorGUILayout.EndHorizontal();
    }
}

//same thing for BandpassNoise
[CustomEditor(typeof(BandpassNoise))]
public class BandpassNoiseInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BandpassNoise myScript = (BandpassNoise)target;
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Shrink"))
        {
            myScript.Shrink();
        }

        if(GUILayout.Button("Expand"))
        {
            myScript.Expand();
        }
        EditorGUILayout.EndHorizontal();
    }
}

//same thing for ArmyShrink
[CustomEditor(typeof(ArmyShrink))]
public class ArmyShrinkInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ArmyShrink myScript = (ArmyShrink)target;
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button("Shrink"))
        {
            myScript.Shrink();
        }

        if(GUILayout.Button("Expand"))
        {
            myScript.Expand();
        }
        EditorGUILayout.EndHorizontal();
    }
}

