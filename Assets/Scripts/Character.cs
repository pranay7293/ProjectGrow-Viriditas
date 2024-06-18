using UnityEngine;
using Photon.Pun;

public class Character : MonoBehaviourPun
{
    public string characterName;
    public int characterID;
    public Vector3 startPosition;

    protected virtual void Start()
    {
        if (photonView.IsMine)
        {
            // Initialize player-specific settings
            InitializeCharacter();
        }
    }

    protected virtual void InitializeCharacter()
    {
        transform.position = startPosition;
    }
}
