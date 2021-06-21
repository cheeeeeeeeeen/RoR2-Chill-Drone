#undef DEBUG

using BepInEx;
using Chen.Helpers.GeneralHelpers;
using Chen.Helpers.LogHelpers;
using R2API.Utils;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Chen.Helpers.GeneralHelpers.AssetsManager;

[assembly: InternalsVisibleTo("ChensTemplate.Tests")]

namespace My.Mod.Namespace
{
    /// <summary>
    /// Description of the plugin.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(Chen.Helpers.HelperPlugin.ModGuid, Chen.Helpers.HelperPlugin.ModVer)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency()]
    public class MyModPluginPlugin : BaseUnityPlugin
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
        public const string ModName = "MyModName";

        /// <summary>
        /// This mod's GUID.
        /// </summary>
        public const string ModGuid = "com.Chen.MyModName";

        internal static Log Log;
        internal static AssetBundle bundle;

        private void Awake()
        {
            Log = new Log(Logger);

#if DEBUG
            Chen.Helpers.GeneralHelpers.MultiplayerTest.Enable(Log);
#endif
            BundleInfo assetBundle = new BundleInfo("ChensTemplate.mymod_assets", BundleType.UnityAssetBundle);
            BundleInfo soundBank = new BundleInfo("ChensTemplate.mymod_sounds.bnk", BundleType.WWiseSoundBank);
            bundle = new AssetsManager(assetBundle).Register() as AssetBundle;
            new AssetsManager(soundBank).Register();
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