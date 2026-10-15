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

        public string LabelCap => (label ?? projectileDef?.label ?? "?").CapitalizeFirst();

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
            };
        }
    }
}
