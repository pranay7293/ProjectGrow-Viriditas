using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GroupManager : MonoBehaviour
{
    public static GroupManager Instance { get; private set; }

    [SerializeField] private float groupFormationDistance = 2f;
    [SerializeField] private float groupMovementSpeed = 1.5f;
    [SerializeField] private float maxGroupDuration = 30f;
    [SerializeField] private float cohesionStrength = 0.5f;
    [SerializeField] private float alignmentStrength = 0.3f;
    [SerializeField] private float separationStrength = 0.7f;
    [SerializeField] private float separationDistance = 1.5f;

    private const float GROUP_DISSOLUTION_CHANCE = 0.1f;
    private const float GROUP_MOVEMENT_INTERVAL = 15f;

    private Dictionary<string, Group> activeGroups = new Dictionary<string, Group>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateGroups();
    }

    public void FormGroup(List<UniversalCharacterController> characters)
    {
        characters = characters.Where(c => !c.IsPlayerControlled).ToList();

        if (characters.Count < 2)
        {
            Debug.LogWarning("FormGroup: Not enough characters to form a group.");
            return;
        }

        string groupId = System.Guid.NewGuid().ToString();
        Group newGroup = new Group(groupId, characters);
        activeGroups[groupId] = newGroup;

        foreach (var character in characters)
        {
            character.AddState(CharacterState.FormingGroup);
        }

        StartCoroutine(FinishGroupFormation(groupId, characters));
    }

    private System.Collections.IEnumerator FinishGroupFormation(string groupId, List<UniversalCharacterController> characters)
    {
        yield return new WaitForSeconds(2f);

        foreach (var character in characters)
        {
            character.RemoveState(CharacterState.FormingGroup);
            character.AddState(CharacterState.InGroup);
            character.JoinGroup(groupId);
        }

        ArrangeGroupFormation(groupId);
    }

    public void AddToGroup(string groupId, UniversalCharacterController character)
    {
        if (character.IsPlayerControlled)
        {
            Debug.Log("AddToGroup: Player-controlled character cannot be added without consent.");
            return;
        }

        if (activeGroups.TryGetValue(groupId, out Group group))
        {
            if (!group.Members.Contains(character))
            {
                group.Members.Add(character);
                character.AddState(CharacterState.InGroup);
                character.JoinGroup(groupId);
            }
        }
    }

    private void UpdateGroups()
    {
        List<string> groupsToDisband = new List<string>();
        foreach (var group in activeGroups.Values)
        {
            UpdateGroupMovement(group);
            group.Duration += Time.deltaTime;

            if (group.Duration >= maxGroupDuration || Random.value < GROUP_DISSOLUTION_CHANCE * Time.deltaTime)
            {
                groupsToDisband.Add(group.Id);
            }
        }

        foreach (var groupId in groupsToDisband)
        {
            DisbandGroup(groupId);
        }
    }

    private void UpdateGroupMovement(Group group)
{
    Vector3 cohesion = CalculateCohesion(group);
    Vector3 alignment = CalculateAlignment(group);
    Vector3 separation = CalculateSeparation(group);

    Vector3 groupCenter = GetGroupCenter(group.Members);
    Vector3 newDestination = groupCenter + cohesion + alignment + separation;

    // Find the nearest waypoint to the new destination
    Vector3 nearestWaypoint = WaypointsManager.Instance.GetNearestWaypoint(newDestination);

    foreach (var member in group.Members)
    {
        Vector3 memberDestination = nearestWaypoint + (member.transform.position - groupCenter).normalized * groupFormationDistance;
        member.MoveWhileInState(memberDestination, groupMovementSpeed);
    }
}

    private Vector3 CalculateCohesion(Group group)
    {
        Vector3 center = GetGroupCenter(group.Members);
        return (center - group.Members[0].transform.position) * cohesionStrength;
    }

    private Vector3 CalculateAlignment(Group group)
    {
        Vector3 averageVelocity = Vector3.zero;
        foreach (var member in group.Members)
        {
            averageVelocity += member.GetComponent<Rigidbody>().velocity;
        }
        averageVelocity /= group.Members.Count;
        return averageVelocity * alignmentStrength;
    }

    private Vector3 CalculateSeparation(Group group)
    {
        Vector3 separationForce = Vector3.zero;
        foreach (var member in group.Members)
        {
            foreach (var otherMember in group.Members)
            {
                if (member != otherMember)
                {
                    Vector3 diff = member.transform.position - otherMember.transform.position;
                    if (diff.magnitude < separationDistance)
                    {
                        separationForce += diff.normalized / diff.magnitude;
                    }
                }
            }
        }
        return separationForce * separationStrength;
    }

    public void DisbandGroup(string groupId)
    {
        if (activeGroups.TryGetValue(groupId, out Group group))
        {
            foreach (var character in group.Members)
            {
                character.RemoveState(CharacterState.InGroup);
                character.LeaveGroup(false);
            }
            activeGroups.Remove(groupId);
        }
    }

    public void MoveGroup(string groupId, Vector3 destination)
{
    if (activeGroups.TryGetValue(groupId, out Group group))
    {
        Vector3 nearestWaypoint = WaypointsManager.Instance.GetNearestWaypoint(destination);
        Vector3 groupCenter = GetGroupCenter(group.Members);
        Vector3 moveDirection = (nearestWaypoint - groupCenter).normalized;

        for (int i = 0; i < group.Members.Count; i++)
        {
            Vector3 offset = CalculateFormationOffset(i, group.Members.Count);
            Vector3 targetPosition = nearestWaypoint + offset;
            group.Members[i].MoveWhileInState(targetPosition, groupMovementSpeed);
        }
    }
}

    private void ArrangeGroupFormation(string groupId)
    {
        if (activeGroups.TryGetValue(groupId, out Group group))
        {
            Vector3 groupCenter = GetGroupCenter(group.Members);

            for (int i = 0; i < group.Members.Count; i++)
            {
                Vector3 offset = CalculateFormationOffset(i, group.Members.Count);
                Vector3 targetPosition = groupCenter + offset;
                group.Members[i].MoveWhileInState(targetPosition, groupMovementSpeed);
            }
        }
    }

    private Vector3 GetGroupCenter(List<UniversalCharacterController> characters)
    {
        Vector3 sum = Vector3.zero;
        foreach (var character in characters)
        {
            sum += character.transform.position;
        }
        return sum / characters.Count;
    }

    private Vector3 CalculateFormationOffset(int index, int totalCharacters)
    {
        float angle = index * (360f / totalCharacters) * Mathf.Deg2Rad;
        float x = Mathf.Sin(angle) * groupFormationDistance;
        float z = Mathf.Cos(angle) * groupFormationDistance;
        return new Vector3(x, 0, z);
    }

    public bool IsInGroup(UniversalCharacterController character)
    {
        return activeGroups.Values.Any(group => group.Members.Contains(character));
    }

    public List<UniversalCharacterController> GetGroupMembers(string groupId)
    {
        if (activeGroups.TryGetValue(groupId, out Group group))
        {
            return new List<UniversalCharacterController>(group.Members);
        }
        return new List<UniversalCharacterController>();
    }

    private class Group
    {
        public string Id { get; private set; }
        public List<UniversalCharacterController> Members { get; private set; }
        public float Duration { get; set; }
        public float LastMovementTime { get; set; }

        public Group(string id, List<UniversalCharacterController> members)
        {
            Id = id;
            Members = members;
            Duration = 0f;
            LastMovementTime = Time.time;
        }
    }
}