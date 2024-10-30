using UnityEngine;

[CreateAssetMenu(fileName = "APIKeyConfig", menuName = "Config/API Key Config")]
public class APIKeyConfig : ScriptableObject
{
    [SerializeField] private string openAIKey;
    public string OpenAIKey => openAIKey;
}