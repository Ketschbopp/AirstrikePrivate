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
        private string selectedWeaponKey;

        public override bool RequiresOrdinance => false;

        public List<StrafingWeapon> Weapons => Props.AllWeapons;
        public bool HasWeaponChoice => Weapons.Count > 1;

        public int SelectedWeaponIndex
        {
            get
            {
                // Re-find the saved choice by key in case the list changed (CE on/off,
                // generic ammo toggled, mod update).
                if (selectedWeaponKey != null)
                {
                    var i = Weapons.FindIndex(w => w.Key == selectedWeaponKey);
                    if (i >= 0) selectedWeaponIndex = i;
                    selectedWeaponKey = null;
                }
                return Mathf.Clamp(selectedWeaponIndex, 0, Weapons.Count - 1);
            }
            set => selectedWeaponIndex = Mathf.Clamp(value, 0, Weapons.Count - 1);
        }

        public StrafingWeapon Weapon => Weapons[SelectedWeaponIndex];

        private int RoundCount => Math.Max(0, Weapon.runLength);
        public int AmmoCountFor(StrafingWeapon w) =>
            Mathf.CeilToInt(Math.Max(0, w.runLength) * Math.Max(1, w.ammoPerRound)
                            * (AirstrikeMod.Settings?.strafingAmmoMultiplier ?? 1f));
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

            if (destMap == null) return; // never switch the view to "no map" (world)
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

        public override IEnumerable<Gizmo> LoiterGizmos()
        {
            foreach (var g in base.LoiterGizmos())
                yield return g;
            if (!HasWeaponChoice) yield break;
            // The Payload tab isn't reachable while airborne; switch weapons here instead.
            var w = Weapon;
            yield return new Command_Action
            {
                defaultLabel = "ROCKET_SwitchWeapon".Translate(w.LabelCap),
                defaultDesc = "ROCKET_SwitchWeaponDesc".Translate(),
                icon = w.ammoDef?.uiIcon ?? Icon,
                action = () =>
                {
                    var options = new List<FloatMenuOption>();
                    for (var i = 0; i < Weapons.Count; i++)
                    {
                        var idx = i;
                        var wi = Weapons[i];
                        var have = wi.ammoDef != null ? CountInCargo(wi.ammoDef) : 0;
                        var label = wi.ammoDef != null
                            ? "ROCKET_StrafingWeaponRow".Translate(wi.LabelCap, have, AmmoCountFor(wi),
                                wi.ammoDef.label).Resolve()
                            : wi.LabelCap;
                        options.Add(new FloatMenuOption(label, () => SelectedWeaponIndex = idx,
                            wi.ammoDef?.uiIcon, Color.white));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                },
            };
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref selectedWeaponIndex, nameof(selectedWeaponIndex));
            if (Scribe.mode == LoadSaveMode.Saving)
                selectedWeaponKey = Weapons[SelectedWeaponIndex].Key;
            Scribe_Values.Look(ref selectedWeaponKey, nameof(selectedWeaponKey));
        }
    }
}
