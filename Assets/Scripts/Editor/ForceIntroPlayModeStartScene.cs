#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class ForceIntroPlayModeStartScene
{
    private const string IntroScenePath = "Assets/Scenes/Scene0-Intro.unity";

    static ForceIntroPlayModeStartScene()
    {
        EditorApplication.delayCall += ApplyForcedStartScene;
    }

    private static void ApplyForcedStartScene()
    {
        SceneAsset introScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(IntroScenePath);
        if (introScene == null)
            return;

        if (EditorSceneManager.playModeStartScene == introScene)
            return;

        EditorSceneManager.playModeStartScene = introScene;
    }
}
#endif
