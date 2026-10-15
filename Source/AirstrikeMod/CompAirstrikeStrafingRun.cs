using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace AirstrikeMod
{
    public class CompAirstrikeStrafingRun : CompAirstrikeBase
    {
        public CompProperties_AirstrikeStrafingRun Props => (CompProperties_AirstrikeStrafingRun)props;

        private static Texture2D _icon;
        private static Texture2D Icon =>
            _icon ??= ContentFinder<Texture2D>.Get("UI/ButtonStrafe", reportFailure: false)
                      ?? BaseContent.BadTex;

        private Action _startDelegate;

        private int selectedWeaponIndex;

        public override bool RequiresOrdinance => false;

        public List<StrafingWeapon> Weapons => Props.AllWeapons;
        public bool HasWeaponChoice => Weapons.Count > 1;

        public int SelectedWeaponIndex
        {
            get => Mathf.Clamp(selectedWeaponIndex, 0, Weapons.Count - 1);
            set => selectedWeaponIndex = Mathf.Clamp(value, 0, Weapons.Count - 1);
        }

        public StrafingWeapon Weapon => Weapons[SelectedWeaponIndex];

        private int RoundCount => Math.Max(0, Weapon.runLength);
        public int AmmoCountFor(StrafingWeapon w) =>
            Math.Max(0, w.runLength) * Math.Max(1, w.ammoPerRound);
        private int AmmoCount => AmmoCountFor(Weapon);

        protected override int RequiredShells => 0;
        protected override OrdinancePattern Pattern => OrdinancePattern.Strafing;

        protected override Gizmo BuildStrikeGizmo()
        {
            var w = Weapon;
            var ammoDef = w.ammoDef;
            var ammoLabel = ammoDef != null ? ammoDef.label : "ammo";
            return BuildLaunchGizmo(
                label: HasWeaponChoice
                    ? "ROCKET_StrafingRunWeapon".Translate(w.LabelCap).Resolve()
                    : "ROCKET_StrafingRun".Translate().Resolve(),
                desc: "ROCKET_StrafingRunDesc".Translate(RoundCount, AmmoCount, ammoLabel, w.LabelCap),
                topIcon: Icon,
                requiredShells: 0,
                onClick: _startDelegate ??= () => PickDestinationMap(StartTargeting),
                ammoOverrideDef: ammoDef,
                ammoOverrideCount: AmmoCount,
                iconUnderlayOverride: ammoDef?.uiIcon);
        }

        protected override void StartTargeting(Map destMap)
        {
            if (Find.Targeter.IsTargeting || LandingTargeter.Instance.IsTargeting
                || BombingRunTargeter.Instance.IsTargeting
                || StrafingRunTargeter.Instance.IsTargeting)
                return;
            var w = Weapon;
            if (w.projectileDef == null)
            {
                Messages.Message("[Rockets.Airstrike] Strafing weapon has no projectileDef configured.",
                    MessageTypeDefOf.RejectInput, false);
                return;
            }

            var originalMap = Current.Game.CurrentMap;
            if (Current.Game.CurrentMap != destMap)
                Current.Game.CurrentMap = destMap;

            var cursorIcon = w.ammoDef?.uiIcon ?? Icon;
            SetTargetingCursor("ROCKET_SelectTargetStrafe".Translate());
            CursorLabel.ThirdLine = "ROCKET_HoldShiftMultiTarget".Translate();

            var payload = new StrafingPayload
            {
                projectileDef = w.projectileDef,
                leadCells = w.leadCells,
                ammoDef = w.ammoDef,
                ammoCount = AmmoCount,
                fireSound = w.fireSound,
                bulletsPerRound = Math.Max(1, w.bulletsPerRound),
                spreadCells = Math.Max(0, w.spreadCells),
                fireOriginOffset = Math.Max(1, w.fireOriginOffset),
                runWidth = Math.Max(1, w.runWidth),
            };

            var maxChain = w.ammoDef == null
                ? int.MaxValue
                : Mathf.Max(1, CountInCargo(w.ammoDef) / Mathf.Max(1, AmmoCount));

            StrafingRunTargeter.Instance.BeginTargeting(
                vehicle: Vehicle,
                map: destMap,
                runWidth: w.runWidth,
                runLength: w.runLength,
                maxChain: maxChain,
                action: segments =>
                {
                    CursorLabel.Clear();
                    LaunchStrike(destMap, segments, originalMap, payload);
                },
                targetValidator: t => t.Cell.InBounds(destMap)
                                      && !Ext_Vehicles.IsRoofRestricted(Vehicle.VehicleDef, t.Cell, destMap),
                actionWhenFinished: () =>
                {
                    CursorLabel.Clear();
                    RestoreCurrentMap(originalMap);
                },
                mouseAttachment: cursorIcon);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref selectedWeaponIndex, nameof(selectedWeaponIndex));
        }
    }
}
