namespace rlmg.Tools.ContentLoading.Examples.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    public static class ExampleRemoteMediaWithCachingStreamingAssetsImporter
    {
        static ExampleRemoteMediaWithCachingStreamingAssetsImporter()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {

            if (scene.name.Contains("ExampleRemoteMediaWithCaching"))
                ImportStreamingAssets();
        }
        private static void ImportStreamingAssets()
        {
            SampleStreamingAssetsImporterUtils.CopySampleFilesToStreamingAssets(
                importerScriptName: nameof(ExampleRemoteMediaWithCachingStreamingAssetsImporter),
                destStreamingAssetsSubFolderName: "RLMG Content Loading Samples/Remote Media With Caching");
        }
    }

}
