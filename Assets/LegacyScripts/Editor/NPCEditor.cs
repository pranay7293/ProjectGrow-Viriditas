using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using TMPro;
using System.Diagnostics;
using Sirenix.OdinInspector.Editor;

[CustomEditor(typeof(NPC), true)]  // true = for NPC and all derived classes
public class NPCEditor : OdinEditor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        NPC npc = (NPC)target;

        if (npc.nameLabel != null)
            npc.nameLabel.text = npc.name;

    }

}
