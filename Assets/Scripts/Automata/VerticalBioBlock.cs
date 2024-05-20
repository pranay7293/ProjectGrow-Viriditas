using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Not a real automata, a very simple bio block which grows upwards.
[RequireComponent(typeof(Rigidbody))]
public class VerticalBioBlock : MonoBehaviour
{
    [SerializeField] private int numBlocks = 6;
    [SerializeField] private GameObject baseBlock;
    [SerializeField] private Vector3 blockScale = Vector3.one;
    [SerializeField] private float blockGrowTime = 1f;
    [SerializeField] private float blockTeardownTime = 0.25f;
    [SerializeField] private float timeBeforeGrowth = 1.5f;
    [SerializeField] private float timeBetweenBlocks = 1f;
    [SerializeField] private float lifetimeAfterComplete = 5f;

    private Rigidbody rb;
    private bool isGrowing = false;
    private List<GameObject> blocks = new List<GameObject>();
    private int originalLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Ignore player collision until we start growing.
        originalLayer = gameObject.layer;
        GameObjectUtil.SetLayerRecursively(gameObject, LayerMask.NameToLayer("IgnorePlayer"));
    }

    private void Update()
    {
        // Wait for the initial rigidbody to settle before growing.
        if (!isGrowing && rb.IsSleeping())
        {
            StartCoroutine(GrowthRoutine());
        }
    }

    // Co-routine which manages the entire growth cycle.
    private IEnumerator GrowthRoutine()
    {
        isGrowing = true;

        // Stabilize the rigidbody and orient to world up.
        // TODO: Tween this rotation as well.
        rb.isKinematic = true;
        transform.rotation = Quaternion.Euler(0, transform.rotation.y, transform.rotation.z);
        yield return new WaitForSeconds(timeBeforeGrowth);

        // Grow the base block on the XZ axis only, re-enable player collision.
        blocks.Add(baseBlock);
        GameObjectUtil.SetLayerRecursively(gameObject, originalLayer);
        yield return TweenLocalScale(baseBlock.transform, baseBlock.transform.localScale, Vector3.Scale(blockScale, new Vector3(1, baseBlock.transform.localScale.y, 1)), blockGrowTime);

        // Grow the base block normally.
        yield return new WaitForSeconds(timeBetweenBlocks);
        yield return TweenLocalScale(baseBlock.transform, baseBlock.transform.localScale, blockScale, blockGrowTime);

        // Grow the subsequent blocks.
        var previousBlock = baseBlock;
        for (int i = 0; i < numBlocks - 1; i++)
        {
            yield return new WaitForSeconds(timeBetweenBlocks);

            var block = Instantiate(baseBlock, transform);
            blocks.Add(block);
            block.transform.position = previousBlock.transform.position + Vector3.up * previousBlock.transform.localScale.y;
            var startScale = Vector3.Scale(blockScale, new Vector3(1, 0, 1));
            yield return TweenLocalScale(block.transform, startScale, blockScale, blockGrowTime);

            previousBlock = block;
        }

        // Death routine.
        yield return new WaitForSeconds(lifetimeAfterComplete);
        blocks.Reverse();
        foreach (var block in blocks)
        {
            block.transform.parent = null;
            var endScale = Vector3.Scale(blockScale, new Vector3(1, 0, 1));
            yield return TweenLocalScale(block.transform, block.transform.localScale, endScale, blockTeardownTime);
            Destroy(block);
        }
        Destroy(gameObject);
    }

    // TODO: This should be a more general shared utility, or we should consider
    // using a library such as DOTween.
    private IEnumerator TweenLocalScale(Transform t, Vector3 from, Vector3 to, float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            float theta = elapsed / time;
            t.localScale = Vector3.Lerp(from, to, theta);
            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        t.localScale = to;
    }
}
