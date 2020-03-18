#if UNITY_EDITOR

using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

namespace PrefabLightMapBaker
{
    class Window : EditorWindow
    {
        #region Editor Window

        public static Window instance;

        [MenuItem("Window/Prefab Baker")]
        public static void OpenWindow()
        {
            if(instance != null) return;

            var window = GetWindow<Window>( "Prefab Baker" );
            window.minSize = new Vector2( 255, 300 );
            instance = window;
        }

        #endregion
        // ----------------------------

        #region Window Vars 

        readonly int label_width = 115;

        readonly string README = 
            "Baking multiple prefabs will result them in sharing the same lightmap textures"
            + "\n. All light sources will be taken into account when baking the prefabs"
            + "\n. Using quick bake will override scene lightmap settings"
            + "\n. Note: this process assumes that all meshs you want to apply a light map to has proper UV's for lightmapping";

        Vector2 scroll;
        
        void RefreshVars()
        {
            textureIndex = lightmapMaxSizeValues.ToList().IndexOf(LightmapEditorSettings.maxAtlasSize);
        }

        #endregion
        // ----------------------------

        #region Static Getters
        public static int TextureSize => lightmapMaxSizeValues[textureIndex];

        #endregion
        // ----------------------------

        #region Editor Events

        private void OnDestroy()
        {
            instance = null;
        }

        private void OnDisable( )
        {
            instance = null;
        }

        private void OnFocus()
        {
            RefreshVars();
        }

        private void OnEnable() 
        {
            RefreshVars();

            OnValidate();
        }

        private void OnValidate()
        {
            if ( ! FolderValidate() ) return;
        }

        private void OnInspectorUpdate()
        {
            if(instance == null) instance = this;

            PrefabBakerUpdate();
        }

        private void OnGUI()
        {
            using( var scope = new GUILayout.ScrollViewScope( scroll ))
            {
                scroll = scope.scrollPosition;

                                                            GUILayout.Space( 10 );
                HeaderGUI( );
                                                            GUILayout.Space( 10 );
                if( ! FolderGUI( ) ) return;
                                                            GUILayout.Space( 5 );
                TextureGUI( );
                                                            GUILayout.Space( 5 );
                if( ! PrefabBakerGUI( ) ) return;
                                                            GUILayout.Space( 5 );
                EditorUtils.BoxGUI( ( ) => {

                    BakeSettingsGUI( );

                                                            GUILayout.Space( 5 );

                    var h = GUILayout.Height( 30 );
                    
                    if( GUILayout.Button( "Bake", h ) ) 
                        
                        Baker.Start( );
                } );

                                                            GUILayout.Space( 5 );
                SceneLightmapsGUI( );
            }
        }

        private void HeaderGUI( )
        {
            using(new GUILayout.HorizontalScope( ))
            {
                GUILayout.Label( "Prefab Lightmap Baker", EditorStyles.boldLabel );

                GUILayout.FlexibleSpace( );

                if( GUILayout.Button( " ? ", EditorStyles.miniButton ) )

                    EditorUtility.DisplayDialog( "Info", README, "Close" );
            }
        }

        #endregion
        // ----------------------------

        #region Scene Lightmaps 

        bool sceneLightmaps = false;

        public static Texture2D DrawTexturePing( Texture2D tx, int size = 60, int txPadding = 6, bool preview = false, int index = -1 )
        {
            if(tx == null) return null;

            var content = new GUIContent("name: ", tx.name + "\ntexel: " + tx.texelSize + "\nsize: " + tx.width  + "\naniso: " + tx.anisoLevel );

            if(GUILayout.Button( content, GUILayout.Width( size ), GUILayout.Height( size ) ))
            {
                if( preview ) EditorUtils.CreateLightmapPreviewWindow( index );
                
                else EditorGUIUtility.PingObject( tx );
            }

            Rect rect = GUILayoutUtility.GetLastRect();

            rect.x += txPadding; rect.y += txPadding;
            rect.width -= txPadding * 2; rect.height -= txPadding * 2;
            EditorGUI.DrawPreviewTexture( rect , tx );

            return tx;
        }

        void SceneLightmapsGUI()
        {
            if(( LightmapSettings.lightmaps?.Length ?? 0 ) < 1) return;

            GUILayout.Space( 5 );

            sceneLightmaps = EditorGUILayout.Foldout( sceneLightmaps, "Preview Scene Lightmaps", true );

            if( ! sceneLightmaps) return;

            for( var i = 0; i < LightmapSettings.lightmaps.Length; ++i )
            {
                var lm = LightmapSettings.lightmaps[ i ];

                List<Texture2D> texs = new List<Texture2D>();

                using(new GUILayout.HorizontalScope( ))
                {
                    texs.Add( DrawTexturePing( lm.lightmapColor, preview : true, index : i ) );     GUILayout.Space( 5 );
                    texs.Add( DrawTexturePing( lm.lightmapDir, preview: true, index: i ) );         GUILayout.Space( 5 );
                    texs.Add( DrawTexturePing( lm.shadowMask, preview: true, index: i ) );          GUILayout.Space( 5 );
                }

                texs = texs.Where( x => x != null ).ToList( );

                var info = texs.Select( x => 
                {
                    var path = Path.GetFileNameWithoutExtension( AssetDatabase.GetAssetPath( x ) );

                    return $"{path}  .  {x.width}x{x.height}  mip={ x.desiredMipmapLevel }  frmt={x.format.ToString()}";
                } );

                EditorGUILayout.HelpBox( string.Join( "\n", info ), MessageType.None );

                GUILayout.Space( 5 );
            }
        }

        #endregion
        // ----------------------------

        #region Bake Settings

        public static bool AutoClean = true;

        GUIContent autoCleanLabel = new GUIContent("Auto clean scene lightmaps (?)",
            "Clear scene generated lighting data assets & clear disk catche"
        );

        public static bool QuickBake = false;

        public enum LightmapBake
        {
            Baked = LightmapBakeType.Baked,
            Mixed = LightmapBakeType.Mixed
        }

        static LightmapBake lightCasting = LightmapBake.Baked;

        public static LightmapBakeType LightBakeType => ( LightmapBakeType ) lightCasting;

        void BakeSettingsGUI()
        {
            QuickBake = EditorGUILayout.ToggleLeft( "Quick bake", QuickBake );

            GUILayout.Space( 5 );
            
            AutoClean = EditorGUILayout.ToggleLeft( autoCleanLabel, AutoClean );

            GUILayout.Space( 5 );

            lightCasting = ( LightmapBake ) EditorGUILayout.EnumPopup( "Lights", lightCasting );
        }

        #endregion
        // ----------------------------

        #region PrefabBaker stats

        int prefab_baker_count = 0;
        int prefab_baker_lights_count = 0;
        bool prefab_baker_missing_lights = false;

        bool PrefabBakerGUI()
        {
            if( prefab_baker_count < 1 )
            {
                var s = "No active prefabs found";
                EditorGUILayout.HelpBox( s, MessageType.Error );
                return false;
            }
            else if( prefab_baker_missing_lights )
            {
                var s = "Note: one of the active prefabs has no lights";
                EditorGUILayout.HelpBox( s, MessageType.Warning );
                GUILayout.Space( 5 );
            }

            var info = $"{ prefab_baker_count } active prefabs will be baked";
            info += $" ( total {prefab_baker_lights_count} lights )";
            EditorGUILayout.HelpBox( info, MessageType.None );

            return true;
        }

        void PrefabBakerUpdate() 
        {
            prefab_baker_count = 0;
            prefab_baker_lights_count = 0;
            prefab_baker_missing_lights = false;

            var scene = SceneManager.GetActiveScene();

            foreach (var go in scene.GetRootGameObjects())
            {
                if( ! go.activeSelf ) continue;

                var baker = go.GetComponent<PrefabBaker>();

                if ( baker == null ) continue;

                prefab_baker_count++;

                var light_count = go.GetComponentsInChildren<Light>().Length;

                if( light_count < 1 ) prefab_baker_missing_lights = true;

                prefab_baker_lights_count += light_count;
                
            }
        }

        #endregion
        // ----------------------------

        #region Texture Atlas Size

        public static int textureIndex = 0;
        //  UnityCsReference/Editor/Mono/Inspector/LightingSettingsEditor.cs 
        public static readonly int[] lightmapMaxSizeValues = { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
        public static readonly GUIContent[] lightmapMaxSizeStrings = System.Array.ConvertAll(
            lightmapMaxSizeValues, x => new GUIContent(x.ToString() + "x" + x.ToString() ) );

        void TextureGUI()
        {
            using(new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Texture Size", GUILayout.Width(label_width));

                GUILayout.Space(5);

                var index = EditorGUILayout.Popup(textureIndex, lightmapMaxSizeStrings);

                if (index != textureIndex)
                {
                    textureIndex = index;
                    LightmapEditorSettings.maxAtlasSize = TextureSize;
                }
            }
        }

        #endregion
        // ----------------------------

        #region Lightmaps Folder

        public static string folder = "Assets/Resources/Lightmaps";

        string preview_folder = "";
        bool folder_is_valid = true;

        bool FolderValidate()
        {
            var last_folder_char = folder[folder.Length - 1];

            if (last_folder_char == '/' || last_folder_char == '\\')
            {
                folder = folder.Substring(0, folder.Length - 1);
            }

            preview_folder = folder;

            if ( preview_folder.Contains(Application.dataPath ) )
            {
                // remove full path and display only relative path 
                preview_folder = preview_folder.Substring(Application.dataPath.Length - "Assets".Length);
            }

            folder_is_valid = AssetDatabase.IsValidFolder(preview_folder);

            return folder_is_valid;
        }

        bool FolderGUI()
        {
            GUILayout.Space( 5 );

            using(new GUILayout.HorizontalScope( ))
            {
                GUILayout.Label( "Lightmaps folder", GUILayout.Width( label_width ) );

                GUILayout.Space( 5 );

                if( GUILayout.Button( "Change", EditorStyles.miniButton ) )
                {
                    string newFolderPath = EditorUtility.OpenFolderPanel("Select lightmaps folder", folder, "");

                    if(!string.IsNullOrEmpty( newFolderPath ))
                    {
                        folder = newFolderPath;

                        OnValidate( );
                    }
                }
            }

            EditorGUILayout.HelpBox( preview_folder, MessageType.None );
            
            GUILayout.Space( 10 );

            if (!folder_is_valid)
            {
                GUILayout.Space(5);

                EditorGUILayout.HelpBox("Invalid path\nMake sure folder exists", MessageType.Error);
            }

            return folder_is_valid;
        }

        #endregion
        // ----------------------------
    }
}

#endif