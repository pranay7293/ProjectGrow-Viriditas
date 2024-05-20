using Systems.Toxin;
using UnityEngine;

// Production behavior allows an entity to produce an environmental effect
// such as scent or toxins.
// TODO: Handle move scent production to this behavior as well.
public class B_Production : Behavior
{
    public IToxinEmitter ToxinToProduce { get; set; }

    // The range at which we produce things in reaction to the player.
    [SerializeField] private float detectionRange = 10f;

    private ToxinVisual visual;
    private Transform player;
    private bool isProducingToxin;

    protected override void Awake()
    {
        base.Awake();
        visual = GetComponentInChildren<ToxinVisual>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    protected override void StartBehavior() { }
    protected override void EndBehavior() { }

    private void Update()
    {
        bool shouldProduceToxin;
        if (paused || !behaviorEnabled)
        {
            shouldProduceToxin = false;
        }
        else
        {
            shouldProduceToxin = ShouldProduceToxin();
        }

        // Update toxin production state.
        if (isProducingToxin != shouldProduceToxin)
        {
            if (DEBUG_Verbose)
                Debug.Log($"{BehaviorName} {(shouldProduceToxin ? "started" : "stopped")} producing toxin because player was near.");
            visual.UpdateVisual(ToxinToProduce, shouldProduceToxin);
            if (shouldProduceToxin)
            {
                ToxinSystem.Register(ToxinToProduce);
            }
            else
            {
                ToxinSystem.Unregister(ToxinToProduce);
            }
        }

        isProducingToxin = shouldProduceToxin;
    }

    private bool ShouldProduceToxin()
    {
        if (ToxinToProduce == null)
        {
            return false;
        }

        // Check if the player is near.
        var range = Mathf.Max(detectionRange, ToxinToProduce.Radius);
        if (Vector3.Distance(player.position, ToxinToProduce.Position) <= range)
        {
            return true;
        }

        // Check if any fauna is near.
        return Physics.CheckSphere(transform.position, detectionRange, Karyo_GameCore.Instance.sceneConfiguration.FaunaLayerMask);
    }
}
