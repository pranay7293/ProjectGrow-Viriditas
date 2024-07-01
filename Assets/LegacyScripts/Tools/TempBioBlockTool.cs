using UI;
using UnityEngine;

namespace Tools
{
    public class TempBioBlockTool : Tool
    {
        [SerializeField] private Transform tossPoint;
        [SerializeField] private GameObject bioBlockPrefab;
        [SerializeField] private float tossForce = 10f;
        [SerializeField] private float spinForce = 10f;

        public override string ToolName => "Bio Block Tool";
        public override ReticleType ReticleType => ReticleType.None;

        public override float MaxTargetAcquisitionCastDistance => 0; // Don't acquire targets
        public override float MaintainTargetDistance(Entity entity) => 0;

        public override void EnablePrimary(TargetAcquisition targetAcquisition)
        {
            base.EnablePrimary(targetAcquisition);

            // Toss the block.
            var obj = Instantiate(bioBlockPrefab, tossPoint.position, Quaternion.identity);
            var rb = obj.GetComponent<Rigidbody>();
            var dir = Vector3.Normalize((transform.forward + Vector3.up) / 2);
            rb.AddForce(dir * tossForce);
            rb.AddTorque(transform.right * spinForce);

            ToolHandler.ClearTool();
        }
    }
}
