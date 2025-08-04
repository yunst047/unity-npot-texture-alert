using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace yunst.npot.reporter
{
    public class TextureChecker : EditorWindow
    {
        private List<TextureInfo> nonCompliantTextures = new List<TextureInfo>();
        private Vector2 scrollPos;
        private bool filterPOT = true;

        [MenuItem("Tools/Texture Tools/Non-compliant Texture Checker")]
        public static void ShowWindow()
        {
            GetWindow<TextureChecker>("Texture Checker");
        }

        private void OnGUI()
        {
            GUILayout.Label("Filter Options", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            filterPOT = EditorGUILayout.Toggle(filterPOT, GUILayout.Width(20));
            EditorGUILayout.LabelField("Only Find Non Multiple of Four Textures", GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (GUILayout.Button("Check Textures"))
            {
                CheckTextures();
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
                            CheckTextures(); // Refresh after resizing
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
                            CheckTextures(); // Refresh after resizing
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

        private void CheckTextures()
        {
            nonCompliantTextures.Clear();
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
                            nonCompliantTextures.Add(
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
                            nonCompliantTextures.Add(
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

            Repaint(); // Refresh the window to show results
        }

       
        public bool IsMultipleOfFour(int value)
        {
            return value % 4 == 0;
        }

        public bool IsPowerOfTwo(int value)
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



        private class TextureInfo
        {
            public string Path { get; set; }
            public bool NotPowerOfTwo { get; set; }
            public bool NotMultipleOfFour { get; set; }

            public bool NotSprite { get; set; }
        }
        
       
    }
}
