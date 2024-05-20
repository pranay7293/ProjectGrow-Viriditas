using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// an Logical Operator used by a Module (see Module)

public class ModuleLogicOperator : MonoBehaviour
{
    // TODO - make these accessible in the editor but readonly to other classes (by using a backing variable)
    public new string name;
    public Sprite image;
    public float capacityDrain; // should be negative   
    public float fitnessDrain; // should be negative
    public float complexity; // should be positive

    // TODO - when we want these to be functional, probably add the capacity to perform the logical operator for all of them here, 
    // to keep it in one place, and just have an enum dropdown to select which operation this instance performs

}
