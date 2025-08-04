using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace yunst.npot.reporter
{
    public class TextureCompressionTool : EditorWindow
    {
        private string folderPath = "";

        [MenuItem("Tools/Texture Tools/Batch Texture Crunch Compression")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(TextureCompressionTool));
        }

        // Add a context menu option for applying compression
        [MenuItem("Assets/Texture Tools/Apply Crunch Compression", false, 20)]
        private static void ApplyCrunchCompressionFromContextMenu()
        {
            string[] selectedPaths = Selection.assetGUIDs;
            if (selectedPaths.Length > 0)
            {
                foreach (string assetGUID in selectedPaths)
                {
                    string path = AssetDatabase.GUIDToAssetPath(assetGUID);
                    if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D))
                    {
                        ApplyCompressionAtPath(path);
                    }
                }
                AssetDatabase.Refresh();
                Debug.Log("Texture compression format changed to Crunch for selected textures.");
            }
            else
            {
                Debug.LogWarning("No textures selected.");
            }
        }

        void OnGUI()
        {
            GUILayout.Label("Change Texture Compression Format", EditorStyles.boldLabel);

            GUILayout.Label("Select Folder with Textures", EditorStyles.label);
            if (GUILayout.Button("Select Folder"))
            {
                folderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
                if (!string.IsNullOrEmpty(folderPath))
                {
                    folderPath = "Assets" + folderPath.Substring(Application.dataPath.Length);
                }
            }

            if (!string.IsNullOrEmpty(folderPath))
            {
                GUILayout.Label("Selected Folder: " + folderPath, EditorStyles.label);

                if (GUILayout.Button("Apply Crunch Compression to Selected Folder"))
                {
                    ShowConfirmationDialog("Apply Crunch Compression to Selected Folder?", () => ApplyCrunchCompression(folderPath));
                }
            }

            GUILayout.Space(20);

            GUILayout.Label("Or apply to entire project:", EditorStyles.label);
            if (GUILayout.Button("Apply Crunch Compression to Entire Project"))
            {
                ShowConfirmationDialog("Apply Crunch Compression to Entire Project?", () => ApplyCrunchCompression(""));
            }
        }

        // Function to display a confirmation dialog
        private static void ShowConfirmationDialog(string message, System.Action confirmedAction)
        {
            if (EditorUtility.DisplayDialog("Confirmation", message, "Apply", "Cancel"))
            {
                confirmedAction?.Invoke();
            }
        }

        // Function to apply crunch compression to textures
        private static void ApplyCrunchCompression(string folderPath)
        {
            string[] guids;
            if (!string.IsNullOrEmpty(folderPath))
            {
                guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            }
            else
            {
                guids = AssetDatabase.FindAssets("t:Texture2D");
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/"))
                {
                    continue; // Skip textures in the Packages folder
                }
                ApplyCompressionAtPath(path);
            }

            AssetDatabase.Refresh();
            Debug.Log("Texture compression format changed to Crunch.");
        }

        // Function to apply compression settings to a single texture
        public static void ApplyCompressionAtPath(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                if (importer.textureType == TextureImporterType.Sprite)
                {
                    // Disable mipmaps
                    importer.mipmapEnabled = false;

                    // Set compression settings
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.crunchedCompression = true;

                    // Save and reimport
                    importer.SaveAndReimport();
                }
                else
                {
                    // Set compression settings
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.crunchedCompression = true;

                    // Save and reimport
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
