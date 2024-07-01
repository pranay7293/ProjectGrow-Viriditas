using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;


// this is the base Behavior class.  all Behaviors derive from it.

public abstract class Behavior : MonoBehaviour
{

    [SerializeField, OnValueChanged("OnBehaviorEnableChanged")]
    private bool _behaviorEnabled;
    public bool behaviorEnabled
    {
        get { return _behaviorEnabled; }
        set
        {
            if (_behaviorEnabled == value)
                return;
            _behaviorEnabled = value;
            OnBehaviorEnableChanged();
        }
    }

    [SerializeField, OnValueChanged("OnBehaviorPauseChanged")]
    private bool _paused;
    public bool paused
    {
        get { return _paused; }
        set
        {
            if (_paused == value)
                return;
            _paused = value;
            OnBehaviorPauseChanged();
        }
    }

    public string BehaviorName => this.GetType().Name;

    public float strength; // set this when needed before setting behaviorEnabled to true
    public string ODS_text;  // text used by the Organism Data Sheet
    protected AI myAI;
    public bool DEBUG_Verbose;  // when true, the Behavior should Debug Log a bunch of useful info


    private void OnBehaviorEnableChanged()
    {
        if (_behaviorEnabled == true)
            StartBehavior();
        else
            EndBehavior();
    }
    private void OnBehaviorPauseChanged()
    {
        if (_paused == true)
            PauseBehavior();
        else
            UnpauseBehavior();
    }

    protected virtual void Awake()
    {
        myAI = GetComponent<AI>();
        if (myAI == null)
            Debug.LogError("Behavior " + this + " on Entity " + gameObject + " can't find its AI component.  I don't think you can have a Behavior without an AI, since the AI manages Behaviors, right?");
    }

    // member classes should override these two without calling Base.
    protected virtual void StartBehavior()
    {
        Debug.LogWarning("StartBehavior not defined for " + this);
    }
    protected virtual void EndBehavior()
    {
        Debug.LogWarning("EndBehavior not defined for " + this);
    }


    // member classes should override these two without calling Base.
    protected virtual void PauseBehavior()
    {
        Debug.LogWarning("PauseBehavior not defined for " + this);
    }
    protected virtual void UnpauseBehavior()
    {
        Debug.LogWarning("UnpauseBehavior not defined for " + this);
    }

    public override string ToString()
    {
        return $"{BehaviorName} behavior";
    }
}
