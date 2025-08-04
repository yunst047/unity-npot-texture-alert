using UnityEngine;
using UnityEditor;

public class SampleScript 
{
    [MenuItem("Tools/Yunst047/NPOT Texture Alert/Show Alert")]
    public static void ShowAlert()
    {
        EditorUtility.DisplayDialog("NPOT Texture Alert", "This is a sample alert for NPOT textures.", "OK");
    }

    [MenuItem("Tools/Yunst047/NPOT Texture Alert/Check Textures")]
    public static void CheckTextures()
    {
        // Logic to check for NPOT textures would go here
        Debug.Log("Checking for NPOT textures...");
    }
}
