// using UnityEngine;
// using Photon.Pun;

// public abstract class Character : MonoBehaviourPunCallbacks, IPunObservable
// {
//     public string characterName;
//     // public float health;
//     public float speed;
//     public Transform characterTransform;

//     protected virtual void Awake()
//     {
//         characterTransform = transform;
//     }

//     public abstract void Move(Vector3 direction);
//     // public abstract void TakeDamage(float amount);

//     public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
//     {
//         if (stream.IsWriting)
//         {
//             // Send data to other players
//             stream.SendNext(characterName);
//             // stream.SendNext(health);
//         }
//         else
//         {
//             // Receive data from other players
//             characterName = (string)stream.ReceiveNext();
//             // health = (float)stream.ReceiveNext();
//         }
//     }
// }