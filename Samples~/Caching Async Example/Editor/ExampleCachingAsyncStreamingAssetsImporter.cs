namespace rlmg.Tools.ContentLoading.Examples.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    public static class ExampleCachingAsyncStreamingAssetsImporter
    {
        static ExampleCachingAsyncStreamingAssetsImporter()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {

            if (scene.name.Contains("ExampleCachingAsync"))
                ImportStreamingAssets();
        }
        private static void ImportStreamingAssets()
        {
            SampleStreamingAssetsImporterUtils.CopySampleFilesToStreamingAssets(
                importerScriptName: nameof(ExampleCachingAsyncStreamingAssetsImporter),
                destStreamingAssetsSubFolderName: "RLMG Content Loading Samples/Caching Async");
        }

    }

}
