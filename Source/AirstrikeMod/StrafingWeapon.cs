using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// One selectable weapon for a strafing run (e.g. cannon rounds or missiles). The
    /// strafing comp's own top-level fields form the first (default) weapon; extra
    /// entries come from <see cref="CompProperties_AirstrikeStrafingRun.weapons"/>.
    /// Negative / null fields inherit the comp's top-level value.
    /// </summary>
    public class StrafingWeapon
    {
        public string label;
        public ThingDef projectileDef;
        public ThingDef ammoDef;
        public int ammoPerRound = -1;
        public int bulletsPerRound = -1;
        public int spreadCells = -1;
        public int runLength = -1;
        public int runWidth = -1;
        public int leadCells = -1;
        public int fireOriginOffset = -1;
        public SoundDef fireSound;

        /// <summary>
        /// Combat Extended only: an AmmoSetDef defName (e.g. AmmoSet_20x102mmNATO). With CE
        /// active this weapon expands into one choice per ammo type in the set (AP, HE,
        /// Sabot, ...), reading the set at runtime so CE's generic-ammo option is honoured.
        /// Without CE it is ignored and projectileDef/ammoDef are used. Not inherited by
        /// entries in <c>weapons</c>.
        /// </summary>
        public string ammoSet;

        public string LabelCap => (label ?? projectileDef?.label ?? "?").CapitalizeFirst();

        /// <summary>Stable save key: survives the weapon list changing (e.g. CE toggled).</summary>
        public string Key => (label ?? "") + "|" + (ammoDef?.defName ?? projectileDef?.defName ?? "");

        internal StrafingWeapon ResolvedAgainst(CompProperties_AirstrikeStrafingRun p)
        {
            return new StrafingWeapon
            {
                label = label,
                projectileDef = projectileDef ?? p.projectileDef,
                ammoDef = ammoDef ?? p.ammoDef,
                ammoPerRound = ammoPerRound >= 0 ? ammoPerRound : p.ammoPerRound,
                bulletsPerRound = bulletsPerRound >= 0 ? bulletsPerRound : p.bulletsPerRound,
                spreadCells = spreadCells >= 0 ? spreadCells : p.spreadCells,
                runLength = runLength >= 0 ? runLength : p.runLength,
                runWidth = runWidth >= 0 ? runWidth : p.runWidth,
                leadCells = leadCells >= 0 ? leadCells : p.leadCells,
                fireOriginOffset = fireOriginOffset >= 0 ? fireOriginOffset : p.fireOriginOffset,
                fireSound = fireSound ?? p.fireSound,
                ammoSet = ammoSet,
            };
        }

        internal StrafingWeapon Clone() => (StrafingWeapon)MemberwiseClone();
    }
}
