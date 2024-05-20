using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlayerData
{
    public static class PlayerSampledData
    {
        private static readonly HashSet<OrganismDataSheet> SampledOrganisms = new();

        // Called automatically by Unity when the game is initializing
        // Note: We could just initialize statically, but if the editor is setup to not require Domain Reload, that method could cause issues. If that doesn't make any sense, call Adrian!
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Initialize()
        {
            SampledOrganisms.Clear();

            // TODO: Load from file
        }

        public static IEnumerable<OrganismDataSheet> SampledDataSheets => SampledOrganisms.AsEnumerable();

        public static bool HasSampled(OrganismDataSheet dataSheet) => SampledOrganisms.Contains(dataSheet);

        public static void SampleOrganism(OrganismDataSheet dataSheet)
        {
            SampledOrganisms.Add(dataSheet);

            // TODO: Save to file
        }
    }
}
