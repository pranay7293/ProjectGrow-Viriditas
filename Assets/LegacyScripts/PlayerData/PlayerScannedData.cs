
using System.Collections.Generic;
using UnityEngine;

namespace PlayerData
{
    public static class PlayerScannedData
    {
        private static readonly Dictionary<OrganismDataSheet, int> OrganismScanLevels = new();
        private static readonly HashSet<Entity> ScannedEntities = new();

        // Called automatically by Unity when the game is initializing
        // Note: We could just initialize statically, but if the editor is setup to not require Domain Reload, that method could cause issues. If that doesn't make any sense, call Adrian!
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Initialize()
        {
            OrganismScanLevels.Clear();
            ScannedEntities.Clear();

            // TODO: Load from file
        }

        public static int GetScanLevel(OrganismDataSheet organism)
        {
            if (organism == null) return -1;

            // If it has been scanned, return the level
            if (OrganismScanLevels.TryGetValue(organism, out var level))
                return level;

            // Otherwise assume not scanned
            return -1;
        }

        public static bool CanScan(OrganismDataSheet organism) => organism != null && GetScanLevel(organism) < organism.maxScanDepth;
        public static bool HasScannedEntity(Entity entity) => ScannedEntities.Contains(entity);

        public static IEnumerable<OrganismDataSheet> ScannedOrganismDataSheets => OrganismScanLevels.Keys; // Scanned at least once

        // TODO - this would be private if not for DEBUG_PreScannedData
        public static int UnlockScan(OrganismDataSheet organism)
        {
            if (organism == null) return -1;

            // Increase current level
            var newLevel = GetScanLevel(organism) + 1;

            OrganismScanLevels[organism] = newLevel; // TODO: Save to file
            return newLevel;
        }

        public static int UnlockScanForEntity(Entity entity)
        {
            if (entity.organismDataSheet == null)
            {
                Debug.LogError($"Tried to unlock scan for entity {entity} that had no organism data sheet!");
                return -1;
            }

            if (HasScannedEntity(entity))
            {
                Debug.LogError($"Tried to unlock scan for entity {entity} / {entity.organismDataSheet.nameScientific} but instance was already scanned!");
                return GetScanLevel(entity.organismDataSheet);
            }

            if (!CanScan(entity.organismDataSheet))
            {
                Debug.LogError($"Tried to unlock scan for entity {entity} / {entity.organismDataSheet.nameScientific} that was fully scanned!");
                return GetScanLevel(entity.organismDataSheet);
            }

            ScannedEntities.Add(entity); // TODO: Save to file. Serializing might be tricky without referencing a scene ID or something else unique...

            return UnlockScan(entity.organismDataSheet);
        }

        // TODO: Save and load to file for sessions
    }
}
