using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace PrefabLightMapBaker
{
    public static class Utils
    {
        public static bool Apply( PrefabBaker prefab )
        {
            if( prefab.renderers == null || prefab.renderers.Length == 0) return false;

            int[] lightmapArrayOffsetIndex;

            var sceneLightmaps = LightmapSettings.lightmaps;

            var added_lightmaps = new List<LightmapData>();

            lightmapArrayOffsetIndex = new int[ prefab.texturesColor.Length ];

            for(int i = 0; i < prefab.texturesColor.Length; i++)
            {
                bool found = false;

                for( int j = 0; j < sceneLightmaps.Length; j++ )
                {
                    if( prefab.texturesColor[ i ] == sceneLightmaps[ j ].lightmapColor )
                    {
                        lightmapArrayOffsetIndex[ i ] = j;

                        found = true;
                    }
                }

                if( ! found )
                {
                    lightmapArrayOffsetIndex[ i ] = added_lightmaps.Count + sceneLightmaps.Length;

                    var newLightmapData = new LightmapData();

                    newLightmapData.lightmapColor = GetElement( prefab.texturesColor, i );
                    newLightmapData.lightmapDir = GetElement( prefab.texturesDir, i );
                    newLightmapData.shadowMask = GetElement( prefab.texturesShadow, i );

                    added_lightmaps.Add( newLightmapData );
                }
            }

            bool combined = false;

            if(added_lightmaps.Count > 0) 
            {
                CombineLightmaps( added_lightmaps );

                combined = true;
            }

            UpdateLightmaps( prefab, lightmapArrayOffsetIndex );

            return combined;
        }

        public static T GetElement<T>( T[] array, int index )
        {
            if(array == null) return default;
            if(array.Length < index + 1) return default;
            return array[ index ];
        }

        public static void CombineLightmaps( List<LightmapData> lightmaps )
        {
            var original = LightmapSettings.lightmaps;
            var combined = new LightmapData[ original.Length + lightmaps.Count ];

            original.CopyTo( combined, 0 );

            for( int i = 0; i < lightmaps.Count; i++ )
            {
                var idx = i + original.Length;
                var item = lightmaps[ i ];

                combined[ idx ] = new LightmapData {

                    lightmapColor = item.lightmapColor,
                    lightmapDir = item.lightmapDir,
                    shadowMask = item.shadowMask,
                };
            }

            LightmapSettings.lightmaps = combined;
        }


        public static void UpdateLightmaps( PrefabBaker prefab, int[ ] lightmapOffsetIndex )
        {
            for( var i = 0; i < prefab.renderers.Length; ++i )
            {
                var renderer = prefab.renderers[ i ];
                var lightIndex = prefab.renderersLightmapIndex[ i ];
                var lightScale = prefab.renderersLightmapOffsetScale[ i ];

                renderer.lightmapIndex = lightmapOffsetIndex[ lightIndex ];
                renderer.lightmapScaleOffset = lightScale;

                ReleaseShaders( renderer.sharedMaterials );
            }

            ChangeLightBaking( prefab.lights );
        }

        static void ReleaseShaders( Material[ ] materials )
        {
            foreach(var mat in materials)
            {
                if(mat == null) continue;
                var shader = Shader.Find( mat.shader.name );
                if( shader == null) continue;
                mat.shader = shader;
            }
        }

        static void ChangeLightBaking( LightInfo[] lightsInfo )
        {
            foreach(var info in lightsInfo)
            {
                info.light.bakingOutput = new LightBakingOutput
                {
                    isBaked = true,
                    mixedLightingMode = ( MixedLightingMode ) info.mixedLightingMode,
                    lightmapBakeType = ( LightmapBakeType ) info.lightmapBaketype
                };
            }
        }

        public static bool SceneHasAllLightmaps( Texture2D[] texs )
        {
            if(( texs?.Length ?? 0 ) < 1) return true;

            else if( ( LightmapSettings.lightmaps?.Length ?? 0 ) < 1) return false;

            foreach( var lmd in LightmapSettings.lightmaps )
            {
                bool found = false;
                
                if( texs.Contains( lmd.lightmapColor ) ) found = true;
                
                else if( texs.Contains( lmd.lightmapDir ) ) found = true;
                
                else if( texs.Contains( lmd.shadowMask ) ) found = true;
                
                if( ! found ) return false;
            }

            return true;
        }
    }
}