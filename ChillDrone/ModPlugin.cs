#undef DEBUG

using BepInEx;
using BepInEx.Configuration;
using Chen.GradiusMod.Drones;
using Chen.Helpers.GeneralHelpers;
using Chen.Helpers.LogHelpers;
using R2API.Utils;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using static Chen.Helpers.GeneralHelpers.AssetsManager;

[assembly: InternalsVisibleTo("ChillDrone.Tests")]

namespace Chen.ChillDrone
{
    /// <summary>
    /// Unity Plugin to set the Chill Drone up.
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(Helpers.HelperPlugin.ModGuid, Helpers.HelperPlugin.ModVer)]
    [BepInDependency(GradiusMod.GradiusModPlugin.ModGuid, GradiusMod.GradiusModPlugin.ModVer)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency("DamageAPI", "RecalculateStatsAPI")]
    public class ModPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// This mod's version.
        /// </summary>
        public const string ModVer =
#if DEBUG
            "0." +
#endif
            "2.0.1";

        /// <summary>
        /// This mod's name.
        /// </summary>
        public const string ModName = "ChillDrone";

        /// <summary>
        /// This mod's GUID.
        /// </summary>
        public const string ModGuid = "com.Chen.ChillDrone";

        internal static ConfigFile cfgFile;
        internal static Log Log;
        internal static List<DroneInfo> dronesList = new List<DroneInfo>();
        internal static AssetBundle assetBundle;
        internal static ContentProvider contentProvider;

        private void Awake()
        {
            Log = new Log(Logger);
            contentProvider = new ContentProvider();

#if DEBUG
            Chen.Helpers.GeneralHelpers.MultiplayerTest.Enable(Log);
#endif

            Log.Debug("Initializing config file...");
            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            Log.Debug("Loading asset bundle...");
            BundleInfo bundleInfo = new BundleInfo("Chen.ChillDrone.chilldrone_assets", BundleType.UnityAssetBundle);
            assetBundle = new AssetsManager(bundleInfo).Register();
            assetBundle.ConvertShaders();

            Log.Debug("Registering Chill Drone...");
            dronesList = DroneCatalog.Initialize(ModGuid, cfgFile);
            DroneCatalog.ScopedSetupAll(dronesList);

            contentProvider.Initialize();
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