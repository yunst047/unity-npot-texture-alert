using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace yunst.npot.reporter
{
    public class PreBuildAlert : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;
        


        public void OnPreprocessBuild(BuildReport report)
        {
            if (EditorPrefs.GetBool("yunst.npot.reporter.EnablePreprocessBuild", false))
            {
                var textures = TextureChecker.CheckTextures(false);
                if (textures.Count > 0)
                {
                    PreBuildTextureAlertWindow.ShowWindow(textures, null);
                 
                }
            }
        }
    }
}
