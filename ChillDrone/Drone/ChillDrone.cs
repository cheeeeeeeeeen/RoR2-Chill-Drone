#undef DEBUG

using Chen.ChillDrone.Drone.States;
using Chen.GradiusMod;
using Chen.GradiusMod.Drones;
using Chen.GradiusMod.Items.GradiusOption;
using Chen.Helpers.CollectionHelpers;
using Chen.Helpers.GeneralHelpers;
using Chen.Helpers.UnityHelpers;
using EntityStates;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Skills;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Chen.ChillDrone.ModPlugin;
using static R2API.DamageAPI;
using static R2API.DirectorAPI;
using static R2API.DirectorAPI.Helpers;
using static R2API.RecalculateStatsAPI;
using UnityObject = UnityEngine.Object;

namespace Chen.ChillDrone.Drone
{
    internal class ChillDrone : Drone<ChillDrone>
    {
        public static ModdedDamageType chillOnHit { get; private set; }
        public static BuffDef chillBuff { get; private set; }
        public static InteractableSpawnCard iSpawnCard { get; private set; }

        public override bool canHaveOptions => true;

        private GameObject brokenObject { get; set; }
        private DirectorCardHolder iDirectorCardHolder { get; set; }
        private GameObject droneBody { get; set; }
        private GameObject droneMaster { get; set; }

        private static readonly Lazy<InteractableSpawnCard> _iscBasis = LazyAddressable<InteractableSpawnCard>("RoR2/Base/Drones/iscBrokenDrone1.asset");
        private static readonly Lazy<SkillDef> _skillBasis = LazyAddressable<SkillDef>("RoR2/Base/Drones/Drone1BodyGun.asset");
        private static readonly Lazy<GameObject> _flameDroneMaster = LazyAddressable<GameObject>("RoR2/Base/Drones/FlameDroneMaster.prefab");

        private static InteractableSpawnCard interactableSpawnCardBasis { get => _iscBasis.Value; }
        private static SkillDef skillBasis { get => _skillBasis.Value; }
        private static GameObject flameDroneMaster { get => _flameDroneMaster.Value; }

        protected override GameObject DroneCharacterMasterObject => droneMaster;

        protected override void SetupConfig()
        {
            spawnWeightWithMachinesArtifact = 0;
            base.SetupConfig();
        }

        protected override void SetupComponents()
        {
            base.SetupComponents();
            LanguageAPI.Add("CHILL_DRONE_NAME", "Chill Drone");
            LanguageAPI.Add("CHILL_DRONE_CONTEXT", "Repair Chill Drone");
            LanguageAPI.Add("CHILL_DRONE_INTERACTABLE_NAME", "Broken Chill Drone");
            LanguageAPI.Add("CHILL_DRONE_PICKUP", "Slows nearby enemies with chilling beams.");
            LanguageAPI.Add("CHILL_DRONE_DESCRIPTION",
                $"Fires beams at every enemy within <style=cIsUtility>{EmitSlow.detectionDistance}m</style> in front of it, " +
                "<style=cIsUtility>slowing</style> them by <style=cIsUtility>70%</style> while they are hit.");
            LanguageAPI.Add("CHILL_DRONE_SKILL_DESCRIPTION",
                $"Chilling Beams: Beam every nearby enemy for up to {EmitSlow.duration} seconds, <style=cIsUtility>slowing</style> them.");
            GameObject pickupDisplay = CreatePickupDisplay(assetBundle.LoadAsset<GameObject>("Assets/GEMO/mdlChillDrone.prefab"));
            brokenObject = interactableSpawnCardBasis.prefab.InstantiateClone($"{name}Broken", true);
            SummonMasterBehavior summonMasterBehavior = brokenObject.GetComponent<SummonMasterBehavior>();
            droneMaster = summonMasterBehavior.masterPrefab.InstantiateClone($"{name}Master", true);
            ContentAddition.AddMaster(droneMaster);
            foreach (var skillDriver in droneMaster.GetComponents<AISkillDriver>())
            {
                UnityObject.DestroyImmediate(skillDriver);
            }
            AISkillDriver[] newSkillDrivers = flameDroneMaster.DeepCopyComponentsTo<AISkillDriver>(droneMaster).ToArray();
            if (newSkillDrivers.Length > 3)
            {
                newSkillDrivers[1].maxDistance = EmitSlow.detectionDistance;
                newSkillDrivers[2].maxDistance = newSkillDrivers[1].maxDistance + (EmitSlow.detectionDistance * .5f);
                newSkillDrivers[3].minDistance = newSkillDrivers[2].maxDistance;
            }
            else Log.Warning($"{name}: The Incinerator Drone skill drivers changed. Attack ranges are left at their default values.");
            newSkillDrivers.SetAllDriversToAimTowardsEnemies();
            CharacterMaster master = droneMaster.GetComponent<CharacterMaster>();
            droneBody = master.bodyPrefab.InstantiateClone($"{name}Body", true);
            ContentAddition.AddBody(droneBody);
            CharacterBody body = droneBody.GetComponent<CharacterBody>();
            body.baseNameToken = "CHILL_DRONE_NAME";
            body.baseMaxHealth *= .9f;
            body.baseRegen *= 3;
            body.baseDamage = 3;
            body.baseCrit = 0f;
            body.baseArmor *= 1.3f;
            body.baseMoveSpeed *= .7f;
            body.levelMaxHealth *= .9f;
            body.levelRegen *= 3;
            body.levelDamage = 1;
            body.levelCrit = 0f;
            body.levelArmor *= 1.3f;
            body.levelMoveSpeed *= .7f;
            body.portraitIcon = assetBundle.LoadAsset<Texture>("Assets/GEMO/texChillDrone.png");
            GameObject customModel = assetBundle.LoadAsset<GameObject>("Assets/GEMO/mdlChillDrone.prefab");
            Quaternion originalRotation = customModel.transform.localRotation;
            droneBody.ReplaceModel(customModel);
            customModel.transform.localRotation = originalRotation;
            customModel.InitializeDroneModelComponents(body, 1.3f);
            customModel.transform.Find("PropellerLEffect").gameObject.AddComponent<RotateObject>().rotationSpeed = new Vector3(0f, 0f, 900f);
            customModel.transform.Find("PropellerREffect").gameObject.AddComponent<RotateObject>().rotationSpeed = new Vector3(0f, 0f, -900f);
            SkillLocator locator = droneBody.GetComponent<SkillLocator>();
            ContentAddition.AddEntityState<EmitSlow>(out _);
            SkillDef newSkillDef = UnityObject.Instantiate(skillBasis);
            newSkillDef.activationState = new SerializableEntityStateType(typeof(EmitSlow));
            newSkillDef.baseRechargeInterval = 4;
            newSkillDef.beginSkillCooldownOnSkillEnd = true;
            newSkillDef.baseMaxStock = 1;
            newSkillDef.fullRestockOnAssign = false;
            ContentAddition.AddSkillDef(newSkillDef);
            SkillFamily skillFamily = UnityObject.Instantiate(locator.primary.skillFamily);
            skillFamily.variants = new SkillFamily.Variant[1];
            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = newSkillDef,
                unlockableDef = null,
                viewableNode = new ViewablesCatalog.Node("", false, null)
            };
            locator.primary.SetFieldValue("_skillFamily", skillFamily);
            ContentAddition.AddSkillFamily(skillFamily);
            CharacterDeathBehavior death = body.GetOrAddComponent<CharacterDeathBehavior>();
            death.deathState = new SerializableEntityStateType(typeof(DeathState));
            master.bodyPrefab = droneBody;
            summonMasterBehavior.masterPrefab = droneMaster;
            PurchaseInteraction purchaseInteraction = brokenObject.GetComponent<PurchaseInteraction>();
            purchaseInteraction.cost = 80;
            purchaseInteraction.Networkcost = purchaseInteraction.cost;
            purchaseInteraction.contextToken = "CHILL_DRONE_CONTEXT";
            purchaseInteraction.displayNameToken = "CHILL_DRONE_INTERACTABLE_NAME";
            GenericDisplayNameProvider nameProvider = brokenObject.GetComponent<GenericDisplayNameProvider>();
            nameProvider.displayToken = "CHILL_DRONE_NAME";
            GameObject customBrokenModel = assetBundle.LoadAsset<GameObject>("Assets/GEMO/mdlChillDroneBroken.prefab");
            MeshRenderer highlightRenderer = customBrokenModel.GetComponentInChildren<MeshRenderer>();
            ReplaceBrokenModel(brokenObject, customBrokenModel, highlightRenderer, customBrokenModel.transform.GetChild(0).gameObject);
            Transform damagePoint = FindBrokenDamagePoint(customBrokenModel);
            if (damagePoint)
            {
                damagePoint.localPosition = Vector3.zero;
                damagePoint.localRotation = Quaternion.identity;
                damagePoint.localScale = Vector3.one;
            }
            iSpawnCard = UnityObject.Instantiate(interactableSpawnCardBasis);
            iSpawnCard.name = $"iscBroken{name}";
            iSpawnCard.prefab = brokenObject;
            DirectorCard directorCard = new DirectorCard
            {
                spawnCard = iSpawnCard,
#if DEBUG
                selectionWeight = 1000,
#else
                selectionWeight = 2,
#endif
                minimumStageCompletions = 0,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Close,
                preventOverhead = false
            };
            iDirectorCardHolder = new DirectorCardHolder
            {
                Card = directorCard,
                MonsterCategory = MonsterCategory.Invalid,
                InteractableCategory = InteractableCategory.Drones,
            };
            droneDef = CreateDroneDef(LoadVanillaDroneDef("RoR2/Base/Drones/MissileDrone.asset"), droneBody, brokenObject, iSpawnCard, "CHILL_DRONE",
                                      body.portraitIcon, pickupDisplay, LoadVanillaDroneDef("RoR2/Base/Drones/Drone1.asset"));
        }

        protected override void SetupBehavior()
        {
            base.SetupBehavior();
            GradiusOption.instance.SetToRotateOptions(droneMaster.name);
            GradiusOption.instance.SetRotateOptionMultiplier(droneMaster.name, 1.2f);
            chillOnHit = ReserveDamageType();
            chillBuff = ScriptableObject.CreateInstance<BuffDef>();
            chillBuff.canStack = false;
            chillBuff.isDebuff = true;
            chillBuff.name = "Chill Drone - Chilled";
            chillBuff.iconSprite = assetBundle.LoadAsset<Sprite>("Assets/texBuffChill.png");
            ContentAddition.AddBuffDef(chillBuff);
            InteractableActions += DirectorAPI_InteractableActions;
            GetStatCoefficients += ChillDrone_GetStatCoefficients;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
        }

        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            if (self.body && damageInfo.HasModdedDamageType(chillOnHit)) self.body.AddTimedBuff(chillBuff, 1f);
        }

        private void ChillDrone_GetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(chillBuff))
            {
                args.moveSpeedMultAdd -= .7f;
                args.jumpPowerMultAdd -= .7f;
            }
        }

        private void DirectorAPI_InteractableActions(DccsPool arg0, StageInfo arg1)
        {
            if (!arg0) return;
#if DEBUG
            ForEachPoolEntryInDccsPool(arg0, (poolEntry) => poolEntry.dccs.AddCard(iDirectorCardHolder));
#else
            if (arg1.CheckStage(DirectorAPI.Stage.RallypointDelta) || arg1.CheckStage(DirectorAPI.Stage.SirensCall))
            {
                ForEachPoolEntryInDccsPool(arg0, (poolEntry) => poolEntry.dccs.AddCard(iDirectorCardHolder));
            }
#endif
        }

        private static Lazy<T> LazyAddressable<T>(string key)
        {
            return new Lazy<T>(() =>
            {
                T asset = Addressables.LoadAssetAsync<T>(key).WaitForCompletion();
                if (asset == null) Log.Error($"Failed to load the addressable asset {key}.");
                return asset;
            });
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
