using System;
using UnityEngine;

namespace SBS
{
    public enum OutputType
    {
        Separately,
        SpriteSheet
    }

    public enum PackingAlgorithm
    {
        Optimized,
        InOrder
    }

    [Serializable]
    public class OutputProperty
    {
        public OutputType type = OutputType.SpriteSheet;
        public PackingAlgorithm algorithm = PackingAlgorithm.Optimized;
        public int atlasSizeIndex = 4;
        public int spritePadding = 0;
        public bool makeAnimationClip = true;
        public bool allInOneAtlas = false;

        public OutputProperty CloneForBaking(bool isStaticModel)
        {
            OutputProperty clone = new OutputProperty();
            clone.type = type;
            clone.algorithm = algorithm;
            clone.atlasSizeIndex = atlasSizeIndex;
            clone.spritePadding = spritePadding;
            clone.makeAnimationClip = makeAnimationClip;
            clone.allInOneAtlas = false;

            if (type == OutputType.SpriteSheet && isStaticModel)
                clone.allInOneAtlas = allInOneAtlas;

            return clone;
        }
    }
}
