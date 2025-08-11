using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace yunst.npot.reporter
{
    public class PreBuildTextureAlertWindow : EditorWindow
    {
        private List<TextureChecker.TextureInfo> textures;
        private Vector2 scrollPos;
        private System.Action<bool> onClose;

        public static void ShowWindow(List<TextureChecker.TextureInfo> textures, System.Action<bool> onClose)
        {
            var window = GetWindow<PreBuildTextureAlertWindow>(true, "Non-compliant Textures Detected", true);
            window.textures = textures;
            window.onClose = onClose;
            window.minSize = new Vector2(500, 600);
            window.ShowModal();
        }

        private void OnGUI()
        {
            GUILayout.Label("The following textures are not a multiple of four in size (NPOT) and may increase build size:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
            foreach (var textureInfo in textures)
            {
                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureInfo.Path);
                GUILayout.Box(AssetPreview.GetAssetPreview(texture), GUILayout.Width(50), GUILayout.Height(50));
                GUILayout.BeginVertical();
                GUILayout.Label(textureInfo.Path, EditorStyles.label);
                if (textureInfo.NotPowerOfTwo)
                {
                    GUIStyle redTextStyle = new GUIStyle(EditorStyles.boldLabel);
                    redTextStyle.normal.textColor = Color.red;
                    GUILayout.Label("Not Power of Two", redTextStyle);
                }
                if (textureInfo.NotMultipleOfFour)
                {
                    GUIStyle redTextStyle = new GUIStyle(EditorStyles.boldLabel);
                    redTextStyle.normal.textColor = Color.red;
                    GUILayout.Label("Not Multiple of Four", redTextStyle);
                }
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Proceed"))
            {
                onClose?.Invoke(true);
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                onClose?.Invoke(false);
                Close();
            }
            GUILayout.EndHorizontal();
        }
    }
}
