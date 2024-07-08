using UnityEngine;
using UnityEngine.AI;

// Helper shared by movement behaviors that need to do NavMesh pathfinding.
// Note - This could be a MonoBehavior but a class was chosen for simplicity,
// it doesn't modify entity state directly.
public class PathfindingHelper
{
    public bool HasPath { get; private set; }

    private NavMeshPath path;
    private int nextPathPoint;

    public PathfindingHelper()
    {
        path = new NavMeshPath();
    }

    // Calculate path to given destination, returns whether a valid path was
    // found or not.
    public bool CalculatePath(Vector3 entityPosition, Vector3 destination)
    {
        // TODO: Bind to a specific NavMeshArea...
        HasPath = NavMesh.CalculatePath(entityPosition, destination, NavMesh.AllAreas, path);

        if (HasPath)
        {
            // 0 will always be the starting point - ignore that one.
            nextPathPoint = 1;
        }

        return HasPath;
    }

    // Gets the next world space destination the entity should go to, taking
    // into account the entity's current position.
    // If there is no valid path or the path is complete, returns null.
    public Vector3? GetNextDestination(Vector3 entityPosition, float epsilonDistanceToNextPoint = 0.25f)
    {
        if (!HasPath || nextPathPoint >= path.corners.Length)
        {
            return null;
        }


        // TODO: Consider using NavMeshAgent to do all this and have obstacle avoidance
        var destination = path.corners[nextPathPoint];

        // If close enough to next point, increment and point to the next one
        var diff = destination - entityPosition;
        diff.y = 0;
        if (diff.magnitude < epsilonDistanceToNextPoint)
        {
            nextPathPoint++;
            if (nextPathPoint < path.corners.Length)
                destination = path.corners[nextPathPoint];
        }

        return destination;
    }

    public void DrawDebugGizmos()
    {
        if (!HasPath)
        {
            return;
        }
        for (var i = 0; i < path.corners.Length - 1; ++i)
        {
            if (i == nextPathPoint - 1)
                Gizmos.color = Color.red; // Current
            else if (i < nextPathPoint)
                Gizmos.color = Color.gray; // Already passed
            else
                Gizmos.color = Color.blue; // Future point

            var pointA = path.corners[i];
            var pointB = path.corners[i + 1];

            Gizmos.DrawLine(pointA, pointB);
        }
    }
}
