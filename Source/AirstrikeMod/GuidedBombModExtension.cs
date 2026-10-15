using Verse;

namespace AirstrikeMod
{
    /// <summary>
    /// Marks a bomb item as guided (JDAM-style tail kit). Scales the aircraft's
    /// skill-based scatter for this bomb only; 0 = always lands on the aimed cell.
    /// </summary>
    public class GuidedBombModExtension : DefModExtension
    {
        public float scatterMultiplier = 0f;
    }
}
