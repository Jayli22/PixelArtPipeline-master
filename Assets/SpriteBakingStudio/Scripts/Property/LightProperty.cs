using System;
using UnityEngine;

namespace SBS
{
    [Serializable]
    public class LightProperty
    {
        public Light obj;
        public bool followCamera = true;
        public Vector3 pos = Vector3.zero;
    }
}
