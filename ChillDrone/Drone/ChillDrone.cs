﻿#define DEBUG

using Chen.ChillDrone.Drone.States;
using Chen.GradiusMod;
using Chen.GradiusMod.Drones;
using Chen.Helpers.CollectionHelpers;
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
using static R2API.DirectorAPI;
using UnityObject = UnityEngine.Object;

namespace Chen.ChillDrone.Drone
{
    internal class ChillDrone : Drone<ChillDrone>
    {
        public InteractableSpawnCard iSpawnCard { get; set; }

        public override bool canHaveOptions => true;

        private GameObject brokenObject { get; set; }
        private DirectorCardHolder iDirectorCardHolder { get; set; }
        private GameObject droneBody { get; set; }
        private GameObject droneMaster { get; set; }

        private static InteractableSpawnCard interactableSpawnCardBasis { get => Resources.Load<InteractableSpawnCard>("spawncards/interactablespawncard/iscBrokenDrone1"); }
        private static SkillDef skillBasis { get => Resources.Load<SkillDef>("skilldefs/drone1body/Drone1BodyGun"); }

        private static GameObject drone1BrokenModel
        {
            get
            {
                return Resources.Load<GameObject>("prefabs/networkedobjects/brokendrones/Drone1Broken")
                                .transform.Find("ModelBase").Find("mdlDrone1").gameObject;
            }
        }

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
            GameObject brokenMeshModel = brokenObject.transform.Find("ModelBase").Find("mdlDrone1").Find("Drone1Mesh").gameObject;
            brokenMeshModel.GetComponent<MeshRenderer>().material = assetBundle.LoadAsset<Material>("Assets/matRustyIce.mat");
            SummonMasterBehavior summonMasterBehavior = brokenObject.GetComponent<SummonMasterBehavior>();
            droneMaster = summonMasterBehavior.masterPrefab.InstantiateClone($"{name}Master", true);
            contentProvider.masterObjects.Add(droneMaster);
            AISkillDriver[] skillDrivers = droneMaster.GetComponents<AISkillDriver>();
            skillDrivers.SetAllDriversToAimTowardsEnemies();
            CharacterMaster master = droneMaster.GetComponent<CharacterMaster>();
            droneBody = master.bodyPrefab.InstantiateClone($"{name}Body", true);
            contentProvider.bodyObjects.Add(droneBody);
            CharacterBody body = droneBody.GetComponent<CharacterBody>();
            body.baseNameToken = "CHILL_DRONE_NAME";
            body.baseRegen *= 3;
            body.baseDamage = 3;
            body.baseCrit = 0f;
            body.baseArmor *= 1.3f;
            body.levelRegen *= 3;
            body.levelDamage = 1;
            body.levelCrit = 0f;
            body.levelArmor *= 1.3f;
            body.portraitIcon = assetBundle.LoadAsset<Texture>("Assets/texChillDrone.png");
            ModelLocator bodyModelLocator = droneBody.GetComponent<ModelLocator>();
            GameObject bodyModelTransformObject = bodyModelLocator.modelTransform.gameObject;
            CharacterModel bodyModel = bodyModelTransformObject.GetComponent<CharacterModel>();
            bodyModel.baseRendererInfos[0].defaultMaterial = assetBundle.LoadAsset<Material>("Assets/matChillDrone.mat");
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
            purchaseInteraction.contextToken = "CHILL_DRONE_CONTEXT";
            purchaseInteraction.displayNameToken = "CHILL_DRONE_INTERACTABLE_NAME";
            GenericDisplayNameProvider nameProvider = brokenObject.GetComponent<GenericDisplayNameProvider>();
            nameProvider.displayToken = "CHILL_DRONE_NAME";
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
            InteractableActions += DirectorAPI_InteractableActions;
        }

        private void DirectorAPI_InteractableActions(List<DirectorCardHolder> arg1, StageInfo arg2)
        {
#if DEBUG
            arg1.ConditionalAdd(iDirectorCardHolder, card => iDirectorCardHolder == card);
#else
            if (arg2.CheckStage(DirectorAPI.Stage.RallypointDelta))
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