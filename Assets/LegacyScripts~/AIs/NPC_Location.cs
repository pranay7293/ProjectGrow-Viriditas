using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using ListExtensions;
using System.Linq;

public class NPC_Location : MonoBehaviour
{
    // TODO - ideally these would all be read-only properties exposed in the Inspector
    public string locationName;
    public Color color;
    public TextMeshPro label;
    public Transform interactPoint;  // where the NPC stands to perform their custom action
    public Transform faceMe;  // where the NPC looks to perform their custom action

    private Vector3[] standPositionOffsets = new Vector3[0];
    private int nextStandPositionOffsetIndex = 0;

    private void Awake()
    {
        label.text = locationName;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
            mr.material.color = color;

        mr = faceMe.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.material.color = color;

        // Populate standing point offsets for NPCs.
        standPositionOffsets = ComputeStandPositionOffsets().Shuffled().ToArray();
    }

    public Vector3 GetAvailableStandPosition()
    {
        // We don't actually check availability we just cycle through them.
        var offset = standPositionOffsets[nextStandPositionOffsetIndex];
        nextStandPositionOffsetIndex = (nextStandPositionOffsetIndex + 1) % standPositionOffsets.Length;
        return transform.position + offset;
    }

    private IList<Vector3> ComputeStandPositionOffsets()
    {
        var size = 10f * transform.localScale.x;  // Assumes mesh is 10 meter square plane. 
        var spacing = 1.25f;
        var ret = new List<Vector3>();
        for (var x = -size / 2; x < size / 2; x += spacing)
        {
            for (var z = -size / 2; z < size / 2; z += spacing)
            {
                ret.Add(new Vector3(x, 0, z));
            }
        }
        return ret;
    }

    // goes on a collider that is a trigger
    // notices when objects (with rigidbodies and colliders) enter and leave the trigger
    // checks to see if they are NPCs and if so updates them about their movements.
    // does the same for the player.
    // NOTE - this assumes these volumes never overlap, and is pretty specific to just this one NPC need
    private void OnTriggerEnter(Collider other)
    {
        NPC npc = other.gameObject.GetComponentInParent<NPC>();

        if (npc != null)
            npc.currentLocation = locationName;

        Player player = other.gameObject.GetComponentInParent<Player>();

        if (player != null)
            player.currentLocation = locationName;
    }

    private void OnTriggerExit(Collider other)
    {
        NPC npc = other.gameObject.GetComponentInParent<NPC>();

        if (npc != null)
            npc.currentLocation = NPC_Data.unknownLocationName;

        Player player = other.gameObject.GetComponentInParent<Player>();

        if (player != null)
            player.currentLocation = NPC_Data.unknownLocationName;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        foreach (var offset in standPositionOffsets)
        {
            Gizmos.DrawWireSphere(transform.position + offset, 0.1f);
        }
    }
}
