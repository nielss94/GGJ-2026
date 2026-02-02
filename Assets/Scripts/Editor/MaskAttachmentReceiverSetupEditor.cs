#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Splines;

/// <summary>
/// Editor helpers for MaskAttachmentReceiver: validate feather spline setup and fix missing SplineContainer references.
/// </summary>
[CustomEditor(typeof(MaskAttachmentReceiver))]
public class MaskAttachmentReceiverSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Validate & fix spline setup"))
        {
            ValidateAndFixSplineSetup();
        }
    }

    private void ValidateAndFixSplineSetup()
    {
        var receiver = (MaskAttachmentReceiver)target;
        var so = new SerializedObject(receiver);
        var splineSetupsProp = so.FindProperty("splineSetups");
        if (splineSetupsProp == null || !splineSetupsProp.isArray) return;

        var root = receiver.transform.root;
        var splineContainers = root.GetComponentsInChildren<SplineContainer>(true);

        int fixedCount = 0;
        for (int i = 0; i < splineSetupsProp.arraySize; i++)
        {
            var element = splineSetupsProp.GetArrayElementAtIndex(i);
            var splineContainerProp = element.FindPropertyRelative("splineContainer");
            if (splineContainerProp == null) continue;

            if (splineContainerProp.objectReferenceValue != null)
            {
                var sc = (SplineContainer)splineContainerProp.objectReferenceValue;
                if (sc.Splines != null && sc.Splines.Count > 0)
                    continue;
                Debug.LogWarning($"MaskAttachmentReceiver: Spline Setups[{i}] has a SplineContainer but it has no splines. Add knots (e.g. via HalfCircleSplineBuilder > Build Half Circle).", receiver);
                continue;
            }

            if (splineContainers.Length == 0)
            {
                Debug.LogWarning("MaskAttachmentReceiver: No SplineContainer found in this prefab/hierarchy. Add a child with SplineContainer (e.g. Spline with HalfCircleSplineBuilder).", receiver);
                continue;
            }

            splineContainerProp.objectReferenceValue = splineContainers[0];
            fixedCount++;
        }

        so.ApplyModifiedProperties();
        if (fixedCount > 0)
            Debug.Log($"MaskAttachmentReceiver: Assigned SplineContainer to {fixedCount} spline setup(s).", receiver);
    }
}
#endif
