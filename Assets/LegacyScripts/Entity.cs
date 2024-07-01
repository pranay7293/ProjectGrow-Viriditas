using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityType
{
    NONE,
    Flora,
    Fauna,
    Humanoid
}

// the Entity class is on any unit with behaviors, such as flora, fauna, and humanoids.
// It is also used to determine the target of the player's attempts to find something in front of it (eg - a creature to edit),
// so  in the future it may also go on buildings, vehicles, etc..

public class Entity : MonoBehaviour
{

    public EntityType entityType;
    public OrganismDataSheet organismDataSheet;  // the organism data sheet this entity type uses

    public bool IsLarge => LayerMask.LayerToName(gameObject.layer) == "LargeFauna";

    private void Update()
    {
#if UNITY_EDITOR
        PerformDebugChecks();
#endif

    }


    // check to see if the object has falled out of the world and similar
    // because of DEBUG_PauseOnWarningOrError.cs these will pause the game so you can examine the problem
#if UNITY_EDITOR
    private static float belowWorldYValue = -500f;

    private void PerformDebugChecks()
    {
        if (transform.position.y < belowWorldYValue)
        {
            Debug.LogWarning("Entity " + gameObject + " is below altitude " + belowWorldYValue + " which means it probably fell through the world somehow. Disabling it.");
            gameObject.SetActive(false);
        }

    }
#endif

}
