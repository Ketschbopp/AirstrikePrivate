using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// An IED made from one of the mod's bombs. Detonates on command (gizmo) with the
    /// bomb's own projectile, so every effect the bomb has from the air (nuke, white
    /// phosphorus, bunker buster, gas, secondaries, CE shrapnel) happens here too.
    /// Damage past <see cref="CompProperties_RemoteDetonator.wickHitPointsPercent"/>
    /// lights a short wick; destroying it outright sets it off at once.
    /// </summary>
    public class CompProperties_RemoteDetonator : CompProperties
    {
        public ThingDef bomb;
        public float wickHitPointsPercent = 0.5f;
        public IntRange wickTicks = new IntRange(30, 60);

        public CompProperties_RemoteDetonator() => compClass = typeof(CompRemoteDetonator);

        public ThingDef Projectile => CECompat.ProjectileFor(bomb);

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var e in base.ConfigErrors(parentDef)) yield return e;
            if (bomb == null) yield return "bomb is null";
            else if (Projectile?.projectile == null) yield return $"bomb {bomb.defName} has no projectileWhenLoaded";
        }
    }

    public class CompRemoteDetonator : ThingComp
    {
        private int wickLeft = -1;
        private bool detonated;

        public CompProperties_RemoteDetonator Props => (CompProperties_RemoteDetonator)props;
        private float Radius => Props.Projectile?.projectile?.explosionRadius ?? 0f;

        private bool Allowed => CompAirstrikeBase.OrdinanceAllowed(Props.bomb);

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra()) yield return g;
            if (parent.Faction != Faction.OfPlayer) yield break;
            var cmd = new Command_Action
            {
                defaultLabel = "ROCKET_IEDDetonate".Translate(),
                defaultDesc = "ROCKET_IEDDetonateDesc".Translate(Props.bomb.LabelCap),
                icon = Props.bomb.uiIcon,
                action = Detonate,
            };
            if (!Allowed) cmd.Disable("ROCKET_IEDNuclearDisabled".Translate());
            yield return cmd;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (wickLeft < 0 || !parent.Spawned) return;
            if (--wickLeft <= 0) Detonate();
        }

        // Lethal hit: go off now, like vanilla CompExplosive.
        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PostPreApplyDamage(ref dinfo, out absorbed);
            if (detonated || !parent.Spawned || !dinfo.Def.ExternalViolenceFor(parent)) return;
            if (dinfo.Amount >= parent.HitPoints)
            {
                Detonate();
                absorbed = parent.Destroyed;
            }
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (wickLeft >= 0 || detonated || !parent.Spawned || totalDamageDealt <= 0f
                || !dinfo.Def.ExternalViolenceFor(parent)) return;
            if (parent.HitPoints <= parent.MaxHitPoints * Props.wickHitPointsPercent)
                wickLeft = Props.wickTicks.RandomInRange;
        }

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            base.Notify_Killed(prevMap, dinfo);
            Explode(parent.Position, prevMap);
        }

        public void Detonate()
        {
            if (detonated || !parent.Spawned) return;
            var map = parent.Map;
            var pos = parent.Position;
            parent.Destroy(DestroyMode.Vanish);
            Explode(pos, map);
        }

        private void Explode(IntVec3 pos, Map map)
        {
            if (detonated || map == null) return;
            detonated = true;
            var proj = Props.Projectile;
            if (proj?.projectile == null) return;
            ExplosionFx.Trigger(proj, proj.projectile, pos, map, parent);
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (Radius > 0f) GenDraw.DrawRadiusRing(parent.Position, Radius);
        }

        public override string CompInspectStringExtra() =>
            wickLeft >= 0 ? "ROCKET_IEDWickLit".Translate().ToString() : null;

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref wickLeft, nameof(wickLeft), -1);
        }
    }

    /// <summary>Shows the IED's blast radius while placing it.</summary>
    public class PlaceWorker_ShowIEDRadius : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var props = def.GetCompProperties<CompProperties_RemoteDetonator>();
            var r = props?.Projectile?.projectile?.explosionRadius ?? 0f;
            if (r > 0f) GenDraw.DrawRadiusRing(center, r);
        }
    }
}
