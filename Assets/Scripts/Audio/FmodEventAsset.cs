using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ScriptableObject wrapper for FMOD events. Use this instead of string paths.
/// Create via Assets > Create > GGJ > FMOD Event.
/// </summary>
[CreateAssetMenu(fileName = "New Fmod Event", menuName = "GGJ/FMOD Event", order = 0)]
public class FmodEventAsset : ScriptableObject
{
    [SerializeField] private FMODUnity.EventReference eventReference;

    public FMODUnity.EventReference EventReference => eventReference;
    public bool IsNull => eventReference.IsNull;
}

#if UNITY_EDITOR
[CustomEditor(typeof(FmodEventAsset))]
public class FmodEventAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("eventReference"), new GUIContent("Event"));
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
