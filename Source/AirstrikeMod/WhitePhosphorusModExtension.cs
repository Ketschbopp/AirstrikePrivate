using RimWorld;
using UnityEngine;
using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// White-phosphorus burst: a blinding white flash, then burning fragments that arc
    /// outward trailing thick white smoke (the "octopus" look of a WP airburst) and set
    /// fire where they land. Put it on the projectile def (airstrike / mortar path) and on
    /// the item def (cook-off path).
    /// </summary>
    public class WhitePhosphorusModExtension : DefModExtension
    {
        public int fragmentCount = 16;
        // Landing distance as a fraction of the explosion radius.
        public FloatRange rangeFactor = new FloatRange(0.55f, 1.35f);
        public IntRange flightTicks = new IntRange(26, 48);
        public float arcHeight = 2.4f;
        public float fireSize = 0.7f;
        // Small flame burst where each fragment lands (0 = fire only).
        public int fragmentDamage = 8;
        public ThingDef fragmentDef;
    }

    internal static class WhitePhosphorusHelper
    {
        private static ThingDef _defaultFragment;
        private static ThingDef DefaultFragment =>
            _defaultFragment ??= DefDatabase<ThingDef>.GetNamedSilentFail("ROCKET_WPFragment");

        public static void Trigger(Def source, IntVec3 center, Map map, float radius, Thing instigator)
        {
            var ext = source?.GetModExtension<WhitePhosphorusModExtension>();
            if (ext == null || map == null || !center.InBounds(map)) return;
            var fragDef = ext.fragmentDef ?? DefaultFragment;

            var c = center.ToVector3Shifted();
            // Blinding white flash + glow at the burst point.
            var flash = FleckMaker.GetDataStatic(c, map, FleckDefOf.ExplosionFlash, Mathf.Max(6f, radius * 2.2f));
            flash.instanceColor = Color.white;
            map.flecks.CreateFleck(flash);
            for (var i = 0; i < 6; i++)
                WhiteSmoke(c + Rand.InsideUnitCircleVec3 * 1.2f, map, Rand.Range(2.2f, 3.4f), Rand.Range(0f, 360f), Rand.Range(0.2f, 0.6f));

            if (fragDef == null) return;
            var r = Mathf.Max(2f, radius);
            for (var i = 0; i < ext.fragmentCount; i++)
            {
                var angle = (i + Rand.Range(-0.35f, 0.35f)) / ext.fragmentCount * 360f;
                var dist = r * ext.rangeFactor.RandomInRange;
                var target = c + Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * dist;
                var cell = target.ToIntVec3();
                if (!cell.InBounds(map)) cell = cell.ClampInsideMap(map);

                var frag = (WhitePhosphorusFragment)ThingMaker.MakeThing(fragDef);
                frag.start = c;
                frag.end = cell.ToVector3Shifted() + new Vector3(Rand.Range(-0.4f, 0.4f), 0f, Rand.Range(-0.4f, 0.4f));
                frag.duration = ext.flightTicks.RandomInRange;
                frag.arcHeight = ext.arcHeight * Rand.Range(0.7f, 1.3f);
                frag.fireSize = ext.fireSize;
                frag.damage = ext.fragmentDamage;
                frag.instigator = instigator;
                GenSpawn.Spawn(frag, center, map);
            }
        }

        public static void WhiteSmoke(Vector3 loc, Map map, float size, float angle, float speed)
        {
            if (!loc.ShouldSpawnMotesAt(map)) return;
            var data = FleckMaker.GetDataStatic(loc, map, FleckDefOf.Smoke, size);
            data.instanceColor = new Color(1f, 1f, 1f, 0.9f);
            data.rotationRate = Rand.Range(-30f, 30f);
            data.velocityAngle = angle;
            data.velocitySpeed = speed;
            map.flecks.CreateFleck(data);
        }
    }

    /// <summary>
    /// One burning WP fragment: flies a ballistic arc from the burst, laying a dense white
    /// smoke trail, and ignites the cell it lands on.
    /// </summary>
    public class WhitePhosphorusFragment : Thing
    {
        public Vector3 start;
        public Vector3 end;
        public int duration = 36;
        public float arcHeight = 2.4f;
        public float fireSize = 0.7f;
        public int damage = 8;
        public Thing instigator;
        private int _ticks;

        private float T => Mathf.Clamp01((float)_ticks / Mathf.Max(1, duration));

        private Vector3 GroundPos => Vector3.Lerp(start, end, T);

        public override Vector3 DrawPos
        {
            get
            {
                var p = GroundPos;
                p.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                p.z += Mathf.Sin(T * Mathf.PI) * arcHeight;
                return p;
            }
        }

        protected override void Tick()
        {
            _ticks++;
            if (!Spawned) return;
            var map = Map;
            var pos = DrawPos;
            // Dense white trail drifting slightly upward, plus the burning glow.
            WhitePhosphorusHelper.WhiteSmoke(pos, map, Rand.Range(0.7f, 1.1f), Rand.Range(-20f, 20f), Rand.Range(0.05f, 0.25f));
            if (_ticks % 2 == 0)
                FleckMaker.ThrowFireGlow(pos, map, 0.45f);
            if (_ticks % 4 == 0)
                FleckMaker.ThrowMicroSparks(pos, map);

            if (_ticks >= duration) Land(map);
        }

        private void Land(Map map)
        {
            var cell = end.ToIntVec3();
            if (cell.InBounds(map))
            {
                FireUtility.TryStartFireIn(cell, map, fireSize, instigator);
                if (damage > 0)
                    GenExplosion.DoExplosion(cell, map, 0.9f, DamageDefOf.Flame, instigator,
                        damAmount: damage, explosionSound: null, doSoundEffects: false);
                for (var i = 0; i < 2; i++)
                    WhitePhosphorusHelper.WhiteSmoke(end, map, Rand.Range(1.2f, 1.8f), Rand.Range(0f, 360f), Rand.Range(0.1f, 0.3f));
            }
            Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            // Bright burning core.
            var mat = MaterialPool.MatFrom("Things/Projectile/ROCKET_WPFragment", ShaderDatabase.MoteGlow);
            var m = Matrix4x4.TRS(DrawPos, Quaternion.identity, new Vector3(0.7f, 1f, 0.7f));
            Graphics.DrawMesh(MeshPool.plane10, m, mat, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref start, nameof(start));
            Scribe_Values.Look(ref end, nameof(end));
            Scribe_Values.Look(ref duration, nameof(duration), 36);
            Scribe_Values.Look(ref arcHeight, nameof(arcHeight), 2.4f);
            Scribe_Values.Look(ref fireSize, nameof(fireSize), 0.7f);
            Scribe_Values.Look(ref damage, nameof(damage), 8);
            Scribe_Values.Look(ref _ticks, nameof(_ticks));
            Scribe_References.Look(ref instigator, nameof(instigator));
        }
    }
}
