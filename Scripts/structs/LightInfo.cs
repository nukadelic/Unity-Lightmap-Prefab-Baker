using System;
using UnityEngine;

namespace PrefabLightMapBaker
{
    [Serializable] public struct LightInfo
    {
        public Light light;
        public int lightmapBaketype;
        public int mixedLightingMode;
    }
}
