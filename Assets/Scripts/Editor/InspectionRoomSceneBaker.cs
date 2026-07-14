using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class InspectionRoomSceneBaker
{
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Sector 7/Bake Inspection Room Into Scene")]
    public static void BakeSampleScene()
    {
        EditorSceneManager.OpenScene(GameplayScenePath);
        DestroyIfExists(CheckpointBootstrap.RuntimeRootName);
        DestroyIfExists("Inspection Room Root");
        DestroyIfExists("Inspection Room Root Old");

        var bakerObject = new GameObject("InspectionRoomBakeRunner");
        var builder = bakerObject.AddComponent<InspectionRoomBuilder>();
        builder.BuildScene();

        Object.DestroyImmediate(bakerObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        Debug.Log("Inspection room baked into " + GameplayScenePath);
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }
    }
}
