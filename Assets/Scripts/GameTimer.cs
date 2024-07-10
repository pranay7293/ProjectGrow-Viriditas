using UnityEngine;
using TMPro;
using Photon.Pun;

public class GameTimer : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI timerText;
    private float gameTime = 1800f; // 30 minutes in seconds

    private void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            gameTime -= Time.deltaTime;
            UpdateTimerDisplay();
            SyncTimer();
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(gameTime / 60f);
        int seconds = Mathf.FloorToInt(gameTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void SyncTimer()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            customProperties["GameTime"] = gameTime;
            PhotonNetwork.CurrentRoom.SetCustomProperties(customProperties);
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("GameTime"))
        {
            gameTime = (float)propertiesThatChanged["GameTime"];
            UpdateTimerDisplay();
        }
    }
}