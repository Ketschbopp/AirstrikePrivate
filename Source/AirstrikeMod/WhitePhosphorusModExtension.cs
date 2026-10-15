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

        // Sticky particles: chance that a pawn inside stickyRadiusFactor * radius is
        // splattered and set alight at the burst (WP clings and keeps burning).
        public float stickyChance = 0f;
        public float stickyRadiusFactor = 0.6f;
        public float stickyFireSize = 0.6f;

        // Lingering burning phosphorus left where fragments land. Burns regardless of
        // terrain, re-ignites its cell and burns anyone standing in it.
        public float burnPatchChance = 0f;
        public IntRange burnPatchTicks = new IntRange(1200, 2400);
        public int burnPatchDamage = 6;
        public ThingDef burnPatchDef;
    }

    internal static class WhitePhosphorusHelper
    {
        private static ThingDef _defaultFragment;
        private static ThingDef DefaultFragment =>
            _defaultFragment ??= DefDatabase<ThingDef>.GetNamedSilentFail("ROCKET_WPFragment");

        private static ThingDef _defaultPatch;
        internal static ThingDef DefaultPatch =>
            _defaultPatch ??= DefDatabase<ThingDef>.GetNamedSilentFail("ROCKET_WPBurnPatch");

        private static readonly System.Collections.Generic.List<Thing> _tmp = new();

        /// <summary>Set burning WP on every pawn in <paramref name="cell"/> and burn them.</summary>
        internal static void Splatter(IntVec3 cell, Map map, float fireSize, int burnDamage, Thing instigator)
        {
            if (!cell.InBounds(map)) return;
            _tmp.Clear();
            _tmp.AddRange(cell.GetThingList(map));
            foreach (var t in _tmp)
            {
                if (t is not Pawn p || p.Dead || p.Destroyed) continue;
                if (burnDamage > 0)
                    p.TakeDamage(new DamageInfo(DamageDefOf.Burn, burnDamage, 0.5f, -1f, instigator));
                if (!p.Destroyed && p.Spawned)
                    p.TryAttachFire(fireSize, instigator);
            }
            _tmp.Clear();
        }

        internal static void SpawnPatch(WhitePhosphorusModExtension ext, IntVec3 cell, Map map, Thing instigator)
        {
            var def = ext.burnPatchDef ?? DefaultPatch;
            if (def == null || !cell.InBounds(map)) return;
            var patch = (WhitePhosphorusBurnPatch)ThingMaker.MakeThing(def);
            patch.lifetime = ext.burnPatchTicks.RandomInRange;
            patch.damage = ext.burnPatchDamage;
            patch.instigator = instigator;
            GenSpawn.Spawn(patch, cell, map);
        }

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

            if (ext.stickyChance > 0f)
            {
                foreach (var cell in GenRadial.RadialCellsAround(center, Mathf.Max(1f, radius * ext.stickyRadiusFactor), true))
                    if (cell.InBounds(map) && Rand.Chance(ext.stickyChance))
                        Splatter(cell, map, ext.stickyFireSize, ext.fragmentDamage, instigator);
            }

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
                frag.ext = ext;
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
        public WhitePhosphorusModExtension ext; // not saved: in-flight fragments are seconds long
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
                // Phosphorus sticks to whoever it hits and keeps burning.
                WhitePhosphorusHelper.Splatter(cell, map, Mathf.Max(0.4f, fireSize), damage, instigator);
                if (ext != null && ext.burnPatchChance > 0f && Rand.Chance(ext.burnPatchChance))
                    WhitePhosphorusHelper.SpawnPatch(ext, cell, map, instigator);
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

    /// <summary>
    /// Burning white phosphorus on the ground. It burns whatever the terrain (stone
    /// included), can't be put out, keeps re-igniting its cell and burns and sets alight
    /// anyone standing in it, until it burns out.
    /// </summary>
    public class WhitePhosphorusBurnPatch : Thing
    {
        private const int PULSE = 45;

        public int lifetime = 1800;
        public int damage = 6;
        public Thing instigator;
        private int _age;

        private float Strength => 1f - Mathf.Clamp01((float)_age / Mathf.Max(1, lifetime));

        protected override void Tick()
        {
            _age++;
            if (!Spawned) return;
            var map = Map;
            var pos = Position.ToVector3Shifted();
            if (_age % 6 == 0)
                WhitePhosphorusHelper.WhiteSmoke(pos + new Vector3(Rand.Range(-0.3f, 0.3f), 0f, Rand.Range(-0.3f, 0.3f)),
                    map, Rand.Range(0.6f, 1.2f) * (0.5f + Strength), Rand.Range(-25f, 25f), Rand.Range(0.15f, 0.35f));
            if (_age % 10 == 0)
                FleckMaker.ThrowFireGlow(pos, map, 0.5f + 0.4f * Strength);
            if (_age % PULSE == 0)
            {
                FireUtility.TryStartFireIn(Position, map, 0.4f, instigator);
                WhitePhosphorusHelper.Splatter(Position, map, 0.4f,
                    Mathf.Max(1, Mathf.RoundToInt(damage * (0.4f + 0.6f * Strength))), instigator);
            }
            if (_age >= lifetime) Destroy();
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var mat = MaterialPool.MatFrom("Things/Projectile/ROCKET_WPFragment", ShaderDatabase.MoteGlow);
            var flicker = Find.TickManager.Paused ? 1f : Rand.Range(0.85f, 1.15f);
            var size = (0.55f + 0.5f * Strength) * flicker;
            var p = Position.ToVector3Shifted();
            p.y = AltitudeLayer.MoteLow.AltitudeFor();
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(p, Quaternion.identity, new Vector3(size, 1f, size)), mat, 0);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lifetime, nameof(lifetime), 1800);
            Scribe_Values.Look(ref damage, nameof(damage), 6);
            Scribe_Values.Look(ref _age, nameof(_age));
            Scribe_References.Look(ref instigator, nameof(instigator));
        }
    }
}
