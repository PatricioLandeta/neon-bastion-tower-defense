using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DefenseSceneBuilder
{
    public const string ScenePath="Assets/Scenes/NeonBastion.unity";
    [MenuItem("Tools/Neon Bastion/Rebuild authored assets and scene")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/Defense/Resources/DefenseMeshes");
        foreach(string name in DefenseArt.MeshNames)
        {
            string path="Assets/Defense/Resources/DefenseMeshes/"+name+".asset";
            Mesh generated=DefenseArt.GenerateMesh(name);
            Mesh existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null) AssetDatabase.CreateAsset(generated,path);
            else { EditorUtility.CopySerialized(generated,existing);Object.DestroyImmediate(generated); }
        }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Neon Bastion — Game Director").AddComponent<DefenseGame>();
        EditorSceneManager.SaveScene(scene,ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("NEON BASTION BUILD COMPLETE");
    }
}
