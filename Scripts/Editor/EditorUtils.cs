#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Directory = System.IO.Directory;

namespace PrefabLightMapBaker
{
    public static class EditorUtils
    { 
        public static void LockForBake( GameObject target )
        {
            var flags = StaticEditorFlags.ContributeGI |
                //StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.OccludeeStatic |
                StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OffMeshLinkGeneration |
                StaticEditorFlags.ReflectionProbeStatic;

            GameObjectUtility.SetStaticEditorFlags(target, flags);

            foreach (var t in target.GetComponentsInChildren<Transform>())
            
                GameObjectUtility.SetStaticEditorFlags(t.gameObject, flags);

            foreach (var light in target.GetComponentsInChildren<Light>())
            {
                light.gameObject.isStatic = false;
                light.lightmapBakeType = Window.LightBakeType;
            }
        }

        public static void DisableLights( GameObject target)
        {
            foreach ( var light in target.GetComponentsInChildren<Light>() )

                light.gameObject.SetActive(false);
        }

        public static List<PrefabBaker> GetAllPrefabs()
        {
            List<PrefabBaker> prefabs = new List<PrefabBaker>();

            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var prefab_component = go.GetComponent<PrefabBaker>();

                if (prefab_component != null) prefabs.Add(prefab_component);
            }

            return prefabs;
        }

        public static List<MeshRenderer> GetValidRenderers( GameObject root )
        {
            return root.GetComponentsInChildren<MeshRenderer>()
                .Where( x => 
                    x.enabled && 
                    x.lightmapIndex > -1 && 
                    x.lightmapScaleOffset != Vector4.zero 
                )
                .ToList();
        }

        public static void UpdateLightmaps( PrefabBaker prefab, List<MeshRenderer> renderers, List<SceneLightmap> lightmaps )
        {
            List<Texture2D>     listColor =         new List<Texture2D>();
            List<Texture2D>     listDir =           new List<Texture2D>();
            List<Texture2D>     listShadow =        new List<Texture2D>();
            List<Renderer>      listRenderers =     new List<Renderer>();
            List<int>           listIndexes =       new List<int>();
            List<Vector4>       listScales =        new List<Vector4>();

            for( var i = 0; i < renderers.Count; ++i )
            {
                // Scan current list of static lightmaps inside baker and compare thier indexes to the current renderer target lightmap index

                var slm = Baker.GetSceneLightmapFromRendererIndex( renderers[ i ].lightmapIndex );

                // Only if lightmap index wasn't found, save textures reference in the prefab 

                int rendererLightmapIndex = listColor.IndexOf( slm.texColor );
                
                if( rendererLightmapIndex == -1 )
                {
                    // Set index to the size of the current array 

                    rendererLightmapIndex = listColor.Count;

                    listColor.Add( slm.texColor );

                    // Optional textures are checked for null
                    
                    if( slm.texDir != null )        listDir.Add( slm.texDir );
                    if( slm.texShadow != null )     listShadow.Add( slm.texShadow );
                }

                MeshRenderer renderer = renderers[ i ];

                // For each renderer add its reference, lightmap index and scale offset by default 

                listRenderers.Add( renderer );
                listIndexes.Add( rendererLightmapIndex );
                listScales.Add( renderer.lightmapScaleOffset );
            }

            // Convert data to match prefab format for proper serialization 

            prefab.texturesColor = listColor.ToArray();
            prefab.texturesDir = listDir.ToArray();
            prefab.texturesShadow = listShadow.ToArray();
            prefab.renderers = listRenderers.ToArray();
            prefab.renderersLightmapIndex = listIndexes.ToArray();
            prefab.renderersLightmapOffsetScale = listScales.ToArray();
        }

        public static void UpdateLights( PrefabBaker component )
        {
            var lights = component.gameObject.GetComponentsInChildren<Light>( true );

            component.lights = lights.Select( light => new LightInfo
            {
                light = light,
                lightmapBaketype = ( int ) light.lightmapBakeType,
                mixedLightingMode = ( int ) LightmapEditorSettings.mixedBakeMode

            } ).ToArray();
        }

        public static void UpdatePrefab( GameObject prefab )
        {
            var targetPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab) as GameObject;

            if( targetPrefab == null )
            {
                var dsc = System.IO.Path.DirectorySeparatorChar;
                var scene = SceneManager.GetActiveScene();
                var folder = System.IO.Path.GetDirectoryName( scene.path ); // + dsc + scene.name;
                var file = folder + dsc + prefab.name + ".prefab";

                bool success; 

                GameObject result = PrefabUtility.SaveAsPrefabAssetAndConnect( prefab, file, InteractionMode.AutomatedAction, out success );

                if( ! success )

                    Debug.LogError( "[PrefabBaker] Target is not a prefab not it was possible to save it as one (" + prefab.name + ")" );

                else Debug.Log( "[PrefabBaker] Prefab was saved to: " + file );

                return;
            }

            GameObject prefab_root = PrefabUtility.GetOutermostPrefabInstanceRoot( prefab );

            if( prefab_root == null )
            {
                Debug.LogWarning( "[PrefabBaker] Failed to find prefab root: " + prefab.name );

                PrefabUtility.ApplyPrefabInstance( prefab, InteractionMode.AutomatedAction );
            }
            else
            {
                GameObject rootPrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefab);

                string rootPath = AssetDatabase.GetAssetPath(rootPrefab);

                PrefabUtility.UnpackPrefabInstanceAndReturnNewOutermostRoots( prefab_root, PrefabUnpackMode.OutermostRoot );

                try { PrefabUtility.ApplyPrefabInstance( prefab, InteractionMode.AutomatedAction ); }
                catch {}
                finally { PrefabUtility.SaveAsPrefabAssetAndConnect( prefab_root, rootPath, InteractionMode.AutomatedAction ); }
            }
        }

        public static Texture2D SaveLightmapAsset( string copyFrom, string saveTo )
        {
            if( saveTo.Contains(Application.dataPath) )
            {
                saveTo.Replace( Application.dataPath, "" );
            }

            UpdateAsset( copyFrom );
            var importer = AssetImporter.GetAtPath( copyFrom ) as TextureImporter;
            importer.isReadable = true;
            importer.maxTextureSize = Window.TextureSize;
            importer.textureCompression = TextureImporterCompression.Compressed;

            // Refresh and Save 
            UpdateAsset( copyFrom );
            var lightMapAsset = AssetDatabase.LoadAssetAtPath<Texture2D>( copyFrom );
            var lightMapCopy = Object.Instantiate( lightMapAsset );

            try
            {
                Directory.CreateDirectory( Directory.GetParent( saveTo ).FullName );

                AssetDatabase.CreateAsset( lightMapCopy, saveTo );
            }
            catch
            {
                Debug.LogError( $"[PrefabBaker] Failed to created asset:\nfrom: {copyFrom}\nto: {saveTo}" );
            }

            // Refresh
            lightMapCopy = AssetDatabase.LoadAssetAtPath<Texture2D>( saveTo );
            importer.isReadable = false;
            UpdateAsset( copyFrom );

            return lightMapCopy;
        }

        static void UpdateAsset( string path )
        {
            AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceUpdate );
        }

        public static void CreateLightmapPreviewWindow(int lightmapId, bool realtimeLightmap = false, bool indexBased = true)
        {
            System.Reflection.Assembly
                .GetAssembly( typeof( EditorWindow ) )
                .GetType( "UnityEditor.LightmapPreviewWindow")
                .GetMethod( "CreateLightmapPreviewWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static )
                .Invoke( null, new object[ ] { lightmapId, realtimeLightmap, indexBased } );
        }
        public static void BoxGUI( System.Action callback, int paddingH = 5, int paddingV = 5 ) {
            using(new GUILayout.HorizontalScope( GUI.skin.textArea )) {
                GUILayout.Space( paddingH );
                using(new GUILayout.VerticalScope( )) {
                    GUILayout.Space( paddingV );
                    callback.Invoke( );
                    GUILayout.Space( paddingV );
                }
                GUILayout.Space( paddingH );
            }
        }

        public static void Reset( PrefabBaker prefab )
        {
            var flags = ( StaticEditorFlags ) 0;

            GameObjectUtility.SetStaticEditorFlags( prefab.gameObject, flags );

            foreach(var t in prefab.gameObject.GetComponentsInChildren<Transform>( ))

                GameObjectUtility.SetStaticEditorFlags( t.gameObject, flags );

            foreach(var light in prefab.gameObject.GetComponentsInChildren<Light>( ))

                light.lightmapBakeType = LightmapBakeType.Realtime;

            foreach(var r in prefab.gameObject.GetComponentsInChildren<MeshRenderer>( ))
            {
                r.lightmapIndex = -1;
                r.realtimeLightmapIndex = -1;
            }

            List<LightmapData> lmds = new List<LightmapData>();

            foreach(var lmd in LightmapSettings.lightmaps)
            {
                bool found = false;

                if(         prefab.texturesColor?.Contains( lmd.lightmapColor ) ?? false ) found = true;
                else if(    prefab.texturesDir?.Contains(     lmd.lightmapDir ) ?? false ) found = true;
                else if(    prefab.texturesShadow?.Contains(   lmd.shadowMask ) ?? false ) found = true;

                if( ! found) lmds.Add( lmd );
            }

            prefab.texturesColor = null;
            prefab.texturesDir = null;
            prefab.texturesShadow = null;
            prefab.lights = null;
            prefab.renderers = null;
            prefab.renderersLightmapIndex = null;
            prefab.renderersLightmapOffsetScale = null;

            LightmapSettings.lightmaps = lmds.ToArray( );

            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.Iterative;
        }
    }
}

#endif