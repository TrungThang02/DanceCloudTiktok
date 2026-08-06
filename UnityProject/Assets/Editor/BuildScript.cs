#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TikTokLiveGame.Editor
{
    public static class BuildScript
    {
        [MenuItem("Build/Build Windows Standalone")]
        public static void BuildWindows()
        {
            string[] scenes = { "Assets/Scenes/Main.unity" };
            string locationPathName = "../TikTokLiveGameUnity.exe";
            BuildPipeline.BuildPlayer(scenes, locationPathName, BuildTarget.StandaloneWindows64, BuildOptions.None);
            Debug.Log("Build Windows Standalone completed: " + locationPathName);
        }
    }
}
#endif
