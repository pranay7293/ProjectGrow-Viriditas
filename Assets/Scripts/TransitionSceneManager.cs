using UnityEngine;
using TMPro;
using Photon.Pun;

public class TransitionSceneManager : MonoBehaviourPunCallbacks
{
    public TextMeshProUGUI loadingText;
    public float loadingDelay = 3f;

    private float elapsedTime = 0f;

    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(LoadIGEMDemoScene());
        }
    }

    private System.Collections.IEnumerator LoadIGEMDemoScene()
    {
        while (elapsedTime < loadingDelay)
        {
            elapsedTime += Time.deltaTime;
            loadingText.text = new string('.', (int)(elapsedTime / loadingDelay * 4));
            yield return null;
        }

        PhotonNetwork.LoadLevel("IGEMDemo");
    }
}