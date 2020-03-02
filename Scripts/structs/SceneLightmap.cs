using UnityEngine;

namespace PrefabLightMapBaker
{
    public struct SceneLightmap
    {
        public int lightMapIndex;
        public Texture2D texColor;
        public Texture2D texDir;
        public Texture2D texShadow;
    }
}
