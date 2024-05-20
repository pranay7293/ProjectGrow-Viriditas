using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// For now a Module is a wrapper for a Trait which also contains some additional data such as optional Inputs, an optional Logic Operator, 
// and parameters relevant to Circuit Design and Bioreactor gameplay.   
// Trait is synonymous with Output, pretty much, except that Traits have some data like duration which could migrate into Module in the future.
// Long term, Inputs and Logic Operators will have actual functionality that impact the operation of the Trait/Output. For now, though, they are just data.

public class Module : MonoBehaviour
{

    public ModuleInput[] inputs;   // TODO - make these 3 readonly but accesible in the editor
    public ModuleLogicOperator logicalOperator;
    public Trait output;

    public bool IsValid
    {
        get
        {
            if (output == null)
                return false;

            int numInputs = GetNumInputs();
            if ((numInputs < 0) || (numInputs > 2))
                return false;

            if (logicalOperator != null)
                if (numInputs != 2)
                    return false;
            if (numInputs == 2)
                if (logicalOperator == null)
                    return false;

            return true;
        }
    }


    public int GetNumInputs ()
    {
        if (inputs == null)
            return 0;
        return inputs.Length;
    }

    public void AddInput(ModuleInput input)
    {
        if (inputs != null)
            if (inputs.Length >= 2)
            {
                Debug.LogError("AddInput called when there are already 2 inputs - call GetNumInputs() first!");
                return;
            }

        if (inputs == null)
        {
            inputs = new ModuleInput[1];
            inputs[0] = input;
            return;
        }

        ModuleInput[] newArray = new ModuleInput[inputs.Length + 1];
        for (int i = 0; i < inputs.Length; i++)
            newArray[i] = inputs[i];
        newArray[newArray.Length - 1] = input;

        inputs = newArray;
    }

    public void AddOutput(Trait trait)
    {
        output = trait;
    }

    public void AddOperator (ModuleLogicOperator logOp)
    {
        logicalOperator = logOp;
    }

    public float totalCapacityUsed
    {
        get
        {
            float total = 0f;

            if (inputs != null)
                foreach (ModuleInput input in inputs)
                    total += input.capacityDrain;

            if (logicalOperator != null)
                total += logicalOperator.capacityDrain;

            if (output != null)
                total += output.capacityDrain;

            return total;
        }
    }


    public float totalFitnessUsed
    {
        get
        {
            float total = 0f;

            if (inputs != null)
                foreach (ModuleInput input in inputs)
                    total += input.fitnessDrain;

            if (logicalOperator != null)
                total += logicalOperator.fitnessDrain;

            if (output != null)
                total += output.fitnessDrain;

            return total;
        }
    }


    public float totalComplexity
    {
        get
        {
            float total = 0f;

            if (inputs != null)
                foreach (ModuleInput input in inputs)
                    total += input.complexity;

            if (logicalOperator != null)
                total += logicalOperator.complexity;

            if (output != null)
                total += output.complexity;

            return total;
        }
    }

}
