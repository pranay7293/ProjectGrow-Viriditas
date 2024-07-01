using Tools;
using UnityEngine;

// this class is responsible for finding a valid target in front of the player when the player indicates they want to take an action
// which requires such a target
public class TargetAcquisition : MonoBehaviour
{
    public delegate void OnTargetChanged(Entity target, bool active);
    public delegate void OnFocusChanged(Entity newFocus);

    public event OnFocusChanged FocusChanged;

    private Karyo_GameCore core;

    private Entity currentFocus;
    public Entity CurrentFocus => currentFocus;
    public bool HasFocus => currentFocus != null;

    public float FocusDistance => currentFocus == null ? 0 : _currentFocusDistance;
    private float _currentFocusDistance;

    public bool DEBUG_verbose;

    private void Awake()
    {
        core = Karyo_GameCore.Instance;
        if (core == null)
            Debug.LogError(this + " cannot find Game Core.");
    }


    public void UpdateTargeting(Vector3 lookPosition, Vector3 lookVector, Vector3 rootPosition, Tool currentTool)
    {
        var targetDistance = currentTool.MaxTargetAcquisitionCastDistance;

        if (targetDistance <= Mathf.Epsilon)
        {
            if (currentFocus)
            {
                ClearTargeting();
            }
        }
        else if (Physics.Raycast(new Ray(lookPosition, lookVector), out var hitInfo, targetDistance, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            // try to find the entity, which may be on the parent or may be on the object itself
            var entity = hitInfo.collider.gameObject.GetComponentInParent<Entity>();
            if (entity == null)
                entity = hitInfo.collider.gameObject.GetComponent<Entity>();

            if (entity)
            {
                _currentFocusDistance = Vector3.Distance(entity.transform.position, rootPosition);
                var range = currentTool.MaintainTargetDistance(entity);

                if (_currentFocusDistance < range)
                {
                    if (currentFocus != entity)
                    {
                        if (currentFocus != null)
                            IndicateFocus(currentFocus, false);

                        currentFocus = entity;
                        IndicateFocus(currentFocus, true);
                        FocusChanged?.Invoke(currentFocus);
                    }
                    return;
                }
            }

            // If we've made it here, clear focus
            if (currentFocus)
            {
                ClearTargeting();
            }
        }
        else if (currentFocus)
        {
            ClearTargeting();
        }
    }

    public void ClearTargeting()
    {
        if (currentFocus != null)
        {
            IndicateFocus(currentFocus, false);
        }
        currentFocus = null;
        FocusChanged?.Invoke(null);
    }

    // causes the passed-in target to be rendered in a way that shows the player it is a target.
    // this is called with active == true when the target has been acquired and false when the target is no longer a target.
    private void IndicateFocus(Entity target, bool active)
    {
        // TODO: Consider caching this or being managed by entity
        var acquisitionListeners = target.GetComponents<ITargetAcquisitionListener>();

        if (active)
        {
            foreach (var listener in acquisitionListeners)
            {
                listener.OnTargetAcquired();
            }
        }
        else
        {
            foreach (var listener in acquisitionListeners)
            {
                listener.OnTargetLost();
            }
        }
    }
}
