﻿using System;
using UnityEngine;

namespace SBS
{
    public enum PreviewBackgroundType
    {
        Checker,
        SingleColor
    }

    [Serializable]
    public class PreviewProperty
    {
        public bool on = false;
        public PreviewBackgroundType backgroundType = PreviewBackgroundType.Checker;
        public Color backgroundColor = Color.black;
    }
}
