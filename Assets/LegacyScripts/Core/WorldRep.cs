using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// this is mostly just static methods for doing thigns in 3d space

public class WorldRep : MonoBehaviour
{
    public static float gravity { get; private set; } = -9.8f;


    public static float NormalizeAngleToZeroto360(float angle)
    {
        while (angle < 0)
            angle += 360f;

        while (angle > 360)
            angle -= 360f;

        return angle;
    }

    public static Vector3 TerrainPosition(Vector3 pos)
    {
        return new Vector3(pos.x, TerrainYHeightAtPosition(pos), pos.z);
    }

    public static float TerrainYHeightAtPosition(Vector3 pos)
    {
        // Create a LayerMask for the Terrain layer
        int terrainLayerMask = 1 << LayerMask.NameToLayer("Terrain");

        RaycastHit hit;
        Vector3 upInTheAir = new Vector3(pos.x, pos.y + 500f, pos.z);
        if (Physics.Raycast(upInTheAir, Vector3.down, out hit, 1000f, terrainLayerMask))  // 1000f = max distance
            return hit.point.y;
        else
        {
            Debug.LogWarning(" No terrain found at position " + pos.ToString());
            return 0f;
        }
    }

    // returns the position at which the passed-in collider is resting on the terrain underneath it
    public static Vector3 PlaceColliderOnTerrain(Collider col)
    {
        if (col == null)
        {
            Debug.LogWarning("PlaceColliderOnTerrain called with null collider.");
            return col.transform.position;
        }

        float terrainHeight = TerrainYHeightAtPosition(col.transform.position);

        if (terrainHeight == 0f)
            return col.transform.position;  // this is the best we can do if there's no terrain under the collider

        Vector3 colliderBottom = col.bounds.min;
        Vector3 newPosition = col.transform.position - new Vector3(0f, colliderBottom.y - terrainHeight, 0f);

        return newPosition;
    }


    public static Quaternion RemoveXZRotations(Quaternion rot)
    {
        Vector3 euler = rot.eulerAngles;

        Vector3 stripped = new Vector3(0f, euler.y, 0f);

        return Quaternion.Euler(stripped);
    }


    public static Vector3 GetBottommostPointOfCollider(Collider col)
    {
        Bounds bounds = col.bounds;
        return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    // TODO - this detection could really be improved, especially since if it returns a false negative, it'll block a hopping entity's movement
    public static bool IsGrounded(Collider col)
    {
        float Epsilon_position = 0.75f;

        float groundHeight = TerrainYHeightAtPosition(col.transform.position);
        float bottomOfCollider = GetBottommostPointOfCollider(col).y;

        if (Mathf.Abs(groundHeight - bottomOfCollider) < Epsilon_position)
            return true;

        return false;
    }


    // TODO - it's probably possible to use Types to collapse this into just one method that takes either a list of entities or gos and returns the same class
    public Entity GetEntityClosestToPoint(List<Entity> entities, Vector3 point)
    {
        float minDistance = 99999f;
        Entity toReturn = null;
        foreach (Entity entity in entities)
        {
            float distance = Vector3.Distance(entity.transform.position, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                toReturn = entity;
            }
        }
        return toReturn;
    }
    public GameObject GetGameObjectClosestToPoint(List<GameObject> gameObjects, Vector3 point)
    {
        float minDistance = 99999f;
        GameObject toReturn = null;
        foreach (GameObject go in gameObjects)
        {
            float distance = Vector3.Distance(go.transform.position, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                toReturn = go;
            }
        }
        return toReturn;
    }


    // returns a worldspace position which is the topmost extent of any collider on this object or one of its children (non-recursive)
    public Vector3 GetHighestPoint(GameObject go)
    {
        Vector3 topmostPoint;
        Collider col = go.GetComponent<Collider>();

        if (col != null)
            topmostPoint = col.bounds.max;
        else
            topmostPoint = new Vector3(0f, -99999f, 0f);

        foreach (Transform child in go.transform)
        {
            col = child.GetComponent<Collider>();
            if (col != null)
            {
                Vector3 localTop = col.bounds.max;
                if (localTop.y > topmostPoint.y)
                    topmostPoint = localTop;
            }
        }

        if (Vector3.Equals(topmostPoint, new Vector3(0f, -99999f, 0f)))
            Debug.LogWarning("No colliders found on GameObject " + go.name + " or its children!");

        return topmostPoint;
    }



    // this is currently not used, as far as I know
    // returns the slope of the terrain at positionToTest or 0f if none is found
    private float GetTerrainSlopeAtPoint(Vector3 positionToTest)
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(positionToTest, Vector3.down, out hitInfo))
        {
            // Check if the hit collider is a TerrainCollider
            Terrain terrain = hitInfo.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                Vector3 terrainNormal = hitInfo.normal;
                float slopeAngle = Vector3.Angle(terrainNormal, Vector3.up);
                return slopeAngle;
            }
        }

        return 0f; // No terrain or slope detected
    }

    public static Quaternion SmoothRotateTowards(Quaternion rotation, Vector3 direction, float amount)
    {
        // Calculate the rotation towards the directin.
        var targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        // Smoothly rotate the given rotation towards the destination rotation.
        return Quaternion.Slerp(rotation, targetRotation, amount);
    }
}
