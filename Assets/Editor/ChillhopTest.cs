using UnityEditor;
using UnityEditor.SceneManagement;

public static class ChillhopTest
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ChillhopCrate.unity");
        EditorApplication.EnterPlaymode();
    }
}
