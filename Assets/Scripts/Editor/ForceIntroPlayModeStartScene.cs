#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/*
* This script forces the Play Mode start scene to be the intro scene, ensuring that the game always starts from the intro when entering Play Mode in the editor.
* It uses the InitializeOnLoad attribute to run the static constructor when the editor loads, and sets the playModeStartScene to the intro scene if it's not already set.
* Exposes:
*   - None (this is an editor utility script that doesn't expose any public interface)
* Requires:
*   - The intro scene must be located at "Assets/Scenes/Scene0-Intro.unity" for the path to be correct.
*/

[InitializeOnLoad]
public static class ForceIntroPlayModeStartScene
{
    // Scene forced as play-mode entry point from the Unity Editor.
    private const string IntroScenePath = "Assets/Scenes/Scene0-Intro.unity";

    // Registers a delayed setup call after editor domain reload.
    static ForceIntroPlayModeStartScene()
    {
        EditorApplication.delayCall += ApplyForcedStartScene;
    }

    // Ensures Editor play mode always starts from intro scene if asset exists.
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
