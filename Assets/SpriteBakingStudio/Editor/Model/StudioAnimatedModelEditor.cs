using UnityEngine;
using UnityEditor;

namespace SBS
{
    public class StudioAnimatedModelEditor : StudioModelEditor
    {
        protected void DrawAnimationFields(StudioAnimatedModel model)
        {
            model.animClip = (AnimationClip)EditorGUILayout.ObjectField("Animation", model.animClip, typeof(AnimationClip), true);
            if (model.animClip == null)
                EditorGUILayout.HelpBox("No animation clip!", MessageType.Warning);
        }

        protected void DrawCustomizerFields(StudioAnimatedModel model)
        {
            model.customizer = (FrameUpdater)EditorGUILayout.ObjectField("Customizer", model.customizer, typeof(FrameUpdater), true);
        }
    }
}
