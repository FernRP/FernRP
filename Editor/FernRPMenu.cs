namespace UnityEditor.Rendering.Universal.Internal
{
    public class FernRPMenu
    {
        [MenuItem("Fern RP/DeveloperMode/Enable")]
        private static void EnableDeveloperMode()
        {
            EditorPrefs.SetBool("DeveloperMode", true);
        }
        
        [MenuItem("Fern RP/DeveloperMode/Disable")]
        private static void DisableeveloperMode()
        {
            EditorPrefs.SetBool("DeveloperMode", false);
        }
    }
}