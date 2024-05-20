using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Diagnostics;
using Sirenix.OdinInspector.Editor;


[CustomEditor(typeof(NPC_Location), true)]  // true = for NPC_Location and all derived classes
public class NPCLocationEditor : OdinEditor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        NPC_Location location = (NPC_Location)target;

        TextMeshPro label = location.GetComponentInChildren<TextMeshPro>();
        if (label != null)
            label.text = location.locationName;

    }

}
