using UnityEngine;
using com.ootii.Input;

public class KaryoUnityInputSource : UnityInputSource
{
    public override bool IsViewingActivated => !InputManager.Instance.IsUIActive;

    private void Update()
    {
        // We'll let InputManager handle the cursor state
        // This Update method can be empty or removed entirely
    }
}