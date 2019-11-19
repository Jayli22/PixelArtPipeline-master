using UnityEngine;

namespace SBS
{
    public abstract class StudioAnimatedModel : StudioModel
    {
        public AnimationClip animClip;
        public FrameUpdater customizer;

#if UNITY_2018 || UNITY_2018_1_OR_NEWER
        public float currDegree = 0.0f;
#endif

        public override float GetTimeForRatio(float ratio)
        {
            if (animClip == null)
                return 0f;
            return animClip.length * ratio;
        }

        public override void UpdateModel(Frame frame)
        {
            if (animClip != null)
                animClip.SampleAnimation(gameObject, frame.time);

            if (customizer != null)
                customizer.UpdateFrame(frame.number, frame.time);

#if UNITY_2018 || UNITY_2018_1_OR_NEWER
            TransformUtils.RotateModel(this, currDegree);
#endif
        }
    }
}
