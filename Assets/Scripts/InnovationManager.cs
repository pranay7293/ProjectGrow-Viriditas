// using UnityEngine;
// using Photon.Pun;
// using System.Collections.Generic;

// public class InnovationManager : MonoBehaviourPunCallbacks
// {
//     public void AttemptInnovation(string playerName, string innovationDescription)
//     {
//         bool success = EvaluateInnovationSuccess();
//         if (success)
//         {
//             GameManager.Instance.UpdatePlayerScore(playerName, 75);
//             string completedMilestone = GetRandomIncompleteMilestone();
//             if (completedMilestone != null)
//         {
//             int milestoneIndex = GameManager.Instance.GetCurrentChallenge().milestones.IndexOf(completedMilestone);
//             GameManager.Instance.CompleteMilestone(playerName, completedMilestone, milestoneIndex);
//         }
//         }
//         else
//         {
//             GameManager.Instance.UpdatePlayerScore(playerName, -25);
//             GameManager.Instance.AddPlayerAction($"Failed innovation attempt: {innovationDescription}");
//         }
//     }

//     private bool EvaluateInnovationSuccess()
//     {
//         return Random.value > 0.5f;
//     }

//     private string GetRandomIncompleteMilestone()
//     {
//         var currentChallenge = GameManager.Instance.GetCurrentChallenge();
//         var incompleteMilestones = new List<string>();

//         foreach (var milestone in currentChallenge.milestones)
//         {
//             if (!GameManager.Instance.IsMilestoneCompleted(milestone))
//             {
//                 incompleteMilestones.Add(milestone);
//             }
//         }

//         if (incompleteMilestones.Count > 0)
//         {
//             return incompleteMilestones[Random.Range(0, incompleteMilestones.Count)];
//         }

//         return null;
//     }
// }