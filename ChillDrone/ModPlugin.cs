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
    public class ModPlugin : BaseUnityPlugin
    {
        /// <summary>
        /// This mod's version.
        /// </summary>
        public const string ModVer =
#if DEBUG
            "0." +
#endif
            "2.2.0";

        /// <summary>
        /// This mod's name.
        /// </summary>
        public const string ModName = "ChillDrone";

        /// <summary>
        /// This mod's GUID.
        /// thank you chen for your amazing mods and all you've done for the RoR2 Modding Community - Phreel
        /// </summary>
        public const string ModGuid = "com.Chen.ChillDrone";

        internal static ConfigFile cfgFile;
        internal static Log Log;
        internal static List<GradiusMod.Drones.DroneInfo> dronesList = new List<GradiusMod.Drones.DroneInfo>();
        internal static AssetBundle assetBundle;

        private void Awake()
        {
            Log = new Log(Logger);

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