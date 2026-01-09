using UnityEditor;
using UnityEngine;
using System.IO;

public static class CIBuilder
{
    public static void BuildWindows()
    {
        string productName = Sanitize(PlayerSettings.productName);
        string path = Path.Combine("Builds", "Windows", productName + ".exe");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            path,
            BuildTarget.StandaloneWindows64,
            BuildOptions.None
        );
    }

    public static void BuildAndroid()
    {
        string productName = Sanitize(PlayerSettings.productName);
        string path = Path.Combine("Builds", "Android", productName + ".apk");

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            path,
            BuildTarget.Android,
            BuildOptions.None
        );
    }

    private static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');

        return name.Trim();
    }
}