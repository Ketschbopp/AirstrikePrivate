using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using SmashTools;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// A VTOL vehicle hovering on station after an in-place sortie, instead of landing.
    /// Selectable: offers the vehicle's strike gizmos (re-tasking flies the next run
    /// straight from the hover), a weapon switch, and landing orders. Burns fuel while
    /// airborne and lands itself when the tank runs dry.
    /// </summary>
    public class VehicleSkyfaller_Loiter : VehicleSkyfaller
    {
        private const int FUEL_INTERVAL = 60;
        private const float BOB_AMPLITUDE = 0.12f;
        private const int LANDING_SEARCH_RADIUS = 25;

        public IntVec3 returnCell;
        public Rot4 returnRot;
        public float visualAltitude = 2f;
        public int hoverLandingTicks = 90;
        public float fuelMultiplier = 1f;

        private int _ticks;
        private bool _landing;

        // VF's skyfaller container has no owner, so vehicle.ParentHolder can't find us.
        private static readonly Dictionary<VehiclePawn, VehicleSkyfaller_Loiter> ByVehicle = new();

        /// <summary>The spawned loiter currently holding <paramref name="v"/>, or null.</summary>
        public static VehicleSkyfaller_Loiter For(VehiclePawn v)
        {
            if (v == null || !ByVehicle.TryGetValue(v, out var l)) return null;
            if (l is { Spawned: true, Destroyed: false } && l.vehicle == v) return l;
            ByVehicle.Remove(v);
            return null;
        }

        internal static IEnumerable<VehicleSkyfaller_Loiter> AllSpawned => ByVehicle.Values;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (vehicle != null) ByVehicle[vehicle] = this;
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (vehicle != null && ByVehicle.TryGetValue(vehicle, out var l) && l == this)
                ByVehicle.Remove(vehicle);
            base.DeSpawn(mode);
        }
        private CompRotorSpinUp _rotorSpinUp;
        private bool _rotorSpinUpLookedUp;

        [UsedImplicitly]
        [Obsolete("Implemented for Xml Deserialization only. Use VehicleSkyfallerMaker instead.", error: true)]
        public VehicleSkyfaller_Loiter()
        {
        }

        public override string Label => vehicle?.LabelShortCap ?? base.Label;

        private float Altitude => visualAltitude + Mathf.Sin(_ticks * 0.05f) * BOB_AMPLITUDE;

        public override Vector3 DrawPos
        {
            get
            {
                var pos = RootPos;
                pos.y = AltitudeLayer.Skyfaller.AltitudeFor();
                pos.z += Altitude;
                return pos;
            }
        }

        /// <summary>True when <paramref name="mapPos"/> falls on the drawn aircraft.</summary>
        public bool SpriteContains(Vector3 mapPos)
        {
            var size = vehicle?.VehicleGraphic?.data?.drawSize ?? new Vector2(3f, 3f);
            // Generous but not the whole (mostly transparent) texture square.
            var half = Mathf.Max(1f, Mathf.Min(size.x, size.y) * 0.35f);
            var c = DrawPos;
            return Mathf.Abs(mapPos.x - c.x) <= half && Mathf.Abs(mapPos.z - c.z) <= half;
        }

        public void Notify_EnteredLoiter()
        {
            if (vehicle?.Faction != Faction.OfPlayer) return;
            if (Find.CurrentMap == Map)
            {
                Find.Selector.ClearSelection();
                Find.Selector.Select(this, playSound: false, forceDesignatorDeselect: false);
            }
            Messages.Message("ROCKET_LoiterStarted".Translate(vehicle.LabelShortCap),
                this, MessageTypeDefOf.NeutralEvent, historical: false);
        }

        protected override void Tick()
        {
            _ticks++;
            TickRotor();
            if (_landing) return;
            if (_ticks % FUEL_INTERVAL == 0) TickFuel();
        }

        private void TickFuel()
        {
            var fuel = vehicle.CompFueledTravel;
            if (fuel == null) return;
            fuel.ConsumeFuel(fuel.ConsumptionRatePerTickRaw * FUEL_INTERVAL * Mathf.Max(0f, fuelMultiplier)
                             * (AirstrikeMod.Settings?.hoverFuelMultiplier ?? 1f));
            if (!fuel.EmptyTank) return;

            Messages.Message("ROCKET_LoiterOutOfFuel".Translate(vehicle.LabelShortCap),
                this, MessageTypeDefOf.NegativeEvent);
            if (TryFindLandingCell(Position, returnRot, out var cell))
                LandAt(cell, returnRot);
            else
                LandAt(Position, returnRot); // nowhere better; VF resolves obstructions
        }

        private void TickRotor()
        {
            if (!_rotorSpinUpLookedUp)
            {
                _rotorSpinUp = vehicle?.GetComp<CompRotorSpinUp>();
                _rotorSpinUpLookedUp = true;
            }
            if (_rotorSpinUp == null) return;
            vehicle.DrawTracker?.overlayRenderer?.SetAcceleration(
                CompRotorSpinUp.ScaleForGameSpeed(_rotorSpinUp.TargetRate));
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            var pos = DrawPos;
            launchProtocolDrawPos = pos;
            vehicle.DrawAt(in pos, Rotation, 0f);
            DrawShadow();
        }

        private void DrawShadow()
        {
            if (cachedShadowMaterial == null && !string.IsNullOrEmpty(def.skyfaller.shadow))
                cachedShadowMaterial = MaterialPool.MatFrom(def.skyfaller.shadow,
                    ShaderDatabase.Transparent);
            if (cachedShadowMaterial == null) return;
            var size = vehicle.VehicleGraphic?.data?.drawSize ?? def.skyfaller.shadowSize;
            AirstrikeShadow.Draw(RootPos, size, Rotation.AsAngle, Altitude, cachedShadowMaterial);
        }

        // ---------------------------------------------------------------- orders

        /// <summary>Fly a new sortie straight from the hover.</summary>
        public void HandOff(ArrivalAction_BombMap arrival)
        {
            if (_landing || !Spawned) return;
            if (!arrival.SpawnBombingSkyfaller(Map, out var next)) return;
            Destroy();
            // Our DeSpawn released the vehicle's sustainer target; hand it to the new skyfaller.
            if (next != null) vehicle.SetSustainerTarget(next);
        }

        public void LandAt(IntVec3 cell, Rot4 rot)
        {
            if (_landing || !Spawned) return;
            _landing = true;
            var map = Map;
            // The landing patch restores this rotation once the vehicle touches down.
            BombingSpeedManager.MarkFast(vehicle, rot);
            var arriving = (VehicleSkyfaller_HoverLanding)
                VehicleSkyfallerMaker.MakeSkyfaller(AirstrikeDefOf.ROCKET_HoverLanding, vehicle);
            arriving.rotatePostLanding = rot;
            arriving.visualAltitude = visualAltitude;
            arriving.descentTicks = hoverLandingTicks;
            GenSpawn.Spawn(arriving, cell, map, rot);
            Destroy();
            vehicle.SetSustainerTarget(arriving);
            vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLanding].ExecuteEvents();
        }

        public bool CanLandAt(IntVec3 cell, Rot4 rot)
        {
            var map = Map;
            if (map == null || !cell.InBounds(map)) return false;
            if (MapHelper.NonStandableOrVehicleBlocked(vehicle, map, cell, rot)) return false;
            foreach (var c in vehicle.PawnOccupiedCells(cell, rot))
            {
                if (!c.InBounds(map)) return false;
                if (Ext_Vehicles.IsRoofRestricted(vehicle.VehicleDef, c, map)) return false;
            }
            return true;
        }

        private bool TryFindLandingCell(IntVec3 near, Rot4 rot, out IntVec3 cell)
        {
            foreach (var c in GenRadial.RadialCellsAround(near, LANDING_SEARCH_RADIUS, true))
            {
                if (CanLandAt(c, rot))
                {
                    cell = c;
                    return true;
                }
            }
            cell = IntVec3.Invalid;
            return false;
        }

        // --------------------------------------------------------------- gizmos

        private static Texture2D _landIcon;
        private static Texture2D LandIcon =>
            _landIcon ??= ContentFinder<Texture2D>.Get("UI/ButtonLocalFlight", reportFailure: false)
                          ?? BaseContent.BadTex;

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var g in base.GetGizmos())
                yield return g;
            if (vehicle == null || vehicle.Faction != Faction.OfPlayer || _landing)
                yield break;

            var comps = vehicle.AllComps;
            for (var i = 0; i < comps.Count; i++)
            {
                if (comps[i] is not CompAirstrikeBase strike) continue;
                foreach (var g in strike.LoiterGizmos())
                    yield return g;
            }

            yield return new Command_Action
            {
                defaultLabel = "ROCKET_LandAt".Translate(),
                defaultDesc = "ROCKET_LandAtDesc".Translate(),
                icon = LandIcon,
                action = BeginLandingTargeting,
            };

            var here = new Command_Action
            {
                defaultLabel = "ROCKET_LandHere".Translate(),
                defaultDesc = "ROCKET_LandHereDesc".Translate(),
                icon = LandIcon,
                action = () =>
                {
                    if (TryFindLandingCell(Position, Rotation, out var c)) LandAt(c, Rotation);
                },
            };
            if (!TryFindLandingCell(Position, Rotation, out _))
                here.Disable("ROCKET_NoLandingSpot".Translate());
            yield return here;

            var home = new Command_Action
            {
                defaultLabel = "ROCKET_ReturnToPad".Translate(),
                defaultDesc = "ROCKET_ReturnToPadDesc".Translate(),
                icon = LandIcon,
                action = () => LandAt(returnCell, returnRot),
            };
            if (!CanLandAt(returnCell, returnRot))
                home.Disable("ROCKET_NoLandingSpot".Translate());
            yield return home;
        }

        private void BeginLandingTargeting()
        {
            var map = Map;
            LandingTargeter.Instance.BeginTargeting(
                vehicle: vehicle,
                map: map,
                action: (target, rot) =>
                {
                    if (CanLandAt(target.Cell, rot)) LandAt(target.Cell, rot);
                    else Messages.Message("ROCKET_NoLandingSpot".Translate(), MessageTypeDefOf.RejectInput, false);
                },
                targetValidator: t => t.Cell.InBounds(map),
                allowRotating: true);
        }

        public override string GetInspectString()
        {
            var sb = new StringBuilder();
            sb.Append("ROCKET_LoiterInspect".Translate());
            var fuel = vehicle?.CompFueledTravel;
            if (fuel != null)
            {
                sb.AppendLine();
                sb.Append("ROCKET_LoiterFuel".Translate(fuel.Fuel.ToString("0"),
                    fuel.FuelCapacity.ToString("0")));
            }
            return sb.ToString();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref returnCell, nameof(returnCell));
            Scribe_Values.Look(ref returnRot, nameof(returnRot));
            Scribe_Values.Look(ref visualAltitude, nameof(visualAltitude), 2f);
            Scribe_Values.Look(ref hoverLandingTicks, nameof(hoverLandingTicks), 90);
            Scribe_Values.Look(ref fuelMultiplier, nameof(fuelMultiplier), 1f);
            Scribe_Values.Look(ref _ticks, nameof(_ticks));
        }
    }
}
