// using UnityEngine;
// using TMPro;

// public class TMProWarningSupressor : MonoBehaviour
// {
//     private void Awake()
//     {
//         TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
//     }

//     private void OnDestroy()
//     {
//         TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
//     }

//     private void OnTextChanged(Object obj)
//     {
//         var textMeshPro = obj as TMP_Text;
//         if (textMeshPro != null)
//         {
//             textMeshPro.fontSharedMaterial.enableWarning = false;
//         }
//     }
// }