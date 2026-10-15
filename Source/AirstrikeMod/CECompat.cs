using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// Optional Combat Extended integration, done entirely through reflection so this
    /// assembly has no compile-time dependency on CE and runs unchanged without it.
    /// With CE active:
    /// - CE mortar shells (AmmoDefs) resolve to their <c>detonateProjectile</c>;
    /// - bombs explode through <c>GenExplosionCE</c> (CE armour/falloff) and throw CE
    ///   shrapnel from any <c>CompProperties_Fragments</c> on the projectile def;
    /// - strafing runs launch CE projectiles (<c>ProjectileCE</c>) via CE's own launcher.
    /// Member names verified against CombatExtended-Continued (CE 16.7).
    /// </summary>
    public static class CECompat
    {
        public const string PackageId = "CETeam.CombatExtended";

        private static bool _init;
        private static bool _active;

        private static Type _projectileCE;
        private static FieldInfo _detonateProjectile;     // AmmoDef.detonateProjectile
        private static MethodInfo _doExplosionCE;          // GenExplosionCE.DoExplosion
        private static ParameterInfo[] _doExplosionParams;
        private static MethodInfo _explosionArmorPen;      // AmmoUtility.GetExplosionArmorPenetration(ProjectileProperties)
        private static Type _fragmentsProps;               // CompProperties_Fragments
        private static MethodInfo _fragmentsThrow;         // CompFragments.Throw(Vector3, Map, Thing, float)
        private static MethodInfo _launchCE;               // CE_Utility.LaunchProjectileCE(ThingDef, Vector2, LocalTargetInfo, Thing, float x4)
        private static readonly HashSet<string> _warned = new();

        public static bool Active
        {
            get
            {
                if (!_init) Init();
                return _active;
            }
        }

        private static void Init()
        {
            _init = true;
            if (!ModsConfig.IsActive(PackageId)) return;
            try
            {
                _projectileCE = AccessTools.TypeByName("CombatExtended.ProjectileCE");
                var ammoDef = AccessTools.TypeByName("CombatExtended.AmmoDef");
                _detonateProjectile = ammoDef != null ? AccessTools.Field(ammoDef, "detonateProjectile") : null;

                var genExplosionCE = AccessTools.TypeByName("CombatExtended.GenExplosionCE");
                _doExplosionCE = genExplosionCE != null ? AccessTools.Method(genExplosionCE, "DoExplosion") : null;
                _doExplosionParams = _doExplosionCE?.GetParameters();

                var ammoUtility = AccessTools.TypeByName("CombatExtended.AmmoUtility");
                if (ammoUtility != null)
                    _explosionArmorPen = AccessTools.Method(ammoUtility, "GetExplosionArmorPenetration",
                        new[] { typeof(ProjectileProperties) });

                _fragmentsProps = AccessTools.TypeByName("CombatExtended.CompProperties_Fragments");
                var compFragments = AccessTools.TypeByName("CombatExtended.CompFragments");
                _fragmentsThrow = compFragments != null ? AccessTools.Method(compFragments, "Throw") : null;

                var ceUtility = AccessTools.TypeByName("CombatExtended.CE_Utility");
                if (ceUtility != null)
                    _launchCE = AccessTools.Method(ceUtility, "LaunchProjectileCE", new[]
                    {
                        typeof(ThingDef), typeof(Vector2), typeof(LocalTargetInfo), typeof(Thing),
                        typeof(float), typeof(float), typeof(float), typeof(float),
                    });

                _active = _projectileCE != null && _doExplosionCE != null;
                if (_active)
                    Log.Message("[Rockets.Airstrike] Combat Extended detected: using CE explosions, shrapnel and projectiles.");
                else
                    Log.Warning("[Rockets.Airstrike] Combat Extended is active but its API was not found; using vanilla behaviour.");
            }
            catch (Exception ex)
            {
                _active = false;
                Log.Warning($"[Rockets.Airstrike] Combat Extended integration disabled: {ex}");
            }
        }

        private static void WarnOnce(string key, string msg)
        {
            if (_warned.Add(key)) Log.Warning("[Rockets.Airstrike] " + msg);
        }

        // ------------------------------------------------------------ ordinance

        /// <summary>
        /// The projectile a dropped ordinance item detonates as: vanilla
        /// <c>projectileWhenLoaded</c>, or CE's <c>AmmoDef.detonateProjectile</c>.
        /// </summary>
        public static ThingDef ProjectileFor(ThingDef ordinance)
        {
            if (ordinance == null) return null;
            if (ordinance.projectileWhenLoaded != null) return ordinance.projectileWhenLoaded;
            if (Active && _detonateProjectile != null && _detonateProjectile.DeclaringType!.IsInstanceOfType(ordinance))
                return _detonateProjectile.GetValue(ordinance) as ThingDef;
            return null;
        }

        public static bool IsCEProjectile(ThingDef def) =>
            Active && def?.thingClass != null && _projectileCE.IsAssignableFrom(def.thingClass);

        // ------------------------------------------------------------ explosions

        /// <summary>Explode <paramref name="projectileDef"/> CE-style. False = not handled.</summary>
        public static bool TryExplode(ThingDef projectileDef, ProjectileProperties pp, IntVec3 pos, Map map,
            Thing instigator)
        {
            if (!Active || pp == null) return false;
            try
            {
                var args = new object[_doExplosionParams.Length];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = _doExplosionParams[i];
                    args[i] = p.HasDefaultValue ? p.DefaultValue
                        : p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
                }
                void Set(string name, object value)
                {
                    for (var i = 0; i < _doExplosionParams.Length; i++)
                        if (_doExplosionParams[i].Name == name) { args[i] = value; return; }
                }

                var armorPen = _explosionArmorPen != null
                    ? (float)_explosionArmorPen.Invoke(null, new object[] { pp })
                    : pp.GetDamageAmount((Thing)null) * 0.4f; // CE: damage x ExplosiveArmorPenetrationMultiplier

                Set("center", pos);
                Set("map", map);
                Set("radius", pp.explosionRadius);
                Set("damType", pp.damageDef ?? DamageDefOf.Bomb);
                Set("instigator", instigator);
                Set("damAmount", pp.GetDamageAmount((Thing)null));
                Set("armorPenetration", armorPen);
                Set("explosionSound", pp.soundExplode);
                Set("projectile", projectileDef);
                Set("postExplosionSpawnThingDef", pp.postExplosionSpawnThingDef);
                Set("postExplosionSpawnChance", pp.postExplosionSpawnChance);
                Set("postExplosionSpawnThingCount", pp.postExplosionSpawnThingCount);
                Set("postExplosionGasType", pp.postExplosionGasType);
                Set("applyDamageToExplosionCellsNeighbors", pp.applyDamageToExplosionCellsNeighbors);
                Set("preExplosionSpawnThingDef", pp.preExplosionSpawnThingDef);
                Set("preExplosionSpawnChance", pp.preExplosionSpawnChance);
                Set("preExplosionSpawnThingCount", pp.preExplosionSpawnThingCount);
                Set("chanceToStartFire", pp.explosionChanceToStartFire);
                Set("damageFalloff", pp.explosionDamageFalloff);
                Set("screenShakeFactor", pp.screenShakeFactor);
                Set("postExplosionSpawnSingleThingDef", pp.postExplosionSpawnSingleThingDef);
                Set("preExplosionSpawnSingleThingDef", pp.preExplosionSpawnSingleThingDef);

                _doExplosionCE.Invoke(null, args);
                ThrowFragments(projectileDef, pos, map, instigator);
                return true;
            }
            catch (Exception ex)
            {
                WarnOnce("explode", $"CE explosion failed, falling back to vanilla: {ex}");
                return false;
            }
        }

        private static void ThrowFragments(ThingDef projectileDef, IntVec3 pos, Map map, Thing instigator)
        {
            if (projectileDef?.comps == null || _fragmentsProps == null || _fragmentsThrow == null) return;
            foreach (var props in projectileDef.comps)
            {
                if (props == null || !_fragmentsProps.IsInstanceOfType(props)) continue;
                // A detached CompFragments: Throw only needs its props (no parent = ground burst).
                var comp = (ThingComp)Activator.CreateInstance(props.compClass);
                comp.props = props;
                _fragmentsThrow.Invoke(comp, new object[] { pos.ToVector3Shifted(), map, instigator, 1f });
            }
        }

        // ------------------------------------------------------------ projectiles

        /// <summary>
        /// Launch a CE projectile from <paramref name="origin"/> (with height
        /// <paramref name="shotHeight"/>) at <paramref name="target"/>. <paramref name="shooter"/>
        /// must be spawned on the map (CE spawns the projectile at its position).
        /// </summary>
        public static bool TryLaunch(ThingDef projectileDef, Vector3 origin, LocalTargetInfo target,
            Thing shooter, float shotHeight)
        {
            if (!IsCEProjectile(projectileDef) || _launchCE == null || shooter is not { Spawned: true }) return false;
            try
            {
                var props = projectileDef.projectile;
                var worker = AccessTools.Property(props.GetType(), "TrajectoryWorker")?.GetValue(props);
                var src = new Vector3(origin.x, shotHeight, origin.z);
                var dst = target.CenterVector3;
                dst.y = 0f;
                var speed = props.speed;
                float rotation, angle;
                if (worker != null)
                {
                    rotation = (float)AccessTools.Method(worker.GetType(), "ShotRotation")
                        .Invoke(worker, new object[] { props, src, dst });
                    angle = (float)AccessTools.Method(worker.GetType(), "ShotAngle")
                        .Invoke(worker, new object[] { props, src, dst, speed });
                }
                else
                {
                    // Flat-fire fallback: CE rotation is clockwise from +z.
                    var d = dst - src;
                    rotation = (Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg + 360f) % 360f;
                    angle = Mathf.Atan2(-shotHeight, new Vector2(d.x, d.z).magnitude);
                }
                _launchCE.Invoke(null, new object[]
                {
                    projectileDef, new Vector2(origin.x, origin.z), target, shooter,
                    angle, rotation, shotHeight, speed,
                });
                return true;
            }
            catch (Exception ex)
            {
                WarnOnce("launch:" + projectileDef.defName, $"CE launch of {projectileDef.defName} failed: {ex}");
                return false;
            }
        }
    }
}
