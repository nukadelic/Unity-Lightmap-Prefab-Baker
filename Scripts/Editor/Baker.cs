#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

namespace PrefabLightMapBaker
{
    public static class Baker
    {
        static void UpdateLightSettings()
        {
            if(Lightmapping.giWorkflowMode != Lightmapping.GIWorkflowMode.OnDemand)
            {
                Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;

                Debug.LogWarning( "[PrefabBaker] GI work flowMode lightmapping settings set on demand" );
            }

            LightmapEditorSettings.maxAtlasSize = Window.TextureSize;

            if( Window.QuickBake )
            {
                LightmapEditorData.Backup( );
                LightmapEditorData.SetProfileQuickBake( );
            }
        }

        public static void Start()
        {
            Debug.ClearDeveloperConsole();

            UpdateLightSettings( );

            // 1. Prepare objects for bake

            // Fetch PrefabVaker components from current active scene 
            EditorUtils.GetAllPrefabs( ).ForEach( x => {

                // Set all nested object as static for bake
                EditorUtils.LockForBake( x.gameObject );

            } );

            // 2. Display progress dialog and await for bake complete in `BakeCompelte()`
            BakeStart( );

            // 3. Start baking 
            Lightmapping.BakeAsync( );
        }


        static void BakeStart( )
        {
            Lightmapping.bakeCompleted += OnBakeComplete;
            EditorApplication.update += BakeUpdate;
            BakeUpdate( );
        }

        static void BakeUpdate( )
        {
            var p = Lightmapping.buildProgress;

            if( EditorUtility.DisplayCancelableProgressBar( "Boiling Prefabs", "Baking them lights...", p ) )
            {
                Lightmapping.ForceStop( );

                BakeFinished( );
            }
        }

        static void BakeFinished( )
        {
            LightmapEditorData.Restore( );
            Lightmapping.bakeCompleted -= OnBakeComplete;
            EditorApplication.update -= BakeUpdate;
            EditorUtility.ClearProgressBar( );
        }

        static void OnBakeComplete()
        {
            // 4. Clear progress & events 

            BakeFinished( );

            // 5. Fetch lightmaps, apply to prefab and save them in selected folder  

            SaveLightmaps();

            if( Window.AutoClean )
            {
                // 6. Delete scene lightmap data

                Lightmapping.ClearLightingDataAsset( );
                Lightmapping.ClearDiskCache( );
                Lightmapping.Clear( );
            }

            // 7. Combine prefab lightmaps with scene lightmaps 

            EditorUtils.GetAllPrefabs( ).ForEach( x => Utils.Apply( x ) );
        }

        public static List<SceneLightmap> SceneLightmapList;

        static string folder_scene;

        static void SaveLightmaps()
        {
            SceneLightmapList = new List<SceneLightmap>();

            // 5.1 Update path for the generated lightmap assets 

            var dsc = Path.AltDirectorySeparatorChar;
            var scene = SceneManager.GetActiveScene();

            folder_scene = Path.GetDirectoryName(scene.path) + dsc + scene.name + dsc;
            
            // 5.2 Get all active MeshRenderers with valid lightmap index

            EditorUtils.GetAllPrefabs().ForEach( prefab => {

                var renderers = EditorUtils.GetValidRenderers( prefab.gameObject );

                // 5.3 Store scene lightmaps as assets in target folder

                renderers.ForEach( r => GetOrSaveSceneLightmapToAsset( r.lightmapIndex, prefab.gameObject.name ) );

                // 5.4 Reference the cloned light maps to the renderer

                EditorUtils.UpdateLightmaps( prefab, renderers, SceneLightmapList );
                
                // 5.5 Update prefab lights 

                EditorUtils.UpdateLights( prefab );

                // 5.6 Save prefab asset in project 

                EditorUtils.UpdatePrefab( prefab.gameObject );

            } );
        }

        static SceneLightmap GetOrSaveSceneLightmapToAsset( int lightmap_index, string name )
        {
            // Return existing lightmap if matching texture is already in list 

            var index = GetSceneLightMapIndex( lightmap_index );

            if ( index > -1 ) return SceneLightmapList[ index ];

            // Store scene light map in target folder and register to list 
            
            SceneLightmap slm = SaveSceneLightmap( lightmap_index, name );

            SceneLightmapList.Add( slm );

            return slm;
        }

        public static int GetSceneLightMapIndex( int lightMapIndex )
        {
            for(var i = 0; i < SceneLightmapList.Count; ++i)

                if( SceneLightmapList[ i ].lightMapIndex == lightMapIndex ) return i;

            return -1;
        }

        public static SceneLightmap GetSceneLightmapFromRendererIndex( int rendererIndex )
        {
            int idx = GetSceneLightMapIndex( rendererIndex );

            if( idx < 0 ) throw new System.Exception( "[PrefabBaker] SceneLightmap not found at index " + rendererIndex );

            return SceneLightmapList[ idx ];
        }

        static SceneLightmap SaveSceneLightmap( int lightmap_index, string name )
        {
            int i = lightmap_index;

            SceneLightmap slm = new SceneLightmap { lightMapIndex = lightmap_index };

            // Create folder if it doesn't exist

            Directory.CreateDirectory( Window.folder );

            // Update paths definitions 

            var dsc = Path.AltDirectorySeparatorChar;

            string copyFrom, saveTo,
                filename = folder_scene + "Lightmap-" + i,
                newFile = Window.folder + dsc + name + dsc + name;

            // Save color texture

            if( LightmapSettings.lightmaps[ i ].lightmapColor != null )
            {
                copyFrom = $"{filename}_comp_light.exr";
                saveTo = $"{newFile}_light-{ i }.asset";

                slm.texColor = EditorUtils.SaveLightmapAsset( copyFrom, saveTo );
            }

            // Save directional texture 

            var lightmapDir = LightmapSettings.lightmaps[ i ].lightmapDir;

            if( lightmapDir != null)
            {
                copyFrom = $"{filename}_comp_dir.png";
                saveTo = $"{newFile}_dir-{ i }.asset";

                slm.texDir = lightmapDir = EditorUtils.SaveLightmapAsset( copyFrom, saveTo );
            }

            if( lightmapDir == null ) 
                Debug.LogWarning( $"[PrefabBaker] Direction-lightmap { i } was not saved" );

            // Save shadow texture 

            if( LightmapSettings.lightmaps[ i ].shadowMask != null )
            {
                copyFrom = $"{filename}_comp_shadowmask.png";
                saveTo = $"{newFile}_shadow-{ i }.asset";

                slm.texShadow = EditorUtils.SaveLightmapAsset( copyFrom, saveTo );
            }

            return slm;
        }
    }
}

#endif