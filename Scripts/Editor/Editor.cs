#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace PrefabLightMapBaker
{
    [CustomEditor( typeof( PrefabBaker ) ) ]
    public class PrefabBakerEditor : Editor
    {
        public bool PreviewData = true;

        public bool EditComponents = false;

        public override void OnInspectorGUI( )
        {
            PrefabBaker instance = ( PrefabBaker ) target;

            GUILayout.Space( 15 );

            if( Window.instance == null )
            {
                if(GUILayout.Button( "Open Baker", GUILayout.Height( 30 ) ))
                
                    Window.OpenWindow( );

                GUILayout.Space( 5 );
            }

            bool hasData = instance.HasBakeData;

            if( hasData )
            {
                using( new GUILayout.HorizontalScope() )
                {
                    EditorGUI.BeginDisabledGroup( instance.BakeApplied );

                    if(GUILayout.Button( "Apply", GUILayout.Height( 25 ) )) 
                        
                        instance.BakeApply( );

                    EditorGUI.EndDisabledGroup( );

                    GUILayout.Space( 5 );

                    GUI.color = new Color( 1f, 0.5f, 0.5f );

                    if(GUILayout.Button( "Clear", GUILayout.Height( 25 ), GUILayout.Width( 70 ) ))
                    
                        EditorUtils.Reset( instance );

                    GUI.color = Color.white;
                }

                GUILayout.Space( 5 );

                GUILayout.Label( "", GUI.skin.horizontalSlider );
            }

            GUILayout.Space( 5 );

            EditorUtils.BoxGUI( ( ) =>
            {
                using(new GUILayout.HorizontalScope( ))
                {
                    GUILayout.Space( 10 );
                    EditComponents = EditorGUILayout.Foldout( EditComponents, " Override Nested Components", true );
                }

                //EditComponents = EditorGUILayout.ToggleLeft( " Override Nested Components", EditComponents );

                if( ! EditComponents ) return;

                GUILayout.Space( 5 );

                PanelComponentsOverride.Draw( );

                GUILayout.Space( 5 );

                if(GUILayout.Button( "Apply" )) 
                    
                    PanelComponentsOverride.Apply( instance.gameObject );
            } );

            GUILayout.Space( 5 );

            if( ! hasData ) return;

            PreviewData = EditorGUILayout.Foldout( PreviewData, "Show data", true );

            GUILayout.Space( 5 );

            if( ! PreviewData ) return ; //base.OnInspectorGUI( );

            var ln = instance.lights?.Length;

            string info = $"{ instance.lights?.Length ?? 0 } Lights | " +
                $" {instance.renderers?.Length ?? 0} Renderers\n" +
                $"{instance.texturesColor?.Length ?? 0} Color Textures";

            if(( instance.texturesDir?.Length ?? 0 ) > 0)
                info += $"\n{ instance.texturesDir?.Length ?? 0} Directional Textures";
            
            if(( instance.texturesShadow?.Length ?? 0 ) > 0)
                info += $"\n{ instance.texturesShadow?.Length ?? 0} Shadow Textures";

            GUILayout.Label( "Info", EditorStyles.boldLabel );
            EditorGUILayout.HelpBox( info, MessageType.None );

            GUILayout.Space( 5 );

            GUILayout.Label( "Textures", EditorStyles.boldLabel );

            DrawTextures( instance.texturesColor, ref scroll_color );
            DrawTextures( instance.texturesDir, ref scroll_dir );
            DrawTextures( instance.texturesShadow, ref scroll_shadow );
        }

        [SerializeField] Vector2 scroll_color;
        [SerializeField] Vector2 scroll_dir;
        [SerializeField] Vector2 scroll_shadow;

        void DrawTextures( Texture2D[] textures, ref Vector2 scroll )
        {
            if(( textures?.Length ?? 0 ) < 1) return;

            var h = GUILayout.Height( scroll.y < 0 ? 70 : 55 );

            using(var scope = new GUILayout.ScrollViewScope( scroll, GUI.skin.textArea, h ) )
            {
                GUILayout.Space( 5 );

                using( new GUILayout.HorizontalScope() )
                {
                    GUILayout.Space( 5 );

                    Rect rect = Rect.zero;

                    foreach( var tex in textures )
                    {
                        Window.DrawTexturePing( tex, 40, 4 );
                        
                        rect = GUILayoutUtility.GetLastRect( );

                        GUILayout.Space( 5 );
                    }
                    
                    if( Event.current.type == EventType.Repaint )

                        // double rect width to support unexpected pixel shifts in DPI scaling in windows 
                        scroll.y = ( rect.width * 2 + rect.x + 10 ) > ( Screen.width / EditorGUIUtility.pixelsPerPoint ) ? - 0.05f : 0;
                }

                GUILayout.Space( 5 );

                scroll.x = scope.scrollPosition.x;
            }

            GUILayout.Space( 5 );
        }
    }
}

#endif