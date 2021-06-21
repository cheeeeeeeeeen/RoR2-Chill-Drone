#undef DEBUG

using BepInEx;
using Chen.Helpers.GeneralHelpers;
using Chen.Helpers.LogHelpers;
using R2API.Utils;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Chen.Helpers.GeneralHelpers.AssetsManager;

[assembly: InternalsVisibleTo("ChillDrone.Tests")]

namespace Chen.ChillDrone
{
    /// <summary>
    /// Description of the plugin.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(Helpers.HelperPlugin.ModGuid, Helpers.HelperPlugin.ModVer)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency()]
    public class ModPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// This mod's version.
        /// </summary>
        public const string ModVer =
#if DEBUG
            "0." +
#endif
            "1.0.0";

        /// <summary>
        /// This mod's name.
        /// </summary>
        public const string ModName = "ChillDrone";

        /// <summary>
        /// This mod's GUID.
        /// </summary>
        public const string ModGuid = "com.Chen.ChillDrone";

        internal static Log Log;
        internal static AssetBundle bundle;

        private void Awake()
        {
            Log = new Log(Logger);

#if DEBUG
            Chen.Helpers.GeneralHelpers.MultiplayerTest.Enable(Log);
#endif
            BundleInfo assetBundle = new BundleInfo("ChillDrone.mymod_assets", BundleType.UnityAssetBundle);
            bundle = new AssetsManager(assetBundle).Register() as AssetBundle;
        }

        internal static bool DebugCheck()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}