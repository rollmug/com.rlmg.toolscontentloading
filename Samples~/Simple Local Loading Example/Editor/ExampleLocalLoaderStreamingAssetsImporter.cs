namespace rlmg.Tools.ContentLoading.Examples.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    public static class ExampleLocalLoaderStreamingAssetsImporter
    {
        static ExampleLocalLoaderStreamingAssetsImporter()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {

            if (scene.name.Contains("ExampleSimpleLocalLoader"))
                ImportStreamingAssets();
        }
        private static void ImportStreamingAssets()
        {
            SampleStreamingAssetsImporterUtils.CopySampleFilesToStreamingAssets(
                importerScriptName: nameof(ExampleLocalLoaderStreamingAssetsImporter),
                destStreamingAssetsSubFolderName: "RLMG Content Loading Samples/Simple Local Loading");
        }
    }

}