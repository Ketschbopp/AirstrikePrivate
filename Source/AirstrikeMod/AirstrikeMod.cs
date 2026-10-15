using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace AirstrikeMod
{
    public class AirstrikeMod : Mod
    {
        public static AirstrikeModSettings Settings;

        public AirstrikeMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<AirstrikeModSettings>();

            var harmony = new Harmony("Rockets.Airstrike");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public override string SettingsCategory() => "ROCKET_SettingsCategory".Translate();

        private enum SettingsTab { General, Debug }
        private static SettingsTab currentTab = SettingsTab.General;
        private static readonly List<TabRecord> tabBuf = new();

        public override void DoSettingsWindowContents(Rect inRect)
        {
            const float tabBarHeight = 32f;
            var contentRect = new Rect(inRect.x, inRect.y + tabBarHeight, inRect.width, inRect.height - tabBarHeight);

            tabBuf.Clear();
            tabBuf.Add(new TabRecord("ROCKET_TabGeneral".Translate(), () => currentTab = SettingsTab.General, currentTab == SettingsTab.General));
            tabBuf.Add(new TabRecord("ROCKET_TabDebug".Translate(), () => currentTab = SettingsTab.Debug, currentTab == SettingsTab.Debug));

            Widgets.DrawMenuSection(contentRect);
            TabDrawer.DrawTabs(contentRect, tabBuf);

            var inner = contentRect.ContractedBy(12f);
            switch (currentTab)
            {
                case SettingsTab.General: DrawGeneralTab(inner); break;
                case SettingsTab.Debug:   DrawDebugTab(inner); break;
            }
        }

        private static void DrawGeneralTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            Text.Font = GameFont.Small;
            var s = Settings;

            listing.Label("ROCKET_FuelScaleLabel".Translate((s.fuelScale * 100f).ToString("0")));
            s.fuelScale = listing.Slider(s.fuelScale, 0f, 1f);

            listing.Label("ROCKET_HoverFuelLabel".Translate((s.hoverFuelMultiplier * 100f).ToString("0")),
                tooltip: "ROCKET_HoverFuelDesc".Translate());
            s.hoverFuelMultiplier = listing.Slider(s.hoverFuelMultiplier, 0f, 3f);

            listing.CheckboxLabeled("ROCKET_StayAirborneDefaultLabel".Translate(), ref s.stayAirborneByDefault,
                "ROCKET_StayAirborneDefaultDesc".Translate());

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label("ROCKET_SettingsWeaponsHeader".Translate());
            Text.Font = GameFont.Small;

            listing.Label("ROCKET_BombDamageLabel".Translate((s.bombDamageMultiplier * 100f).ToString("0")),
                tooltip: "ROCKET_BombDamageDesc".Translate());
            s.bombDamageMultiplier = listing.Slider(s.bombDamageMultiplier, 0.25f, 3f);

            listing.Label("ROCKET_ScatterLabel".Translate((s.scatterMultiplier * 100f).ToString("0")),
                tooltip: "ROCKET_ScatterDesc".Translate());
            s.scatterMultiplier = listing.Slider(s.scatterMultiplier, 0f, 2f);

            listing.Label("ROCKET_StrafingAmmoLabel".Translate((s.strafingAmmoMultiplier * 100f).ToString("0")),
                tooltip: "ROCKET_StrafingAmmoDesc".Translate());
            s.strafingAmmoMultiplier = listing.Slider(s.strafingAmmoMultiplier, 0f, 2f);

            listing.CheckboxLabeled("ROCKET_AllowNuclearLabel".Translate(), ref s.allowNuclearWeapons,
                "ROCKET_AllowNuclearDesc".Translate());
            listing.CheckboxLabeled("ROCKET_WPLingeringLabel".Translate(), ref s.wpLingeringFire,
                "ROCKET_WPLingeringDesc".Translate());

            listing.Gap(12f);
            if (listing.ButtonText("ROCKET_ResetSettings".Translate()))
                s.ResetToDefaults();

            listing.End();
        }

        private static void DrawDebugTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);
            Text.Font = GameFont.Small;

            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            listing.Label("ROCKET_DebugTabBlurb".Translate());
            GUI.color = Color.white;
            listing.Gap(6f);

            listing.CheckboxLabeled(
                "ROCKET_DebugDrawFlightPathLabel".Translate(),
                ref Settings.debugDrawFlightPath,
                "ROCKET_DebugDrawFlightPathDesc".Translate());

            listing.End();
        }
    }

    public class AirstrikeModSettings : ModSettings
    {
        public float fuelScale = 1f;
        public bool debugDrawFlightPath;

        // Balance
        public float bombDamageMultiplier = 1f;
        public float scatterMultiplier = 1f;
        public float strafingAmmoMultiplier = 1f;
        public float hoverFuelMultiplier = 1f;
        public bool allowNuclearWeapons = true;
        public bool stayAirborneByDefault = true;
        public bool wpLingeringFire = true;

        public void ResetToDefaults()
        {
            fuelScale = 1f;
            bombDamageMultiplier = 1f;
            scatterMultiplier = 1f;
            strafingAmmoMultiplier = 1f;
            hoverFuelMultiplier = 1f;
            allowNuclearWeapons = true;
            stayAirborneByDefault = true;
            wpLingeringFire = true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref fuelScale, nameof(fuelScale), 1f);
            Scribe_Values.Look(ref debugDrawFlightPath, nameof(debugDrawFlightPath));
            Scribe_Values.Look(ref bombDamageMultiplier, nameof(bombDamageMultiplier), 1f);
            Scribe_Values.Look(ref scatterMultiplier, nameof(scatterMultiplier), 1f);
            Scribe_Values.Look(ref strafingAmmoMultiplier, nameof(strafingAmmoMultiplier), 1f);
            Scribe_Values.Look(ref hoverFuelMultiplier, nameof(hoverFuelMultiplier), 1f);
            Scribe_Values.Look(ref allowNuclearWeapons, nameof(allowNuclearWeapons), true);
            Scribe_Values.Look(ref stayAirborneByDefault, nameof(stayAirborneByDefault), true);
            Scribe_Values.Look(ref wpLingeringFire, nameof(wpLingeringFire), true);
        }
    }
}
