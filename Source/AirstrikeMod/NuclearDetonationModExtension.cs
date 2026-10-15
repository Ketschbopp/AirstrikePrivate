using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace AirstrikeMod
{
    /// <summary>
    /// Extra nuclear-detonation effects layered on a bomb's blast, modelled on
    /// Rimatomics' warhead detonation (NuclearStrike.GoBang): everything in a small core
    /// is vaporised, constructed/thin roofs are blown off out to a wider radius, and a
    /// mushroom cloud mote is spawned. All references to Rimatomics are by def name, so
    /// this assembly has no hard dependency on it.
    /// </summary>
    public class NuclearDetonationModExtension : DefModExtension
    {
        public float vaporizeRadius;
        public float roofStripRadius;
        public SoundDef detonationSound;
        public ThingDef cloudMote;
        public float cloudScale = 1f;
    }

    internal static class NuclearDetonationHelper
    {
        private static readonly List<Thing> _buf = new List<Thing>();

        public static void Trigger(Def source, IntVec3 center, Map map)
        {
            var ext = source?.GetModExtension<NuclearDetonationModExtension>();
            if (ext == null || map == null || !center.InBounds(map)) return;

            ext.detonationSound?.PlayOneShot(new TargetInfo(center, map));
            Find.TickManager.slower.SignalForceNormalSpeed();

            if (ext.roofStripRadius > 0f)
            {
                foreach (var c in GenRadial.RadialCellsAround(center, ext.roofStripRadius, true))
                {
                    if (!c.InBounds(map)) continue;
                    var roof = c.GetRoof(map);
                    if (roof != null && !roof.isThickRoof)
                        map.roofGrid.SetRoof(c, null);
                }
            }

            if (ext.vaporizeRadius > 0f)
                Vaporize(center, map, ext.vaporizeRadius);

            SpawnCloud(ext, center, map);
        }

        private static void Vaporize(IntVec3 center, Map map, float radius)
        {
            foreach (var c in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (!c.InBounds(map)) continue;
                _buf.Clear();
                _buf.AddRange(c.GetThingList(map));
                foreach (var t in _buf)
                {
                    if (t.Destroyed || !t.def.destroyable || t is Skyfaller
                        || t is Vehicles.VehicleSkyfaller || t is Vehicles.ProjectileSkyfaller) continue;
                    // Keep the attacking aircraft, falling bombs, effects and filth.
                    switch (t.def.category)
                    {
                        case ThingCategory.Mote:
                        case ThingCategory.Ethereal:
                        case ThingCategory.Projectile:
                        case ThingCategory.Filth:
                        case ThingCategory.Gas:
                            continue;
                    }
                    try
                    {
                        if (t is Pawn p)
                        {
                            if (!p.Dead)
                                p.Kill(new DamageInfo(DamageDefOf.Bomb, 9999f));
                            if (p.Corpse is { Destroyed: false } corpse)
                                corpse.Destroy();
                            else if (!p.Destroyed && p.Spawned)
                                p.Destroy();
                            continue;
                        }
                        if (t is IThingHolder holder && !t.def.IsCorpse)
                            holder.GetDirectlyHeldThings()?.ClearAndDestroyContents();
                        t.Destroy();
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning($"[Rockets.Airstrike] Nuclear core could not vaporise {t}: {ex}");
                    }
                }
            }
            _buf.Clear();
        }

        private static void SpawnCloud(NuclearDetonationModExtension ext, IntVec3 center, Map map)
        {
            if (ext.cloudMote == null) return;
            try
            {
                var mote = ThingMaker.MakeThing(ext.cloudMote);
                if (mote is Mote m)
                    m.exactPosition = center.ToVector3Shifted();
                // Rimatomics' SmokeThrown scales its cloud via AtomicScale.
                var field = AccessTools.Field(mote.GetType(), "AtomicScale");
                if (field != null) field.SetValue(mote, ext.cloudScale);
                else if (mote is Mote m2) m2.Scale = ext.cloudScale;
                GenSpawn.Spawn(mote, center, map);
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[Rockets.Airstrike] Could not spawn nuclear cloud: {ex}");
            }
        }
    }
}
