using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GroupManager : MonoBehaviour
{
    public static GroupManager Instance { get; private set; }

    [SerializeField] private float groupFormationDistance = 2f;
    [SerializeField] private float groupMovementSpeed = 3f;
    [SerializeField] private float maxGroupDuration = 120f; // 2 minutes
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
        CheckGroupDurations();
        MoveGroups();
    }

    public void FormGroup(List<UniversalCharacterController> characters)
    {
        characters = characters.Where(c => !c.IsPlayerControlled).ToList();

        if (characters.Count == 0)
        {
            Debug.LogWarning("FormGroup: No characters to form a group.");
            return;
        }

        string groupId = System.Guid.NewGuid().ToString();
        Group newGroup = new Group(groupId, characters);
        activeGroups[groupId] = newGroup;

        foreach (var character in characters)
        {
            character.AddState(UniversalCharacterController.CharacterState.FormingGroup);
        }

        StartCoroutine(FinishGroupFormation(groupId, characters));
    }

    private System.Collections.IEnumerator FinishGroupFormation(string groupId, List<UniversalCharacterController> characters)
    {
        yield return new WaitForSeconds(2f);  // Adjust time as needed

        foreach (var character in characters)
        {
            character.RemoveState(UniversalCharacterController.CharacterState.FormingGroup);
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
                character.JoinGroup(groupId);
            }
        }
    }

    private void MoveGroups()
    {
        foreach (var group in activeGroups.Values)
        {
            if (Time.time - group.LastMovementTime > GROUP_MOVEMENT_INTERVAL)
            {
                Vector3 newDestination = GetRandomDestinationForGroup(group);
                MoveGroup(group.Id, newDestination);
                group.LastMovementTime = Time.time;
            }

            if (Random.value < GROUP_DISSOLUTION_CHANCE)
            {
                DisbandGroup(group.Id);
            }
        }
    }

private Vector3 GetRandomDestinationForGroup(Group group)
{
    // Get a list of all available locations
    List<string> allLocations = LocationManagerMaster.Instance.GetAllLocations();
    
    // Choose a random location
    string randomLocation = allLocations[Random.Range(0, allLocations.Count)];
    
    // Get a waypoint near the chosen location
    return WaypointsManager.Instance.GetWaypointNearLocation(randomLocation);
}

    public void DisbandGroup(string groupId)
    {
        if (activeGroups.TryGetValue(groupId, out Group group))
        {
            foreach (var character in group.Members)
            {
                character.LeaveGroup(false);
            }
            activeGroups.Remove(groupId);
        }
    }

   public void MoveGroup(string groupId, Vector3 destination)
    {
        if (activeGroups.TryGetValue(groupId, out Group group))
        {
            for (int i = 0; i < group.Members.Count; i++)
            {
                Vector3 offset = CalculateFormationOffset(i, group.Members.Count);
                Vector3 targetPosition = destination + offset;
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
                group.Members[i].MoveTo(targetPosition);
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

    private void CheckGroupDurations()
    {
        List<string> groupsToDisband = new List<string>();

        foreach (var group in activeGroups.Values)
        {
            group.Duration += Time.deltaTime;
            if (group.Duration >= maxGroupDuration)
            {
                groupsToDisband.Add(group.Id);
            }
        }

        foreach (var groupId in groupsToDisband)
        {
            DisbandGroup(groupId);
        }
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
        }
    }
}