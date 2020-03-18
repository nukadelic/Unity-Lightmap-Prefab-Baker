#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PrefabLightMapBaker
{
    public static class PanelComponentsOverride
    {
        static public LightShadows lightShadows = LightShadows.None;

        static public ShadowCastingMode shadowCastingMode = ShadowCastingMode.Off;

        static public bool receiveShadows = false;

        static public LightmapBakeType lightCasting = LightmapBakeType.Mixed;

        public static void Draw()
        {
            // -- 

            GUILayout.Label( "Lights", EditorStyles.boldLabel );

            lightShadows = ( LightShadows ) EditorGUILayout.EnumPopup( "Shadows", lightShadows );

            lightCasting = ( LightmapBakeType ) EditorGUILayout.EnumPopup( "Casting", lightCasting );

            // -- 

            GUILayout.Space( 5 );

            GUILayout.Label( "Renderers", EditorStyles.boldLabel );

            shadowCastingMode = ( ShadowCastingMode ) EditorGUILayout.EnumPopup( "Cast Shadows", shadowCastingMode );

            receiveShadows = EditorGUILayout.Toggle( "Receive Shadow", receiveShadows );
        }

        public static void Apply( GameObject root )
        {
            root.GetComponentsInChildren<MeshRenderer>( true ).ToList( ).ForEach( renderer =>
            {
                renderer.receiveShadows = receiveShadows;
                renderer.shadowCastingMode = shadowCastingMode;
            } );

            root.GetComponentsInChildren<Light>( true ).ToList( ).ForEach( light =>
            {
                light.shadows = lightShadows;
                light.lightmapBakeType = lightCasting;
            } );
        }
    }
}

#endif