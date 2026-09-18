namespace rlmg.Tools.ContentLoading.Examples.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    public static class ExampleGraphQLWithCachingStreamingAssetsImporter
    {
        static ExampleGraphQLWithCachingStreamingAssetsImporter()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {

            if (scene.name.Contains("ExampleGraphQLLoaderWithCaching"))
                ImportStreamingAssets();
        }
        private static void ImportStreamingAssets()
        {
            SampleStreamingAssetsImporterUtils.CopySampleFilesToStreamingAssets(
                importerScriptName: nameof(ExampleGraphQLWithCachingStreamingAssetsImporter),
                destStreamingAssetsSubFolderName: "RLMG Content Loading Samples/GraphQL With Caching");
        }
    }

}
