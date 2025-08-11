using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace yunst.npot.reporter
{
    public class TextureChecker : EditorWindow
    {
        public static List<TextureInfo> nonCompliantTextures { get; private set; } = new List<TextureInfo>();
        private Vector2 scrollPos;
        private bool filterPOT = true;

        public bool EnablePreprocessBuild { get; private set; } = false;

        [MenuItem("Tools/Texture Tools/Non-compliant Texture Checker")]
        public static void ShowWindow()
        {
            GetWindow<TextureChecker>("Texture Checker");
        }

        private void OnGUI()
        {
            GUILayout.Label("Filter Options", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            filterPOT = EditorGUILayout.Toggle(filterPOT, GUILayout.Width(20));
            EditorGUILayout.LabelField("Only Find Non Multiple of Four Textures", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            GUILayout.Label("Pre-Build Options", EditorStyles.boldLabel);
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            bool newEnablePreprocessBuild = EditorGUILayout.Toggle(EnablePreprocessBuild, GUILayout.Width(20));
            EditorGUILayout.LabelField("Enable Preprocess Build Alert", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            if (newEnablePreprocessBuild != EnablePreprocessBuild)
            {
                EnablePreprocessBuild = newEnablePreprocessBuild;
                EditorPrefs.SetBool("yunst.npot.reporter.EnablePreprocessBuild", EnablePreprocessBuild);
                if (EnablePreprocessBuild)
                {
                    EditorUtility.DisplayDialog(
                        "Preprocess Build Enabled",
                        "The Preprocess Build feature is now enabled. It will check textures before building.",
                        "OK"
                    );
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Preprocess Build Disabled",
                        "The Preprocess Build feature is now disabled.",
                        "OK"
                    );
                }
            }
        


            GUILayout.Space(10);

            if (GUILayout.Button("Check Textures"))
            {
                nonCompliantTextures = CheckTextures(filterPOT);
                Repaint();
            }

            GUILayout.Space(10);

            if (nonCompliantTextures.Count > 0)
            {
                GUILayout.Label("Non-compliant textures:", EditorStyles.boldLabel);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(1000));
                foreach (var textureInfo in nonCompliantTextures)
                {
                    GUILayout.BeginVertical("box");
                    GUILayout.BeginHorizontal();

                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureInfo.Path);

                    if (
                        GUILayout.Button(
                            AssetPreview.GetAssetPreview(texture),
                            GUILayout.Width(50),
                            GUILayout.Height(50)
                        )
                    )
                    {
                        Selection.activeObject = texture;
                        EditorGUIUtility.PingObject(texture);
                    }

                    GUILayout.BeginVertical();
                    if (GUILayout.Button(textureInfo.Path, EditorStyles.linkLabel))
                    {
                        Selection.activeObject = texture;
                        EditorGUIUtility.PingObject(texture);
                    }

                    GUILayout.BeginVertical("box");

                    GUIStyle redTextStyle = new GUIStyle(EditorStyles.boldLabel);
                    redTextStyle.normal.textColor = Color.red;
                    if (
                        textureInfo.NotPowerOfTwo
                        && GUILayout.Button("Not Power of Two", redTextStyle)
                    )
                    {
                        if (textureInfo.NotSprite)
                        {
                            EditorUtility.DisplayDialog(
                                "Warning",
                                "This texture is not set as Sprite type. Please contact the contributor or check if it's a texture for UI before making changes.",
                                "OK"
                            );
                        }
                        else if (
                            EditorUtility.DisplayDialog(
                                "Resize Texture",
                                $"Do you want to resize {textureInfo.Path} to the nearest power of two? Please ensure this is a UI texture before proceeding.",
                                "Yes",
                                "No"
                            )
                        )
                        {
                            ResizeTextureToPowerOfTwo(textureInfo.Path);
                            if (EditorUtility.DisplayDialog(
                                "Crunch Compression",
                                $"Do you want to apply Crunch Compression to {textureInfo.Path}?",
                                "Yes",
                                "No"))
                            {
                                TextureCompressionTool.ApplyCompressionAtPath(textureInfo.Path);
                            }
                            nonCompliantTextures = CheckTextures(filterPOT); // Refresh after resizing
                        }
                    }
                    if (
                        textureInfo.NotMultipleOfFour
                        && GUILayout.Button("Not Multiple of Four", redTextStyle)
                    )
                    {
                        if (textureInfo.NotSprite)
                        {
                            EditorUtility.DisplayDialog(
                                "Warning",
                                "This texture is not set as Sprite type. Please contact the contributor or check if it's a texture for UI before making changes.",
                                "OK"
                            );
                        }
                        else if (
                            EditorUtility.DisplayDialog(
                                "Resize Texture",
                                $"Do you want to resize {textureInfo.Path} to the nearest multiple of four? Please ensure this is a UI texture before proceeding.",
                                "Yes",
                                "No"
                            )
                        )
                        {
                            ResizeTextureToMultipleOfFour(textureInfo.Path);
                            if (EditorUtility.DisplayDialog(
                                "Crunch Compression",
                                $"Do you want to apply Crunch Compression to {textureInfo.Path}?",
                                "Yes",
                                "No"))
                            {
                                TextureCompressionTool.ApplyCompressionAtPath(textureInfo.Path);
                            }
                            nonCompliantTextures = CheckTextures(filterPOT); // Refresh after resizing
                        }
                    }

                    GUILayout.EndVertical();
                    GUILayout.EndVertical();
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("All textures are compliant.", EditorStyles.boldLabel);
            }
        }

        private void OnEnable()
        {
            EnablePreprocessBuild = EditorPrefs.GetBool("yunst.npot.reporter.EnablePreprocessBuild", false);
        }

        public static List<TextureInfo> CheckTextures(bool filterPOT)
        {
            var result = new List<TextureInfo>();
            string[] guids = AssetDatabase.FindAssets("t:Texture");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/"))
                {
                    continue; // Skip textures in the Packages folder
                }

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                if (texture != null)
                {
                    int width = texture.width;
                    int height = texture.height;
                    bool notPowerOfTwo = !IsPowerOfTwo(width) || !IsPowerOfTwo(height);
                    bool notMultipleOfFour = !IsMultipleOfFour(width) || !IsMultipleOfFour(height);
                    bool notSprite = false;
                    string relativePath = path.Replace(Application.dataPath, "Assets");
                    TextureImporter importer =
                        AssetImporter.GetAtPath(relativePath) as TextureImporter;
                    if (importer.textureType != TextureImporterType.Sprite)
                    {
                        notSprite = true;
                    }
                    if (filterPOT)
                    {
                        if (notMultipleOfFour)
                        {
                            result.Add(
                                new TextureInfo
                                {
                                    Path = path,
                                    NotMultipleOfFour = notMultipleOfFour,
                                    NotSprite = notSprite
                                }
                            );
                        }
                    }
                    else
                    {
                        if (notPowerOfTwo || notMultipleOfFour)
                        {
                            result.Add(
                                new TextureInfo
                                {
                                    Path = path,
                                    NotPowerOfTwo = notPowerOfTwo,
                                    NotMultipleOfFour = notMultipleOfFour,
                                    NotSprite = notSprite
                                }
                            );
                        }
                    }
                }
            }

            return result;
        }

        public static bool IsMultipleOfFour(int value)
        {
            return value % 4 == 0;
        }

        public static bool IsPowerOfTwo(int value)
        {
            return (value & (value - 1)) == 0;
        }

        private void ResizeTextureToPowerOfTwo(string path)
        {
            TextureTool.ResizeAndSliceImage(path, false);
        }

        private void ResizeTextureToMultipleOfFour(string path)
        {
            TextureTool.ResizeAndSliceImage(path, true);
        }



        public class TextureInfo
        {
            public string Path { get; set; }
            public bool NotPowerOfTwo { get; set; }
            public bool NotMultipleOfFour { get; set; }

            public bool NotSprite { get; set; }
        }
        
       
    }
}
