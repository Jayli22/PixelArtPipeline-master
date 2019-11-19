using System;
using UnityEngine;

namespace SBS
{
    [Serializable]
    public class StudioSetting
    {
        public StudioModel model;

        [SerializeField]
        public LightProperty light = new LightProperty();

        [SerializeField]
        public ViewProperty view = new ViewProperty();

        [SerializeField]
        public ShadowProperty shadow = new ShadowProperty();

        public ExtractorBase extractor;

        [SerializeField]
        public VariationProperty variation = new VariationProperty();

        [SerializeField]
        public PreviewProperty preview = new PreviewProperty();

        public Vector2 textureResolution = new Vector2(500.0f, 400.0f);
        public int simulatedFrame = 0;
        public int frameSize = 10;
        public double delay = 0;

        public int frameSamples = 20; // for animation clip
        public int spriteInterval = 1; // for animation clip

        [SerializeField]
        public TrimProperty trim = new TrimProperty();

        [SerializeField]
        public OutputProperty output = new OutputProperty();

        public bool autoFileNaming = true;
        public string fileName;
        public string outputPath;

        public bool IsStaticModel()
        {
            return model is StudioStaticModel || model is StaticModelGroup;
        }

        public bool IsSingleStaticModel()
        {
            return model is StudioStaticModel;
        }

        public bool IsModelGroup()
        {
            return model is StaticModelGroup;
        }

        public StaticModelGroup GetModelGroup()
        {
            return model as StaticModelGroup;
        }

        public bool IsTopView()
        {
            return view.slopeAngle == 90f;
        }

        public bool IsSideView()
        {
            return view.slopeAngle == 0f;
        }

        public bool IsDynamicRealShadow()
        {
            return shadow.type == ShadowType.Real && shadow.method == RealShadowMethod.Dynamic;
        }

        public bool IsStaticRealShadow()
        {
            return shadow.type == ShadowType.Real && shadow.method == RealShadowMethod.Static;
        }
    }
}
