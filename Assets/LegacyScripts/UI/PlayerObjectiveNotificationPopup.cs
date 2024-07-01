using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerObjectiveNotificationPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title;
    [SerializeField] private TextMeshProUGUI body;

    private static float popupDuration = 15f;
    private bool active;
    private float startTime;

    public void DisplayNotification (NPC_PlayerObjective objective)
    {
        title.text = objective.title;

        string subtasks = new string("");
        foreach (string subtask in objective.subtasks)
            subtasks = subtasks + "* " + subtask + "\n";

        body.text = subtasks;

        active = true;
        startTime = Time.time;
    }

    private void Update()
    {
        if (!active)
            return;

        if ((Time.time - startTime) > popupDuration)
        {
            active = false;
            gameObject.SetActive(false);
        }
        
    }

}
