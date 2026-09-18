namespace rlmg.Tools.ContentLoading.Examples.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    public static class ExampleSequentialLoadersStreamingAssetsImporter
    {
        static ExampleSequentialLoadersStreamingAssetsImporter()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {

            if (scene.name.Contains("ExampleSequentialLoaders"))
                ImportStreamingAssets();
        }

        private static void ImportStreamingAssets()
        {
            SampleStreamingAssetsImporterUtils.CopySampleFilesToStreamingAssets(
                importerScriptName: nameof(ExampleSequentialLoadersStreamingAssetsImporter),
                destStreamingAssetsSubFolderName: "RLMG Content Loading Samples/Sequential Loaders");
        }
    }

}