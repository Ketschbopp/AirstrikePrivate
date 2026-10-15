using System.Collections.Generic;
using Verse;

namespace AirstrikeMod
{
    public class CompProperties_AirstrikeStrafingRun : CompProperties_AirstrikeBase
    {
        /// <summary>
        /// Cast sound played at each fired cell. 
        /// </summary>
        public SoundDef fireSound;

        /// <summary>
        /// Targeting rectangle's length along the flight axis.
        /// </summary>
        public int runLength = 40;

        /// <summary>
        /// Targeting rectangle's width perpendicular to the flight axis. 
        /// </summary>
        public int runWidth = 3;

        /// <summary>
        /// How far ahead of the plane (in cells, along the flight axis) firing happens.
        /// </summary>
        public int leadCells = 8;

        /// <summary>
        /// Vanilla bullet projectile to spawn at each cell. Defaults via XML.
        /// </summary>
        public ThingDef projectileDef;

        /// <summary>
        /// Cargo item consumed to load rounds for the strafing run.
        /// </summary>
        public ThingDef ammoDef;

        /// <summary>
        /// Units of <c>ammoDef</c> consumed per round fired.
        /// </summary>
        public int ammoPerRound = 1;

        /// <summary>
        /// Bullets spawned per consumed round (per cell).
        /// </summary>
        public int bulletsPerRound = 4;

        /// <summary>
        /// Half-extent of the random target offset applied to each bullet within a round, in cells.
        /// </summary>
        public int spreadCells = 1;

        /// <summary>
        /// Cells between each bullet's spawn point and its target cell, along the flight axis.
        /// </summary>
        public int fireOriginOffset = 3;

        /// <summary>
        /// Label of the default weapon formed by the fields above (shown in the Payload tab
        /// when <see cref="weapons"/> offers alternatives).
        /// </summary>
        public string defaultWeaponLabel = "cannon";

        /// <summary>Combat Extended ammo set for the default weapon (see StrafingWeapon.ammoSet).</summary>
        public string ammoSet;

        /// <summary>
        /// Alternative strafing weapons the player can switch to. Unset fields inherit
        /// the top-level values.
        /// </summary>
        public List<StrafingWeapon> weapons;

        [Unsaved] private List<StrafingWeapon> _resolved;

        /// <summary>Default weapon first, then <see cref="weapons"/>, fully resolved.</summary>
        public List<StrafingWeapon> AllWeapons
        {
            get
            {
                if (_resolved != null) return _resolved;
                var list = new List<StrafingWeapon>
                {
                    new StrafingWeapon { label = defaultWeaponLabel, ammoSet = ammoSet }.ResolvedAgainst(this),
                };
                if (weapons != null)
                    foreach (var w in weapons)
                        if (w != null) list.Add(w.ResolvedAgainst(this));

                // Combat Extended: one choice per ammo type of the weapon's ammo set.
                _resolved = new List<StrafingWeapon>();
                foreach (var w in list)
                {
                    var links = string.IsNullOrEmpty(w.ammoSet) ? null : CECompat.AmmoLinks(w.ammoSet);
                    if (links == null || links.Count == 0)
                    {
                        _resolved.Add(w);
                        continue;
                    }
                    foreach (var link in links)
                    {
                        var v = w.Clone();
                        v.label = $"{w.LabelCap} ({link.label})";
                        v.projectileDef = link.projectile;
                        // Ammo system off in CE settings: strafing is free, like CE guns.
                        v.ammoDef = CECompat.AmmoSystemEnabled ? link.ammo : null;
                        v.ammoPerRound = System.Math.Max(1, w.bulletsPerRound) * link.consumedPerShot;
                        _resolved.Add(v);
                    }
                }
                return _resolved;
            }
        }

        public CompProperties_AirstrikeStrafingRun()
        {
            compClass = typeof(CompAirstrikeStrafingRun);
        }
    }
}
