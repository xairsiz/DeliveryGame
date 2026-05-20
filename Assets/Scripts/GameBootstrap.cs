using UnityEngine;
using UnityEngine.SceneManagement;

/// THE ONLY THING YOU NEED.
/// Press Play in ANY scene and the entire game builds itself from scratch.
/// No GameObjects to create, no components to add, no menus to click.
///
/// It also wipes whatever junk is already in the open scene first, so old
/// generated cities / leftover objects can never stack up again.
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        // Already built (e.g. a reload)? Don't build twice.
        if (Object.FindFirstObjectByType<DeliveryGameManager>() != null)
            return;

        // Clean slate: destroy everything currently in the open scene.
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
            Object.DestroyImmediate(root);

        // Build a fresh, complete game.
        var go  = new GameObject("Game");
        var gen = go.AddComponent<CityGenerator>();
        gen.GenerateCity();
    }
}
