using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.Herding
{
    public interface IHerdAgent
    {
        public int herdGroupId { get; set; }
        public Transform transform { get; }
        public GameObject gameObject { get; }
        public Vector3? herdWanderDestination { get; }
    }

    public class HerdingSystem
    {

        public Dictionary<int, Vector3> HerdGroupDestinations = new Dictionary<int, Vector3>();

        private static float herdGroupingRadius = 20f;
        private int lastHerdGroupId = 0;
        private Dictionary<int, Vector3> herdGroupInitialCenters = new Dictionary<int, Vector3>();
        private Dictionary<int, int> herdGroupCounts = new Dictionary<int, int>();

        // Herd group initialization.
        // TODO: Herd groups should dynamically form over time using the boids
        // behavior that already exists, for now we initialize them at startup.
        public void AssignHerdGroupId(IHerdAgent agent)
        {
            // Find the herd group with the closest center.
            var groups = herdGroupInitialCenters
                .Select(kv => (GroupId: kv.Key, Center: kv.Value, Distance: Vector3.Distance(kv.Value, agent.transform.position)))
                .Where(p => p.Distance <= herdGroupingRadius)
                .OrderBy(p => p.Distance);
            if (groups.Count() < 1)
            {
                // Create a new group.
                agent.herdGroupId = lastHerdGroupId++;
                herdGroupInitialCenters[agent.herdGroupId] = agent.transform.position;
                herdGroupCounts[agent.herdGroupId] = 1;
            }
            else
            {
                // Assign the agent to the group.
                var group = groups.First();
                agent.herdGroupId = group.GroupId;
                herdGroupCounts[group.GroupId]++;

                // Update the existing center.
                herdGroupInitialCenters[group.GroupId] = Vector3.Lerp(group.Center, agent.transform.position, 1 / herdGroupCounts[group.GroupId]);
            }
        }
    }
}
