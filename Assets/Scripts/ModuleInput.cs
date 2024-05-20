using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// an Input used by a Module (see Module)

public class ModuleInput : MonoBehaviour
{
    public new string name;  // TODO - should be readonly but assignable in the inspector
    public float capacityDrain; // should be negative  TODO - make these 3 accessible in the editor but readonly to other classes (by using a backing variable)
    public float fitnessDrain; // should be negative 
    public float complexity; // should be positive

}
