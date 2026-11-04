using UnityEditor;
using UnityEditor.SceneManagement;

public static class ChillhopTest
{
    public static void Run()
    {
        EditorSettings.asyncShaderCompilation = false;   // otherwise shots taken in the first seconds show pink for uncompiled variants
        EditorSceneManager.OpenScene("Assets/Scenes/ChillhopCrate.unity");
        EditorApplication.EnterPlaymode();
    }
}
