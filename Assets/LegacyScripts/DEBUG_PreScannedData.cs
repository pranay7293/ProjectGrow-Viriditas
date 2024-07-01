using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerData;
using System;

// you can use this component to mark organisms and entities as scanned and/or sampled at the beginning of runtime
// to emulate the game having been played previously.

public class DEBUG_PreScannedData : MonoBehaviour
{
    [Serializable]
    public class ODS_PreScan
    {
        public OrganismDataSheet organismDataSheet;
        public bool fullyScanned; 
        public bool alsoSample;   // this is just a shortcut so you don't have to add this organism to the presamlpedOrganisms list
        public int timesToScan;  // if not fully scanned
    }

    public ODS_PreScan[] prescannedODSs;  // these are organism types, ie - species
    public Entity[] prescannedEntities;  // these are instances of organisms, ie - individuals

    public OrganismDataSheet[] presampledOrganisms;  // these organism types have been sampled, ie - their genome is sequenced

    private void Start()
    {

        // scan entities first, because they won't actually scan if the organism type is already fully scanned
        foreach (Entity entity in prescannedEntities)
            PlayerScannedData.UnlockScanForEntity(entity);

        // now unlock scan depths for organisms
        foreach (ODS_PreScan preScan in prescannedODSs)
        {
            if (preScan.organismDataSheet != null)
            {
                if (preScan.fullyScanned)
                    preScan.timesToScan = preScan.organismDataSheet.maxScanDepth + 1;

                int current_scan_amount = PlayerScannedData.GetScanLevel(preScan.organismDataSheet) +1;

                int actual_num_times_to_scan = preScan.timesToScan - current_scan_amount;

                for (int i = 0; i < actual_num_times_to_scan; i++)
                    if (PlayerScannedData.CanScan(preScan.organismDataSheet))
                        PlayerScannedData.UnlockScan(preScan.organismDataSheet);

                if (preScan.alsoSample)
                    PlayerSampledData.SampleOrganism(preScan.organismDataSheet);
            }
        }

        // now sample organisms in the presampledOrganisms list
        foreach (OrganismDataSheet ods in presampledOrganisms)
            PlayerSampledData.SampleOrganism(ods);
    }


}
