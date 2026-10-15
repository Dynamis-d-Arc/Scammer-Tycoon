using UnityEditor;
using UnityEditor.SceneManagement;

// A fresh checkout starts in the playable scene instead of an empty Untitled scene.
[InitializeOnLoad]
public static class StartupScene
{
    static StartupScene()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path) && !scene.isDirty)
                EditorSceneManager.OpenScene("Assets/Scenes/ScammerTown.unity");
        };
    }
}
