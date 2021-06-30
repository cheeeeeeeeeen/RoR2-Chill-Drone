#undef DEBUG

using Chen.ChillDrone.Drone.States;
using Chen.GradiusMod;
using Chen.GradiusMod.Drones;
using Chen.GradiusMod.Items.GradiusOption;
using Chen.Helpers.CollectionHelpers;
using Chen.Helpers.RoR2Helpers;
using Chen.Helpers.UnityHelpers;
using EntityStates;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Skills;
using System.Collections.Generic;
using UnityEngine;
using static Chen.ChillDrone.ModPlugin;
using static R2API.DamageAPI;
using static R2API.DirectorAPI;
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

        private static InteractableSpawnCard interactableSpawnCardBasis { get => Resources.Load<InteractableSpawnCard>("spawncards/interactablespawncard/iscBrokenDrone1"); }
        private static SkillDef skillBasis { get => Resources.Load<SkillDef>("skilldefs/drone1body/Drone1BodyGun"); }
        private static GameObject flameDroneMaster { get => Resources.Load<GameObject>("prefabs/charactermasters/FlameDroneMaster"); }

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
            brokenObject = interactableSpawnCardBasis.prefab.InstantiateClone($"{name}Broken", true);
            SummonMasterBehavior summonMasterBehavior = brokenObject.GetComponent<SummonMasterBehavior>();
            droneMaster = summonMasterBehavior.masterPrefab.InstantiateClone($"{name}Master", true);
            contentProvider.masterObjects.Add(droneMaster);
            foreach (var skillDriver in droneMaster.GetComponents<AISkillDriver>())
            {
                UnityObject.Destroy(skillDriver);
            }
            AISkillDriver[] newSkillDrivers = flameDroneMaster.DeepCopyComponentsTo<AISkillDriver>(droneMaster).ToArray();
            newSkillDrivers[1].maxDistance = EmitSlow.detectionDistance;
            newSkillDrivers[2].maxDistance = newSkillDrivers[1].maxDistance + (EmitSlow.detectionDistance * .5f);
            newSkillDrivers[3].minDistance = newSkillDrivers[2].maxDistance;
            newSkillDrivers.SetAllDriversToAimTowardsEnemies();
            CharacterMaster master = droneMaster.GetComponent<CharacterMaster>();
            droneBody = master.bodyPrefab.InstantiateClone($"{name}Body", true);
            contentProvider.bodyObjects.Add(droneBody);
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
            customModel.InitializeDroneModelComponents(body);
            customModel.transform.Find("PropellerLEffect").gameObject.AddComponent<RotateObject>().rotationSpeed = new Vector3(0f, 0f, 900f);
            customModel.transform.Find("PropellerREffect").gameObject.AddComponent<RotateObject>().rotationSpeed = new Vector3(0f, 0f, -900f);
            SkillLocator locator = droneBody.GetComponent<SkillLocator>();
            LoadoutAPI.AddSkill(typeof(EmitSlow));
            SkillDef newSkillDef = UnityObject.Instantiate(skillBasis);
            newSkillDef.activationState = new SerializableEntityStateType(typeof(EmitSlow));
            newSkillDef.baseRechargeInterval = 4;
            newSkillDef.beginSkillCooldownOnSkillEnd = true;
            newSkillDef.baseMaxStock = 1;
            newSkillDef.fullRestockOnAssign = false;
            LoadoutAPI.AddSkillDef(newSkillDef);
            SkillFamily skillFamily = UnityObject.Instantiate(locator.primary.skillFamily);
            skillFamily.variants = new SkillFamily.Variant[1];
            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = newSkillDef,
                viewableNode = new ViewablesCatalog.Node("", false, null)
            };
            locator.primary.SetFieldValue("_skillFamily", skillFamily);
            LoadoutAPI.AddSkillFamily(skillFamily);
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
            brokenObject.ReplaceModel(customBrokenModel);
            Highlight highlight = brokenObject.GetComponent<Highlight>();
            highlight.targetRenderer = customBrokenModel.GetComponentInChildren<MeshRenderer>();
            EntityLocator entityLocator = customBrokenModel.AddComponent<EntityLocator>();
            entityLocator.entity = brokenObject;
            GameObject coreObject = customBrokenModel.transform.GetChild(0).gameObject;
            EntityLocator coreEntityLocator = coreObject.AddComponent<EntityLocator>();
            coreEntityLocator.entity = brokenObject;
            GameObject brokenEffects = brokenObject.transform.Find("ModelBase").Find("BrokenDroneVFX").gameObject;
            brokenEffects.transform.parent = customBrokenModel.transform;
            GameObject sparks = brokenEffects.transform.Find("Small Sparks, Mesh").gameObject;
            ParticleSystem.ShapeModule sparksShape = sparks.GetComponent<ParticleSystem>().shape;
            sparksShape.shapeType = ParticleSystemShapeType.MeshRenderer;
            sparksShape.meshShapeType = ParticleSystemMeshShapeType.Edge;
            sparksShape.meshRenderer = (MeshRenderer)highlight.targetRenderer;
            GameObject damagePoint = brokenEffects.transform.Find("Damage Point").gameObject;
            damagePoint.transform.localPosition = Vector3.zero;
            damagePoint.transform.localRotation = Quaternion.identity;
            damagePoint.transform.localScale = Vector3.one;
            iSpawnCard = UnityObject.Instantiate(interactableSpawnCardBasis);
            iSpawnCard.name = $"iscBroken{name}";
            iSpawnCard.prefab = brokenObject;
            DirectorCard directorCard = new DirectorCard
            {
                spawnCard = iSpawnCard,
#if DEBUG
                selectionWeight = 1000,
#else
                selectionWeight = 1,
#endif
                minimumStageCompletions = 0,
                spawnDistance = DirectorCore.MonsterSpawnDistance.Close,
                allowAmbushSpawn = true,
                preventOverhead = false
            };
            iDirectorCardHolder = new DirectorCardHolder
            {
                Card = directorCard,
                MonsterCategory = MonsterCategory.None,
                InteractableCategory = InteractableCategory.Drones,
            };
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
            BuffAPI.Add(new CustomBuff(chillBuff));
            InteractableActions += DirectorAPI_InteractableActions;
            GetStatCoefficients += ChillDrone_GetStatCoefficients;
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
        }

        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            if (damageInfo.HasModdedDamageType(chillOnHit)) self.body.AddTimedBuff(chillBuff, 1f);
        }

        private void ChillDrone_GetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(chillBuff))
            {
                args.moveSpeedMultAdd -= .7f;
                args.jumpPowerMultAdd -= .7f;
            }
        }

        private void DirectorAPI_InteractableActions(List<DirectorCardHolder> arg1, StageInfo arg2)
        {
#if DEBUG
            arg1.ConditionalAdd(iDirectorCardHolder, card => iDirectorCardHolder == card);
#else
            if (arg2.CheckStage(DirectorAPI.Stage.RallypointDelta) || arg2.CheckStage(DirectorAPI.Stage.SirensCall))
            {
                arg1.ConditionalAdd(iDirectorCardHolder, card => iDirectorCardHolder == card);
            }
#endif
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