// using UnityEngine;
// using System.Collections.Generic;
// using System.Linq;

// public class PlayerRankingManager : MonoBehaviour
// {
//     [SerializeField] private Transform playerListContainer;
//     [SerializeField] private GameObject playerProfilePrefab;

//     private Dictionary<string, PlayerProfileUI> playerProfiles = new Dictionary<string, PlayerProfileUI>();

//     public void InitializeProfiles(List<UniversalCharacterController> characters)
//     {
//         foreach (var character in characters)
//         {
//             CreatePlayerProfile(character);
//         }
//         UpdateRankings();
//     }

//     private void CreatePlayerProfile(UniversalCharacterController character)
//     {
//         GameObject profileObj = Instantiate(playerProfilePrefab, playerListContainer);
//         PlayerProfileUI profile = profileObj.GetComponent<PlayerProfileUI>();

//         profile.SetupProfile(
//             character.characterName,
//             character.characterColor,
//             GetCharacterSilhouette(character.characterName),
//             !character.IsPlayerControlled
//         );

//         playerProfiles[character.characterName] = profile;
//     }

//     public void UpdatePlayerProgress(string playerName, float overallProgress, float personalProgress)
//     {
//         if (playerProfiles.TryGetValue(playerName, out PlayerProfileUI profile))
//         {
//             profile.UpdateProgress(overallProgress, personalProgress);
//         }
//     }

//     public void UpdatePlayerInsights(string playerName, int insightCount)
//     {
//         if (playerProfiles.TryGetValue(playerName, out PlayerProfileUI profile))
//         {
//             profile.UpdateInsights(insightCount);
//         }
//     }

//     public void UpdateRankings()
//     {
//         var sortedProfiles = playerProfiles.OrderByDescending(kvp => GameManager.Instance.GetPlayerScore(kvp.Key)).ToList();

//         for (int i = 0; i < sortedProfiles.Count; i++)
//         {
//             sortedProfiles[i].Value.transform.SetSiblingIndex(i);
//         }
//     }

//     private Sprite GetCharacterSilhouette(string characterName)
//     {
//         // Load the character silhouette sprite based on the character name
//         return Resources.Load<Sprite>($"CharacterSilhouettes/{characterName}");
//     }
// }