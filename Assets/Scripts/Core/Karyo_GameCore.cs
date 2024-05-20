using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// right now this is just the switch board between various components
// UPDATE: it also tracks player objectives

[DefaultExecutionOrder(-1000)] // Execute first so awake is triggered before others
public class Karyo_GameCore : MonoBehaviour
{
    public static Karyo_GameCore Instance { get; private set; }

    [Header("Subcomponent references")]
    public Player player;
    public WorldRep worldRep;
    public InputManager inputManager;
    public UIManager uiManager;
    public KaryoUnityInputSource karyoUnityInputSource;
    public TargetAcquisition targetAcquisition;
    public PersistentData persistentData;
    public SceneConfiguration sceneConfiguration;
    public OpenAIService openAiService;

    public float DEBUG_TimeScale = 1f;

    public NPC_PlayerObjective currentPlayerObjective { get; private set; }
    private List<NPC_PlayerObjective> completedObjectives;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Found multiple instances of Karyo_GameCore - this should not happen!");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (player == null)
            player = GameObject.FindFirstObjectByType<Player>();  // could also get Player.Instance depending on the script order
        if (player == null)
            Debug.LogError("GameCore can't find Player.");

        if (worldRep == null)
            worldRep = GameObject.FindFirstObjectByType<WorldRep>();
        if (worldRep == null)
            Debug.LogError("GameCore can't find WorldRep.");

        if (inputManager == null)
            inputManager = GameObject.FindFirstObjectByType<InputManager>();
        if (inputManager == null)
            Debug.LogError("GameCore can't find InputManager.");

        if (uiManager == null)
            uiManager = GameObject.FindFirstObjectByType<UIManager>();
        if (uiManager == null)
            Debug.LogError("GameCore can't find UIManager.");

        if (karyoUnityInputSource == null)
            karyoUnityInputSource = GameObject.FindFirstObjectByType<KaryoUnityInputSource>();
        if (karyoUnityInputSource == null)
            Debug.LogError("GameCore can't find KaryoUnityInputSource.");

        if (targetAcquisition == null)
            targetAcquisition = GameObject.FindFirstObjectByType<TargetAcquisition>();
        if (targetAcquisition == null)
            Debug.LogError("GameCore can't find targetAcquisition.");

        if (persistentData == null)
            persistentData = GameObject.FindFirstObjectByType<PersistentData>();
        if (persistentData == null)
            Debug.LogError("GameCore can't find persistentData.");

        if (sceneConfiguration == null)
            sceneConfiguration = GameObject.FindFirstObjectByType<SceneConfiguration>();
        if (sceneConfiguration == null)
            Debug.LogError("GameCore can't find sceneConfiguration.");

        if (openAiService == null)
            openAiService = GameObject.FindFirstObjectByType<OpenAIService>();
        if (openAiService == null)
            Debug.LogError("GameCore can't find openAiService.");

        if (DEBUG_TimeScale != 1f)
        {
            Debug.Log($"Setting time scale to {DEBUG_TimeScale}. Set it to 1.0 in GameCore to prevent this.");
            Time.timeScale = DEBUG_TimeScale;
        }

        completedObjectives = new List<NPC_PlayerObjective>();
    }

    private void OnDestroy()
    {
        // This shouldn't be necessary, but still nice to cleanup
        Instance = null;
    }



    public void CreateObjective (NPC_PlayerObjective objective)
    {
        if (DoesPlayerCurrentlyHaveAnObjective())
            Debug.LogWarning($"Player is being given an objective, but they already have an active objective which is {objective} ");

        currentPlayerObjective = objective;

        uiManager.DisplayObjectivePopupNotification(objective);

    }

    public void PlayerHasFulfilledObjective ()
    {
        if (currentPlayerObjective == null)
        {
            Debug.LogWarning($"PlayerHasFulfilledObjective() being called, but player did not have a current objective.");
            return;
        }

        string body_text = new string("");

        body_text = body_text + "Great work! You finished the objective:\n" + currentPlayerObjective.title;

        uiManager.LaunchGenericDialogWindow("Objective Complete", body_text, true);

        completedObjectives.Add(currentPlayerObjective);
        currentPlayerObjective = null;
    }

    public bool DoesPlayerCurrentlyHaveAnObjective ()
    {
        return (currentPlayerObjective != null);
    }

    public bool HasPlayerAlreadyFulfilledThisObjective (NPC_PlayerObjective objective)
    {
        return completedObjectives.Contains(objective);
    }

    public string CompletedObjectivesAsString()
    {
        string toReturn = new string("");

        foreach (NPC_PlayerObjective objective in completedObjectives)
            toReturn = toReturn + objective.title + "\n";

        return toReturn;
    }


    // shuffles the list in place
    public static void Shuffle<T>(List<T> list)
    {
        if (list.Count <= 1)
            return;

        for (int times = 0; times < 2; times++)
        {
            // fisher-yates
            T tmp;
            for (int i = 0; i < list.Count - 1; i++)
            {
                int j = Random.Range(i, list.Count);
                tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }

    // shuffles the array in place
    public static void Shuffle<T>(T[] array)
    {
        if (array.Length <= 1)
            return;

        for (int times = 0; times < 2; times++)
        {
            // fisher-yates
            T tmp;
            for (int i = 0; i < array.Length - 1; i++)
            {
                int j = Random.Range(i, array.Length);
                tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
            }
        }
    }


}
