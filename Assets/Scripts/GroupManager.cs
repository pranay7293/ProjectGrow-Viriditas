using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class GroupManager : MonoBehaviour
{
    public static GroupManager Instance { get; private set; }

    [SerializeField] private float groupFormationDistance = 2f;
    [SerializeField] private float groupMovementSpeed = 3f;

    private Dictionary<string, List<UniversalCharacterController>> activeGroups = new Dictionary<string, List<UniversalCharacterController>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FormGroup(List<UniversalCharacterController> characters)
    {
        string groupId = System.Guid.NewGuid().ToString();
        activeGroups[groupId] = characters;

        foreach (var character in characters)
        {
            character.AddState(UniversalCharacterController.CharacterState.FormingGroup);
        }

        StartCoroutine(FinishGroupFormation(groupId, characters));
    }

    private IEnumerator FinishGroupFormation(string groupId, List<UniversalCharacterController> characters)
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
        if (activeGroups.TryGetValue(groupId, out List<UniversalCharacterController> groupMembers))
        {
            if (!groupMembers.Contains(character))
            {
                groupMembers.Add(character);
                character.JoinGroup(groupId);
            }
        }
    }

    public void DisbandGroup(string groupId)
    {
        if (activeGroups.TryGetValue(groupId, out List<UniversalCharacterController> characters))
        {
            foreach (var character in characters)
            {
                character.LeaveGroup();
            }
            activeGroups.Remove(groupId);
        }
    }

    public void MoveGroup(string groupId, Vector3 destination)
    {
        if (activeGroups.TryGetValue(groupId, out List<UniversalCharacterController> characters))
        {
            Vector3 groupCenter = GetGroupCenter(characters);
            Vector3 moveDirection = (destination - groupCenter).normalized;

            for (int i = 0; i < characters.Count; i++)
            {
                Vector3 offset = CalculateFormationOffset(i, characters.Count);
                Vector3 targetPosition = destination + offset;
                characters[i].MoveTo(targetPosition);
            }
        }
    }

    private void ArrangeGroupFormation(string groupId)
    {
        if (activeGroups.TryGetValue(groupId, out List<UniversalCharacterController> characters))
        {
            Vector3 groupCenter = GetGroupCenter(characters);

            for (int i = 0; i < characters.Count; i++)
            {
                Vector3 offset = CalculateFormationOffset(i, characters.Count);
                Vector3 targetPosition = groupCenter + offset;
                characters[i].MoveTo(targetPosition);
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
        foreach (var group in activeGroups.Values)
        {
            if (group.Contains(character))
            {
                return true;
            }
        }
        return false;
    }

    public List<UniversalCharacterController> GetGroupMembers(string groupId)
    {
        if (activeGroups.TryGetValue(groupId, out List<UniversalCharacterController> characters))
        {
            return new List<UniversalCharacterController>(characters);
        }
        return new List<UniversalCharacterController>();
    }
}