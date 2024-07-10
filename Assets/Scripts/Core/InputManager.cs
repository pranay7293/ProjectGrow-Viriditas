using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector3 PlayerRelativeMoveDirection
    {
        get
        {
            var moveVector = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveVector.z += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveVector.z -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveVector.x += 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveVector.x -= 1;

            return moveVector.normalized;
        }
    }

    public bool PlayerRunModifier => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    public bool PlayerJumpActivate => Input.GetKeyDown(KeyCode.Space);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UniversalCharacterController localPlayer = FindLocalPlayer();
            if (localPlayer != null)
            {
                UniversalCharacterController nearestNPC = FindNearestNPC(localPlayer.transform);
                if (nearestNPC != null)
                {
                    Debug.Log($"Initiating dialogue with {nearestNPC.characterName}");
                    DialogueManager.Instance.InitiateDialogue(nearestNPC);
                }
                else
                {
                    Debug.Log("No nearby NPC found");
                }
            }
            else
            {
                Debug.Log("Local player not found or not owned");
            }
        }
    }

    private UniversalCharacterController FindLocalPlayer()
    {
        UniversalCharacterController[] characters = FindObjectsOfType<UniversalCharacterController>();
        foreach (UniversalCharacterController character in characters)
        {
            if (character.photonView.IsMine && character.IsPlayerControlled)
            {
                return character;
            }
        }
        return null;
    }

    private UniversalCharacterController FindNearestNPC(Transform playerTransform)
    {
        UniversalCharacterController[] characters = FindObjectsOfType<UniversalCharacterController>();
        UniversalCharacterController nearestNPC = null;
        float nearestDistance = float.MaxValue;

        foreach (UniversalCharacterController character in characters)
        {
            if (!character.IsPlayerControlled && character.IsPlayerInRange(playerTransform))
            {
                float distance = Vector3.Distance(playerTransform.position, character.transform.position);
                if (distance < nearestDistance)
                {
                    nearestNPC = character;
                    nearestDistance = distance;
                }
            }
        }

        return nearestNPC;
    }
}

// WE NEED THIS: //             if (Input.GetKeyDown(KeyCode.R))
//                 Karyo_GameCore.Instance.GetLocalPlayerCharacter().GetComponent<Player>().InitiatePlayerDialog();

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using Photon.Pun;

// // this handles all input

// public class InputManager : MonoBehaviour
// {
//     private float timeOfLastUserInput;
//     [SerializeField] private float detectIdlePlayerTime = 60; // time in seconds before the game puts up an idle player window

//     private Karyo_GameCore core;

//     public Vector3 PlayerRelativeMoveDirection
//     {
//         get
//         {
//             var moveVector = Vector3.zero;

//             if (core.uiManager.textEntryActive)
//                 return moveVector;

//             // TODO: Controller joystick support would be nice here...
//             if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) moveVector.z += 1;
//             if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) moveVector.z -= 1;
//             if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveVector.x += 1;
//             if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveVector.x -= 1;

//             return moveVector.normalized;
//         }
//     }


//     public bool PlayerRunModifier
//     {
//         get { return (Input.GetKey(KeyCode.LeftShift) || (Input.GetKey(KeyCode.RightShift))); }
//     }

//     public bool PlayerJumpActivate => !core.uiManager.InMenu && Input.GetKeyDown(KeyCode.Space);
//     public bool PlayerJumpHeld => !core.uiManager.InMenu && Input.GetKey(KeyCode.Space);

//     public bool PlayerPrimaryActivate => !core.uiManager.InMenu && Input.GetMouseButtonDown(0);
//     public bool PlayerPrimaryHeld => !core.uiManager.InMenu && Input.GetMouseButton(0);
//     public bool PlayerSecondaryActivate => !core.uiManager.InMenu && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.F));
//     public bool PlayerSecondaryHeld => !core.uiManager.InMenu && (Input.GetMouseButton(1) || Input.GetKey(KeyCode.F));

//     public bool TimeOfDayActivate_Day => !core.uiManager.InMenu && !core.uiManager.textEntryActive && Input.GetKeyDown(KeyCode.F9);
//     public bool TimeOfDayActivate_Sunset => !core.uiManager.InMenu && !core.uiManager.textEntryActive && Input.GetKeyDown(KeyCode.F10);
//     public bool TimeOfDayActivate_Night => !core.uiManager.InMenu && !core.uiManager.textEntryActive && Input.GetKeyDown(KeyCode.F11);
//     public bool TimeOfDayActivate_Sunrise => !core.uiManager.InMenu && !core.uiManager.textEntryActive && Input.GetKeyDown(KeyCode.F12);


//     public bool TargetAcquisitionUnselectTarget
//     {
//         get { return Input.GetKeyDown(KeyCode.Escape); }
//     }

//     private const float MouseScrollScale = .1f;
//     public bool SelectNextTool
//     {
//         get
//         {
//             if (core.uiManager.InMenu) return false;

//             // Ignore scroll if using tool
//             if (PlayerPrimaryHeld || PlayerSecondaryHeld)
//                 return false;

//             return ScrollDown;
//         }
//     }

//     public bool SelectPrevTool
//     {
//         get
//         {
//             if (core.uiManager.InMenu) return false;

//             // Ignore scroll if using tool
//             if (PlayerPrimaryHeld || PlayerSecondaryHeld)
//                 return false;

//             return ScrollUp;
//         }
//     }

//     public bool ScrollUp => Input.mouseScrollDelta.y > MouseScrollScale;
//     public bool ScrollDown => Input.mouseScrollDelta.y < -MouseScrollScale;

//     public bool ClearTool => !core.uiManager.InMenu && Input.GetKeyDown(KeyCode.BackQuote);

//     public bool SelectTool(int index)
//     {
//         if (core.uiManager.InMenu) return false;

//         switch (index)
//         {
//             case 1: return Input.GetKeyDown(KeyCode.Alpha1);
//             case 2: return Input.GetKeyDown(KeyCode.Alpha2);
//             case 3: return Input.GetKeyDown(KeyCode.Alpha3);
//             case 4: return Input.GetKeyDown(KeyCode.Alpha4);
//             case 5: return Input.GetKeyDown(KeyCode.Alpha5);
//             case 6: return Input.GetKeyDown(KeyCode.Alpha6);
//             case 7: return Input.GetKeyDown(KeyCode.Alpha7);
//             case 8: return Input.GetKeyDown(KeyCode.Alpha8);
//             case 9: return Input.GetKeyDown(KeyCode.Alpha9);
//             case 0: return Input.GetKeyDown(KeyCode.Alpha0);
//             default:
//                 Debug.LogError($"Tried to check for index selection that was out of range: {index} must be 0-9");
//                 return false;
//         }
//     }

//     private void Awake()
//     {
//         core = GameObject.FindGameObjectWithTag("GameCore").GetComponent<Karyo_GameCore>();
//         if (core == null)
//             Debug.LogError(this + " cannot find Game Core.");

//     }

//     void Update()
//     {
//         if ((Input.anyKey) || (Input.GetAxis("Mouse X") != 0) || (Input.GetAxis("Mouse Y") != 0))
//             timeOfLastUserInput = Time.time;

//         if ((Time.time - timeOfLastUserInput) > detectIdlePlayerTime)
//             core.uiManager.OpenIdlePlayerDialogWindow();

//         if (!core.uiManager.textEntryActive)
//         {
//             if (Input.GetKeyDown(KeyCode.Q))
//                 core.uiManager.ToggleResetMenu();

//             if (Input.GetKeyDown(KeyCode.Escape))
//                 core.uiManager.CloseOpenWindows();

//             if (Input.GetKeyDown(KeyCode.T))
//                 core.uiManager.ToggleTreeOfLifeWindow();

//             if (Input.GetKeyDown(KeyCode.O))
//                 core.uiManager.ToggleObjectiveWindow();

//             if (Input.GetKeyDown(KeyCode.R))
//                 Karyo_GameCore.Instance.GetLocalPlayerCharacter().GetComponent<Player>().InitiatePlayerDialog();

//             // note you have to hold shift first and then press 8 to make this work
//             if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
//                 Input.GetKeyDown(KeyCode.Alpha8))
//             {
//                 Karyo_GameCore.Instance.PlayerHasFulfilledObjective();
//             }
//         }
//     }
// }
