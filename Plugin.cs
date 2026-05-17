using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace GWYF.WhoNeedsEyes
{
    [BepInProcess("Gamble With Your Friends.exe")]
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;

            new Harmony(MyPluginInfo.PLUGIN_GUID).PatchAll();

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    public static class MyPluginInfo
    {
        public const string PLUGIN_GUID = "GWYF.Epoch.WhoNeedsEyes";
        public const string PLUGIN_NAME = "Who Needs Eyes!";
        public const string PLUGIN_VERSION = "1.0.1";
    }
}
