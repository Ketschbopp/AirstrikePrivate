using RimWorld;
using UnityEngine;
using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// Air-to-ground missile fired on strafing runs. Draws a flickering rocket-motor
    /// flame behind the body and leaves a smoke trail. Registers its own def as the
    /// damage "weapon", so a <c>Vehicles.VehicleDamageMultiplierDefModExtension</c> on the
    /// projectile def multiplies both the direct hit and the warhead blast against vehicles.
    /// </summary>
    public class Projectile_AirstrikeMissile : Projectile_Explosive
    {
        private static readonly Vector3 FlameScale = new Vector3(0.45f, 1f, 1.1f);
        private static Material _flameMat;
        private static Material FlameMat =>
            _flameMat ??= MaterialPool.MatFrom("Things/Projectile/ROCKET_MissileFlame", ShaderDatabase.MoteGlow);

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false,
            Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire,
                equipment, targetCoverDef);
            equipmentDef ??= def;
        }

        protected override void Tick()
        {
            base.Tick();
            if (!Spawned || Destroyed) return;
            var tail = DrawPos - ExactRotation * Vector3.forward * 0.6f;
            if (this.IsHashIntervalTick(2))
                FleckMaker.ThrowSmoke(tail, Map, Rand.Range(0.45f, 0.7f));
            if (this.IsHashIntervalTick(3))
                FleckMaker.ThrowFireGlow(tail, Map, 0.35f);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var rot = ExactRotation;
            var flicker = Find.TickManager.Paused ? 1f : Rand.Range(0.8f, 1.2f);
            var size = new Vector3(FlameScale.x, 1f, FlameScale.z * flicker);
            var pos = drawLoc - rot * Vector3.forward * (0.35f + size.z * 0.5f);
            pos.y = drawLoc.y - 0.01f;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(pos, rot, size), FlameMat, 0);
            base.DrawAt(drawLoc, flip);
        }
    }
}
