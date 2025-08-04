using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;


namespace yunst.npot.reporter
{
    public class TextureTool : EditorWindow
    {
        private string folderPath = "";

        [MenuItem("Tools/Texture Tools/Texture Resizer")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(TextureTool));
        }

        [MenuItem("Assets/Texture Tools/Resize Images to Nearest Multiple of Four")]
        private static void ResizeImagesToNearestMultipleOfFour(MenuCommand menuCommand)
        {
            string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Confirm Resize",
                        "Are you sure you want to resize all images in the folder to the nearest multiple of four?",
                        "Yes",
                        "No"
                    )
                )
                {
                    ResizeAndSliceImagesInFolder(folderPath, true);
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Invalid Folder",
                    "Please select a valid folder.",
                    "OK"
                );
            }
        }

        [MenuItem("Assets/Texture Tools/Resize Images to Nearest Power of Two")]
        private static void ResizeImagesToNearestPowerOfTwo(MenuCommand menuCommand)
        {
            string folderPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            {
                if (
                    EditorUtility.DisplayDialog(
                        "Confirm Resize",
                        "Are you sure you want to resize all images in the folder to the nearest power of two?",
                        "Yes",
                        "No"
                    )
                )
                {
                    ResizeAndSliceImagesInFolder(folderPath, false);
                }
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Invalid Folder",
                    "Please select a valid folder.",
                    "OK"
                );
            }
        }

        void OnGUI()
        {
            GUILayout.Label("Image Resizer", EditorStyles.boldLabel);
            GUILayout.Label("Select Folder with Images", EditorStyles.label);

            if (GUILayout.Button("Select Folder"))
            {
                folderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
            }

            if (!string.IsNullOrEmpty(folderPath))
            {
                GUILayout.Label("Selected Folder: " + folderPath, EditorStyles.label);

                if (GUILayout.Button("Resize Images to Nearest Multiple of Four"))
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Confirm Resize",
                            "Are you sure you want to resize all images in the folder to the nearest multiple of four?",
                            "Yes",
                            "No"
                        )
                    )
                    {
                        ResizeAndSliceImagesInFolder(folderPath, true);
                    }
                }

                if (GUILayout.Button("Resize Images to Nearest Power of Two"))
                {
                    if (
                        EditorUtility.DisplayDialog(
                            "Confirm Resize",
                            "Are you sure you want to resize all images in the folder to the nearest power of two?",
                            "Yes",
                            "No"
                        )
                    )
                    {
                        ResizeAndSliceImagesInFolder(folderPath, false);
                    }
                }
            }
        }

        private static int NearestMultipleOfFour(int value)
        {
            return (value + 3) & ~3;
        }

        private static int NearestPowerOfTwo(int value)
        {
            int power = 1;
            while (power < value)
            {
                power *= 2;
            }
            return power;
        }

        public static Texture2D ResizeImageWithPadding(
            Texture2D source,
            int newWidth,
            int newHeight
        )
        {
            TextureFormat format =
                source.format == TextureFormat.RGBA32 ? TextureFormat.RGBA32 : TextureFormat.RGB24;
            Color fillColor = format == TextureFormat.RGBA32 ? new Color(0, 0, 0, 0) : Color.black;

            Texture2D newTexture = new Texture2D(newWidth, newHeight, format, false);
            Color[] fillPixels = new Color[newWidth * newHeight];
            for (int i = 0; i < fillPixels.Length; i++)
                fillPixels[i] = fillColor;
            newTexture.SetPixels(fillPixels);

            int xOffset = (newWidth - source.width) / 2;
            int yOffset = (newHeight - source.height) / 2;

            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    newTexture.SetPixel(x + xOffset, y + yOffset, source.GetPixel(x, y));
                }
            }
            newTexture.Apply();

            return newTexture;
        }

        public static void ResizeAndSliceImagesInFolder(string folderPath, bool nearestMultipleOf4)
        {
            string[] files = Directory
                .GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(
                    s =>
                        s.EndsWith(".jpg", System.StringComparison.OrdinalIgnoreCase)
                        || s.EndsWith(".jpeg", System.StringComparison.OrdinalIgnoreCase)
                        || s.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)
                        || s.EndsWith(".bmp", System.StringComparison.OrdinalIgnoreCase)
                )
                .ToArray();

            foreach (string file in files)
            {
                string relativePath = file.Replace(Application.dataPath, "Assets");
                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer.textureType != TextureImporterType.Sprite)
                    continue;
                byte[] fileData = File.ReadAllBytes(file);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);

                int newWidth = nearestMultipleOf4
                    ? NearestMultipleOfFour(texture.width)
                    : NearestPowerOfTwo(texture.width);
                int newHeight = nearestMultipleOf4
                    ? NearestMultipleOfFour(texture.height)
                    : NearestPowerOfTwo(texture.height);

                Texture2D resizedTexture = ResizeImageWithPadding(texture, newWidth, newHeight);
                byte[] bytes = resizedTexture.EncodeToPNG();
                File.WriteAllBytes(file, bytes);

                SliceSprite(file, texture.width, texture.height, newWidth, newHeight);

                Debug.Log(
                    $"Resized and sliced {file} from {texture.width}x{texture.height} to {newWidth}x{newHeight}"
                );
                AssetDatabase.Refresh();
            }
        }

        public static void ResizeAndSliceImage(string filePath, bool nearestMultipleOf4)
        {
           
                string relativePath = filePath.Replace(Application.dataPath, "Assets");
                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer.textureType != TextureImporterType.Sprite)
                    return;
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);

                int newWidth = nearestMultipleOf4
                    ? NearestMultipleOfFour(texture.width)
                    : NearestPowerOfTwo(texture.width);
                int newHeight = nearestMultipleOf4
                    ? NearestMultipleOfFour(texture.height)
                    : NearestPowerOfTwo(texture.height);

                Texture2D resizedTexture = ResizeImageWithPadding(texture, newWidth, newHeight);
                byte[] bytes = resizedTexture.EncodeToPNG();
                File.WriteAllBytes(filePath, bytes);

                SliceSprite(filePath, texture.width, texture.height, newWidth, newHeight);

                Debug.Log(
                    $"Resized and sliced {filePath} from {texture.width}x{texture.height} to {newWidth}x{newHeight}"
                );
                AssetDatabase.Refresh();
            
        }


        public static void SliceSprite(
            string filePath,
            int originalWidth,
            int originalHeight,
            int newWidth,
            int newHeight
        )
        {
            string spriteName = Path.GetFileNameWithoutExtension(filePath);
            string relativePath = filePath.Replace(Application.dataPath, "Assets");
            TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = 100; // Use a standard value or adjust as needed

                List<SpriteMetaData> metas = new List<SpriteMetaData>();

                // Calculate offsets to center the original image in the new size
                int xOffset = (newWidth - originalWidth) / 2;
                int yOffset = (newHeight - originalHeight) / 2;

                // Create a single sprite metadata for the original size
                SpriteMetaData meta = new SpriteMetaData();
                meta.rect = new Rect(
                    xOffset,
                    newHeight - originalHeight - yOffset,
                    originalWidth,
                    originalHeight
                );
                meta.name = spriteName;
                meta.alignment = (int)SpriteAlignment.Center;
                meta.pivot = new Vector2(0.5f, 0.5f);
                metas.Add(meta);

                importer.spritesheet = metas.ToArray();

                TextureImporterSettings tis = new TextureImporterSettings();
                importer.ReadTextureSettings(tis);
                tis.spriteMeshType = SpriteMeshType.FullRect;
                tis.spriteExtrude = 0;
                tis.spriteGenerateFallbackPhysicsShape = false;
                importer.SetTextureSettings(tis);

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
                Debug.Log("Sliced sprite to original size: " + relativePath);
            }
            else
            {
                Debug.LogError(
                    "Failed to slice sprite: TextureImporter not found for " + relativePath
                );
            }
        }
    }
}
