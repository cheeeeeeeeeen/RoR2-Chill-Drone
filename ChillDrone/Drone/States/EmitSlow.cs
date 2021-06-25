using Chen.GradiusMod;
using Chen.GradiusMod.Items.GradiusOption;
using Chen.GradiusMod.Items.GradiusOption.Components;
using EntityStates;
using EntityStates.EngiTurret.EngiTurretWeapon;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Chen.ChillDrone.Drone.States
{
    internal class EmitSlow : BaseState
    {
        private static FireBeam fireBeamInstance { get => new FireBeam(); }
        private static GameObject laserPrefab { get => fireBeamInstance.laserPrefab; }
        private static string attackSoundString { get => fireBeamInstance.attackSoundString; }
        private static float fireFrequency { get => fireBeamInstance.fireFrequency; }
        private static GameObject effectPrefab { get => fireBeamInstance.effectPrefab; }
        private static float minSpread { get => fireBeamInstance.minSpread; }
        private static float maxSpread { get => fireBeamInstance.maxSpread; }
        private static float force { get => fireBeamInstance.force; }
        private static GameObject hitEffectPrefab { get => fireBeamInstance.hitEffectPrefab; }

        public static readonly float detectionDistance = 20f;

        private static readonly float angularDetection = 60f;
        private static readonly string muzzleString = "Muzzle";
        private static readonly float maxDistance = detectionDistance * 2f;
        private static readonly float duration = 5f;

        private float stopwatch = 0f;
        private float fireTimer = 0f;
        private Transform modelTransform;
        private BullseyeSearch enemyFinder;
        private Ray aimRay;
        private List<HurtBox> results = new List<HurtBox>();
        private readonly List<GameObject> laserEffectInstances = new List<GameObject>();
        private readonly List<Transform> laserEffectInstanceEndTransforms = new List<Transform>();
        private Transform muzzleTransform;

        private void InitializeEnemyFinder()
        {
            enemyFinder = new BullseyeSearch
            {
                viewer = characterBody,
                maxDistanceFilter = detectionDistance,
                maxAngleFilter = angularDetection,
                searchOrigin = aimRay.origin,
                searchDirection = aimRay.direction,
                filterByLoS = true,
                sortMode = BullseyeSearch.SortMode.DistanceAndAngle,
                teamMaskFilter = TeamMask.allButNeutral
            };
            if (teamComponent) enemyFinder.teamMaskFilter.RemoveTeam(teamComponent.teamIndex);
            enemyFinder.RefreshCandidates();
            results = enemyFinder.GetResults().ToList();
        }

        private void FireBullet(Ray laserRay)
        {
            if (effectPrefab) EffectManager.SimpleMuzzleFlash(effectPrefab, gameObject, muzzleString, false);
            if (isAuthority)
            {
                BulletAttack bulletAttack = new BulletAttack
                {
                    owner = gameObject,
                    weapon = gameObject,
                    origin = laserRay.origin,
                    aimVector = laserRay.direction,
                    minSpread = minSpread,
                    maxSpread = maxSpread,
                    bulletCount = 1U,
                    damage = damageStat,
                    procCoefficient = 1f / fireFrequency,
                    force = force,
                    muzzleName = muzzleString,
                    hitEffectPrefab = hitEffectPrefab,
                    isCrit = Util.CheckRoll(critStat, characterBody.master),
                    HitEffectNormal = false,
                    maxDistance = maxDistance,
                    radius = 0f
                };
                bulletAttack.AddModdedDamageType(ChillDrone.chillOnHit);
                bulletAttack.Fire();
            }
        }

        private void InitializeOptionDataStructure()
        {
            GradiusOption.instance.FireForAllOptions(characterBody, (option, behavior, _t, _d) =>
            {
                if (behavior.O.ContainsKey("laserEffectInstances"))
                {
                    List<GameObject> optionLaserInstances;
                    optionLaserInstances = behavior.O["laserEffectInstances"] as List<GameObject>;
                    optionLaserInstances.Clear();
                }
                else behavior.O["laserEffectInstances"] = new List<GameObject>();
                if (behavior.O.ContainsKey("laserEffectInstanceEndTransforms"))
                {
                    List<Transform> optionLaserEffectInstanceEndTransforms;
                    optionLaserEffectInstanceEndTransforms = behavior.O["laserEffectInstanceEndTransforms"] as List<Transform>;
                    optionLaserEffectInstanceEndTransforms.Clear();
                }
                else behavior.O["laserEffectInstanceEndTransforms"] = new List<Transform>();
            });
        }

        private void OptionsFire(Action<GameObject, OptionBehavior, GameObject, Vector3, List<GameObject>, List<Transform>> action)
        {
            GradiusOption.instance.FireForAllOptions(characterBody, (option, behavior, target, direction) =>
            {
                if (behavior.O.ContainsKey("laserEffectInstances") && behavior.O.ContainsKey("laserEffectInstanceEndTransforms"))
                {
                    List<GameObject> laserInstances = behavior.O["laserEffectInstances"] as List<GameObject>;
                    List<Transform> laserEndTransforms = behavior.O["laserEffectInstanceEndTransforms"] as List<Transform>;
                    action(option, behavior, target, direction, laserInstances, laserEndTransforms);
                }
            });
        }

        private void OptionFireBullet(Ray laserRay, GameObject option)
        {
            if (effectPrefab) option.MuzzleEffect(effectPrefab, false);
            if (isAuthority)
            {
                BulletAttack bulletAttack = new BulletAttack
                {
                    owner = gameObject,
                    weapon = option,
                    origin = laserRay.origin,
                    aimVector = laserRay.direction,
                    minSpread = minSpread,
                    maxSpread = maxSpread,
                    bulletCount = 1U,
                    damage = damageStat * GradiusOption.instance.damageMultiplier,
                    procCoefficient = 1f / fireFrequency,
                    force = force * GradiusOption.instance.damageMultiplier,
                    muzzleName = muzzleString,
                    hitEffectPrefab = hitEffectPrefab,
                    isCrit = Util.CheckRoll(critStat, characterBody.master),
                    HitEffectNormal = false,
                    maxDistance = maxDistance,
                    radius = 0f
                };
                bulletAttack.AddModdedDamageType(ChillDrone.chillOnHit);
                bulletAttack.Fire();
            }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            InitializeOptionDataStructure();
            Util.PlaySound(attackSoundString, gameObject);
            modelTransform = GetModelTransform();
            aimRay = GetAimRay();
            InitializeEnemyFinder();
            if (modelTransform)
            {
                ChildLocator childLocator = modelTransform.GetComponent<ChildLocator>();
                if (childLocator)
                {
                    muzzleTransform = childLocator.FindChild(muzzleString);
                    if (muzzleTransform && laserPrefab)
                    {
                        for (int i = 0; i < results.Count; i++)
                        {
                            GameObject laserEffect = UnityObject.Instantiate(laserPrefab, muzzleTransform.position, muzzleTransform.rotation);
                            laserEffect.transform.parent = muzzleTransform;
                            laserEffectInstances.Add(laserEffect);
                            laserEffectInstanceEndTransforms.Add(laserEffect.GetComponent<ChildLocator>().FindChild("LaserEnd"));
                            OptionsFire((option, _b, _t, _d, laserEffects, laserEndTransforms) =>
                            {
                                GameObject optionLaserEffect = UnityObject.Instantiate(laserPrefab, option.transform);
                                optionLaserEffect.transform.parent = option.transform;
                                laserEffects.Add(optionLaserEffect);
                                laserEndTransforms.Add(optionLaserEffect.GetComponent<ChildLocator>().FindChild("LaserEnd"));
                            });
                        }
                    }
                }
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            stopwatch += Time.fixedDeltaTime;
            fireTimer += Time.fixedDeltaTime;
            float num = fireFrequency * characterBody.attackSpeed;
            float num2 = 1f / num;
            Vector3 start = muzzleTransform.position;
            for (int i = 0; i < results.Count; i++)
            {
                HurtBox hurtBox = results[i];
                GameObject laserInstance = laserEffectInstances[i];
                if (hurtBox && hurtBox.healthComponent && hurtBox.healthComponent.alive)
                {
                    Transform laserEndTransform = laserEffectInstanceEndTransforms[i];
                    Vector3 end = hurtBox.transform.position;
                    Ray ray = new Ray(start, end - start);
                    if (fireTimer > num2) FireBullet(ray);
                    if (laserInstance && laserEndTransform)
                    {
                        Vector3 point = ray.GetPoint(maxDistance);
                        if (Util.CharacterRaycast(gameObject, ray, out RaycastHit raycastHit, maxDistance,
                                                  LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
                        {
                            point = raycastHit.point;
                        }
                        laserEndTransform.position = point;
                    }
                    OptionsFire((option, _b, _t, _d, laserEffects, laserEndTransforms) =>
                    {
                        Transform optionLaserEndTransform = laserEndTransforms[i];
                        Vector3 optionStart = option.transform.position;
                        Vector3 optionEnd = hurtBox.transform.position;
                        Ray optionRay = new Ray(optionStart, optionEnd - optionStart);
                        if (fireTimer > num2) OptionFireBullet(optionRay, option);
                        if (laserEffects[i] && optionLaserEndTransform)
                        {
                            Vector3 point = optionRay.GetPoint(maxDistance);
                            if (Physics.Raycast(optionStart, optionEnd - optionStart, out RaycastHit raycastHit, maxDistance,
                                                LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
                            {
                                point = raycastHit.point;
                            }
                            optionLaserEndTransform.position = point;
                        }
                    });
                }
                else if (laserInstance)
                {
                    OptionsFire((option, _b, _t, _d, laserEffects, laserEndTransforms) =>
                    {
                        Destroy(laserEffects[i]);
                        laserEffects.RemoveAt(i);
                        laserEndTransforms.RemoveAt(i);
                    });
                    Destroy(laserInstance);
                    results.RemoveAt(i);
                    laserEffectInstances.RemoveAt(i);
                    laserEffectInstanceEndTransforms.RemoveAt(i);
                    i--;
                }
            }
            if (fireTimer > num2) fireTimer = 0f;
            if (isAuthority && (stopwatch >= duration || results.Count == 0)) outer.SetNextStateToMain();
        }

        public override void OnExit()
        {
            foreach (var laserInstance in laserEffectInstances)
            {
                if (laserInstance) Destroy(laserInstance);
            }
            laserEffectInstances.Clear();
            laserEffectInstanceEndTransforms.Clear();
            OptionsFire((_o, _b, _t, _d, laserEffects, laserEndTransforms) =>
            {
                foreach (var laserEffect in laserEffects)
                {
                    if (laserEffect) Destroy(laserEffect);
                }
                laserEffects.Clear();
                laserEndTransforms.Clear();
            });
            results.Clear();
            base.OnExit();
        }
    }
}