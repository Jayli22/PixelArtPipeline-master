using UnityEngine;

namespace SBS
{
    public class StudioRigidModel : StudioAnimatedModel
    {
        public MeshRenderer meshRndr;

        public override Vector3 GetSize()
        {
            return meshRndr != null ? meshRndr.bounds.size : Vector3.one;
        }

        public override Vector3 GetMinPos()
        {
            return meshRndr != null ? meshRndr.bounds.min : Vector3.zero;
        }

        public override Vector3 GetMaxPos()
        {
            return meshRndr != null ? meshRndr.bounds.max : Vector3.zero;
        }

        public override bool IsReady()
        {
            return (animClip != null && meshRndr != null);
        }
    }
}
