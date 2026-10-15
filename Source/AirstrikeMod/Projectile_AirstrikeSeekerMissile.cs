using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace AirstrikeMod
{
    /// <summary>Tuning for <see cref="Projectile_AirstrikeSeekerMissile"/>.</summary>
    public class SeekerMissileModExtension : DefModExtension
    {
        // Search radius around the aimed cell when the missile is fired.
        public float seekRadius = 18f;
        // Max heading change per tick, degrees: limits how hard the missile can turn.
        public float turnRateDegPerTick = 7f;
        public int retargetInterval = 3;
        // Within this distance the missile flies straight at the target (no turn limit).
        public float terminalDistance = 2.5f;
    }

    /// <summary>
    /// Air-to-ground missile that locks onto a hostile Vehicle Framework vehicle near
    /// its aim point and steers toward it in flight (turn-rate limited). Missiles spread
    /// over vehicles not yet targeted; with nothing to lock it flies like a normal missile.
    /// Inherits the flame, smoke trail and vehicle-damage weapon registration.
    /// </summary>
    public class Projectile_AirstrikeSeekerMissile : Projectile_AirstrikeMissile
    {
        private static readonly Dictionary<VehiclePawn, int> Locks = new();

        private VehiclePawn _lock;
        private bool _counted;

        private SeekerMissileModExtension Ext =>
            def.GetModExtension<SeekerMissileModExtension>() ?? _defaultExt;
        private static readonly SeekerMissileModExtension _defaultExt = new();

        public override void Launch(Thing launcher, Vector3 origin, LocalTargetInfo usedTarget,
            LocalTargetInfo intendedTarget, ProjectileHitFlags hitFlags, bool preventFriendlyFire = false,
            Thing equipment = null, ThingDef targetCoverDef = null)
        {
            base.Launch(launcher, origin, usedTarget, intendedTarget, hitFlags, preventFriendlyFire,
                equipment, targetCoverDef);
            var aim = intendedTarget.IsValid ? intendedTarget.Cell : destination.ToIntVec3();
            SetLock(FindTarget(aim));
        }

        private VehiclePawn FindTarget(IntVec3 near)
        {
            if (Map == null) return null;
            var radius = Ext.seekRadius;
            VehiclePawn best = null;
            var bestScore = float.MaxValue;
            var pawns = Map.mapPawns.AllPawnsSpawned;
            for (var i = 0; i < pawns.Count; i++)
            {
                if (pawns[i] is not VehiclePawn v || v.Dead || v.Destroyed || !v.Spawned) continue;
                if (!IsHostile(v)) continue;
                var dist = v.Position.DistanceTo(near);
                if (dist > radius) continue;
                // Spread salvos: each existing lock counts as 8 cells of extra distance.
                Locks.TryGetValue(v, out var n);
                var score = dist + n * 8f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = v;
                }
            }
            return best;
        }

        private bool IsHostile(VehiclePawn v)
        {
            var faction = launcher?.Faction ?? Faction.OfPlayer;
            return v.Faction != null && v.Faction.HostileTo(faction);
        }

        private void SetLock(VehiclePawn v)
        {
            ReleaseLock();
            _lock = v;
            if (v == null) return;
            Locks.TryGetValue(v, out var n);
            Locks[v] = n + 1;
            _counted = true;
            usedTarget = v;
            intendedTarget = v;
        }

        private void ReleaseLock()
        {
            if (_counted && _lock != null && Locks.TryGetValue(_lock, out var n))
            {
                if (n <= 1) Locks.Remove(_lock);
                else Locks[_lock] = n - 1;
            }
            _counted = false;
            _lock = null;
        }

        protected override void Tick()
        {
            if (Spawned && !landed && this.IsHashIntervalTick(Mathf.Max(1, Ext.retargetInterval)))
                Steer();
            base.Tick();
        }

        private void Steer()
        {
            if (_lock is not { Spawned: true, Dead: false } || _lock.Map != Map)
            {
                // Target gone: try to pick up another nearby, else keep flying straight.
                SetLock(FindTarget(ExactPosition.ToIntVec3()));
                if (_lock == null) return;
            }

            var cur = ExactPosition;
            var target = _lock.TrueCenter();
            target.y = cur.y;
            var toTarget = target - cur;
            var dist = toTarget.magnitude;
            if (dist < 0.05f) return;

            var heading = destination - origin;
            heading.y = 0f;
            Vector3 newDir;
            if (dist <= Ext.terminalDistance || heading.sqrMagnitude < 0.0001f)
            {
                newDir = toTarget / dist;
            }
            else
            {
                var maxRad = Ext.turnRateDegPerTick * Mathf.Max(1, Ext.retargetInterval) * Mathf.Deg2Rad;
                newDir = Vector3.RotateTowards(heading.normalized, toTarget / dist, maxRad, 0f).normalized;
            }

            // Re-seat the straight-line flight: from here, along the new heading, for the
            // remaining distance. Vanilla movement then interpolates from origin.
            origin = cur;
            destination = cur + newDir * dist;
            ticksToImpact = Mathf.Max(1, Mathf.CeilToInt(StartingTicksToImpact));
            if (dist <= Ext.terminalDistance) usedTarget = _lock;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            ReleaseLock();
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref _lock, "seekerLock");
            if (Scribe.mode == LoadSaveMode.PostLoadInit && _lock != null)
            {
                Locks.TryGetValue(_lock, out var n);
                Locks[_lock] = n + 1;
                _counted = true;
            }
        }
    }
}
