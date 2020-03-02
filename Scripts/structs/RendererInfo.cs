using System;
using UnityEngine;

namespace PrefabLightMapBaker
{
    [Serializable] public struct RendererInfo
    {
        public Renderer renderer;
        public int lightmapIndex;
        public Vector4 lightmapOffsetScale;
    }
}