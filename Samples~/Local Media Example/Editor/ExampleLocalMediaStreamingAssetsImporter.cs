namespace rlmg.Tools.ContentLoading.Examples.Editor
{
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine.SceneManagement;

    [InitializeOnLoad]
    public static class ExampleLocalMediaStreamingAssetsImporter
    {
        static ExampleLocalMediaStreamingAssetsImporter()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {

            if (scene.name.Contains("ExampleLocalMedia"))
                ImportStreamingAssets();
        }
        private static void ImportStreamingAssets()
        {
            SampleStreamingAssetsImporterUtils.CopySampleFilesToStreamingAssets(
                importerScriptName: nameof(ExampleLocalMediaStreamingAssetsImporter),
                destStreamingAssetsSubFolderName: "RLMG Content Loading Samples/Local Media");
        }
        
    }

}
