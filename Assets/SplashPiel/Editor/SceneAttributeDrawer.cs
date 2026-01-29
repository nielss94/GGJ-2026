using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(SceneAttribute))]
public class SceneAttributeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.LabelField(position, label.text, "Use [Scene] with a string field.");
            return;
        }

        var sceneAsset = FindSceneAsset(property.stringValue);
        var newSceneAsset = EditorGUI.ObjectField(position, label, sceneAsset, typeof(SceneAsset), false) as SceneAsset;

        if (newSceneAsset != sceneAsset)
            property.stringValue = newSceneAsset != null ? newSceneAsset.name : "";
    }

    private static SceneAsset FindSceneAsset(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;

        var guids = AssetDatabase.FindAssets("t:SceneAsset " + sceneName);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (asset != null && asset.name == sceneName)
                return asset;
        }
        return null;
    }
}
