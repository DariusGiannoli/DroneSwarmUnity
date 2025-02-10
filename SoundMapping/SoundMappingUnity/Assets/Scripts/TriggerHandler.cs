using UnityEngine;
using UnityEngine.Events; // For UnityEvent
using UnityEditor;
using System; // For PropertyDrawer

public class TriggerHandlerWithCallback : MonoBehaviour
{
    [TagSelector] // Custom attribute for tag selection
    public string targetTag; // Tag to filter the objects

    [SerializeField] bool useUnityEvent = true;
    [SerializeField] bool isStart = true;

    public UnityEvent onTriggerEnter; // Callback to assign in the Inspector

    private GameObject gm;

    void Start()
    {
        if (useUnityEvent)
        {
            gm = GameObject.FindGameObjectWithTag("GameManager");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            if (useUnityEvent)
            {
                if (isStart)
                {
                    gm.GetComponent<Timer>().StartTimer();
                }
                else
                {
                    gm.GetComponent<Timer>().StopTimer();
                }
            }
            else
            {
                onTriggerEnter?.Invoke(); // Call the assigned callback
            }
            
        }
    }
}

public class TagSelectorAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(TagSelectorAttribute))]
public class TagSelectorPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType == SerializedPropertyType.String)
        {
            EditorGUI.BeginProperty(position, label, property);
            property.stringValue = EditorGUI.TagField(position, label, property.stringValue);
            EditorGUI.EndProperty();
        }
        else
        {
            EditorGUI.PropertyField(position, property, label);
            Debug.LogWarning("TagSelector can only be used with string properties.");
        }
    }
}
#endif