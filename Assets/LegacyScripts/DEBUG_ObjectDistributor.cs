using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DEBUG_ObjectDistributor : MonoBehaviour
{
    public GameObject[] distrbuteMePrefabs;
    public string nameOfChildWithCollider;
    public int numToDistribute;
    public GameObject parentFolder;

    public Vector3 distRangeBottomLeft;   // most likely make the Y high up in the air for both
    public Vector3 distRangeTopRight;

    public Vector3 scaleMin;
    public Vector3 scaleMax;
    public bool randomRotateAllAxes;  // otherwise its just y axis
    public float sinkYAmount;  // after snapping to physics, lower the object by this much

    private bool alreadyDid = false;
    private List<GameObject> distributedObjects;


    private void Awake()
    {
        distributedObjects = new List<GameObject>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (alreadyDid)
                ClearDistributedOjbects();

            Distribute();
        }
    }

    private void Distribute()
    {
        GameObject newGO;
        Vector3 newPos, oldRot, newRot, newScale;

        for (int i = 0; i < numToDistribute; i++)
        {
            int prefab = Random.Range(0, distrbuteMePrefabs.Length);
            newGO = GameObject.Instantiate(distrbuteMePrefabs[prefab]);

            oldRot = newGO.transform.rotation.eulerAngles;

            newPos = new Vector3(Random.Range(distRangeBottomLeft.x, distRangeTopRight.x), Random.Range(distRangeBottomLeft.y, distRangeTopRight.y), Random.Range(distRangeBottomLeft.z, distRangeTopRight.z));

            newScale = new Vector3(Random.Range(scaleMin.x, scaleMax.x), Random.Range(scaleMin.y, scaleMax.y), Random.Range(scaleMin.z, scaleMax.z));

            if (randomRotateAllAxes)
                newRot = new Vector3(Random.Range(0f, 360f), Random.Range(0f, 360f), Random.Range(0f, 360f));
            else
                newRot = new Vector3(oldRot.x, Random.Range(0f, 360f), oldRot.z);

            newGO.transform.position = newPos;
            newGO.transform.rotation = Quaternion.Euler(newRot);
            newGO.transform.localScale = newScale;

            newGO.transform.parent = parentFolder.transform;

            SnapToTerrain(newGO, nameOfChildWithCollider);
            newGO.transform.position = new Vector3(newGO.transform.position.x, newGO.transform.position.y - sinkYAmount, newGO.transform.position.z);

            distributedObjects.Add(newGO);

            alreadyDid = true;
        }
    }


    private void ClearDistributedOjbects()
    {
        foreach (GameObject go in distributedObjects)
            GameObject.Destroy(go);
    }

    // this should be on a WorldRep class
    private void SnapToTerrain(GameObject go, string childName)
    {
        Transform child;

        if (childName == "")
        {
            child = go.transform;
        }
        else
        {
            child = go.transform.Find(childName);
            if (child == null)
                Debug.LogWarning("Can't find child of GameObject " + go + " with name " + childName);
        }
        Collider collider = child.GetComponent<Collider>();
        if (collider == null)
            Debug.LogWarning("No collider found on GameObject " + child.gameObject);


        RaycastHit hit;
        if (Physics.Raycast(go.transform.position, Vector3.down, out hit, 500f))  // 500f = max distance
        {
            // Get the terrain height at the hit point
            float terrainHeight = hit.point.y;

            if (collider != null)
            {
                float objectHeight = collider.bounds.extents.y;

                // Calculate the new position to snap the object to the terrain
                Vector3 newPosition = go.transform.position;
                newPosition.y = terrainHeight + objectHeight;

                // Set the object's position to the new snapped position
                go.transform.position = newPosition;
            }
        }
        else
            Debug.LogWarning("No terrain found below GameObject " + go + " at position " + go.transform.position.ToString());
    }


    // TODO - this should be in a worldrep class
    public static void SnapToTerrain(Collider col)
    {
        if (col == null)
        {
            Debug.LogWarning("SnapToTerrain called with null collider");
            return;
        }

        // Create a LayerMask for the Terrain layer
        int terrainLayerMask = 1 << LayerMask.NameToLayer("Terrain");

        RaycastHit hit;
        Vector3 upInTheAir = new Vector3(col.transform.position.x, col.transform.position.y + 100f, col.transform.position.z);
        if (Physics.Raycast(upInTheAir, Vector3.down, out hit, 500f, terrainLayerMask))  // 500f = max distance
        {
            // Get the terrain height at the hit point
            float terrainHeight = hit.point.y;

            float offsetGOtoCollider = col.transform.position.y - col.bounds.center.y;  // TODO - is this dependant on how the object is composed?
            float colliderHeight = col.bounds.extents.y;

            // Calculate the new position to snap the object to the terrain
            Vector3 newPosition = col.gameObject.transform.position;
            newPosition.y = terrainHeight + colliderHeight + offsetGOtoCollider;

            // Set the object's position to the new snapped position
            col.gameObject.transform.position = newPosition;
        }
        else
            Debug.LogWarning("No terrain found below GameObject " + col.gameObject + " at position " + col.gameObject.transform.position.ToString());
    }



}
