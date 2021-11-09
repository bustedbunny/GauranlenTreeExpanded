using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using RimWorld;

namespace GauranlenTreeExpanded
{
    [StaticConstructorOnStartup]

    public class GauranlenTreeSettings : ModSettings
    {
        public static float MaxBonusDryad = 1.5f;
        public static float SpawnDays = 8f;
        public static bool TreeExtraction = true;
        public static float MaxMossRadius = 7.9f;
        public static float BuildingRadius = 7.9f;
        public static bool EnableDisconnectionRitual = true;
        public static int ConnectionTornTicks = 450000;
        public static int PruningDuration = 2500;
        public static float DurationDays = 5;
        public static float BaseMoodDebuff = 10;
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref MaxBonusDryad, "MaxBonusDryad", 1.5f);
            Scribe_Values.Look(ref SpawnDays, "SpawnDays", 8f);
            Scribe_Values.Look(ref TreeExtraction, "TreeExtraction", true);
            Scribe_Values.Look(ref MaxMossRadius, "MaxMossRadius", 7.9f);
            Scribe_Values.Look(ref BuildingRadius, "BuildingRadius", 7.9f);
            Scribe_Values.Look(ref EnableDisconnectionRitual, "EnableDisconnectionRitual", true);
            Scribe_Values.Look(ref ConnectionTornTicks, "ConnectionTornTicks", 450000);
            Scribe_Values.Look(ref PruningDuration, "PruningDuration", 2500);
            Scribe_Values.Look(ref DurationDays, "DurationDays", 5);
            Scribe_Values.Look(ref BaseMoodDebuff, "BaseMoodDebuff", 10);
        }
    }

    public class GauranlenTreeExpandedMod : Mod
    {

        public GauranlenTreeExpandedMod(ModContentPack content) : base(content)
        {
            GetSettings<GauranlenTreeSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard
            {
                verticalSpacing = -4f,
                maxOneColumn = false
            };
            listingStandard.Begin(inRect);

            listingStandard.Label("MaxBonusDryadLabel".Translate());
            listingStandard.Label("CurrentValueGauranlenTreeExpanded".Translate(Math.Round(GauranlenTreeSettings.MaxBonusDryad * 100, 2)));
            GauranlenTreeSettings.MaxBonusDryad = listingStandard.Slider(GauranlenTreeSettings.MaxBonusDryad, 0f, 3f);

            listingStandard.Label("DaysForDryadsToGrow".Translate());
            listingStandard.Label("CurrentValueDaysGauranlenTreeExpanded".Translate(Math.Round(GauranlenTreeSettings.SpawnDays, 2)));
            GauranlenTreeSettings.SpawnDays = listingStandard.Slider(GauranlenTreeSettings.SpawnDays, 0.5f, 12f);

            listingStandard.Label("MaxMossRadiusExpandedLabel".Translate());
            listingStandard.Label("CurrentValueRadiusGauranlenTreeExpanded".Translate(Math.Round(GauranlenTreeSettings.MaxMossRadius, 1)));
            GauranlenTreeSettings.MaxMossRadius = listingStandard.Slider(GauranlenTreeSettings.MaxMossRadius, 1f, 12f);

            listingStandard.Label("MaxBuildingRadiusExpandedLabel".Translate());
            listingStandard.Label("CurrentValueRadiusGauranlenTreeExpanded".Translate(Math.Round(GauranlenTreeSettings.BuildingRadius, 1)));
            GauranlenTreeSettings.BuildingRadius = listingStandard.Slider(GauranlenTreeSettings.BuildingRadius, 0f, 12f);

            listingStandard.Label("ConnectionTornTicksExpanded".Translate());
            listingStandard.Label("CurrentValueDaysGauranlenTreeExpanded".Translate((GauranlenTreeSettings.ConnectionTornTicks / 60000).ToString()));
            GauranlenTreeSettings.ConnectionTornTicks = (int)(60000 * listingStandard.Slider(GauranlenTreeSettings.ConnectionTornTicks / 60000, 1, 30));

            listingStandard.Label("PruningTicksExpanded".Translate());
            listingStandard.Label("CurrentValueHoursGauranlenTreeExpanded".Translate((GauranlenTreeSettings.PruningDuration).ToStringTicksToPeriod()));
            GauranlenTreeSettings.PruningDuration = (int)(listingStandard.Slider(GauranlenTreeSettings.PruningDuration, 625, 5000));

            listingStandard.Label("DebuffDurationDaysGauranlenTreeExpanded".Translate());
            listingStandard.Label("CurrentValueDaysGauranlenTreeExpanded".Translate(Math.Round(GauranlenTreeSettings.DurationDays, 1)));
            GauranlenTreeSettings.DurationDays = listingStandard.Slider(GauranlenTreeSettings.DurationDays, 1f, 15f);

            listingStandard.Label("DebuffBaseMoodValueGauranlenTreeExpanded".Translate());
            listingStandard.Label("CurrentValueHoursGauranlenTreeExpanded".Translate(Math.Round(GauranlenTreeSettings.BaseMoodDebuff, 0)));
            GauranlenTreeSettings.BaseMoodDebuff = (float)Math.Round(listingStandard.Slider(GauranlenTreeSettings.BaseMoodDebuff, 5f, 20f), 0);

            listingStandard.verticalSpacing = 2f;

            listingStandard.CheckboxLabeled("DisableTreeExtractingExpanded".Translate(), ref GauranlenTreeSettings.TreeExtraction);
            listingStandard.CheckboxLabeled("DisableDisconnectionRitualExpanded".Translate(), ref GauranlenTreeSettings.EnableDisconnectionRitual);

            if (listingStandard.ButtonText("RestoreDefaultsGauranlenTreeExpanded".Translate()))
            {
                GauranlenTreeSettings.MaxBonusDryad = 1.5f;
                GauranlenTreeSettings.SpawnDays = 8f;
                GauranlenTreeSettings.TreeExtraction = true;
                GauranlenTreeSettings.MaxMossRadius = 7.9f;
                GauranlenTreeSettings.BuildingRadius = 7.9f;
                GauranlenTreeSettings.EnableDisconnectionRitual = true;
                GauranlenTreeSettings.ConnectionTornTicks = 450000;
                GauranlenTreeSettings.PruningDuration = 2500;
                GauranlenTreeSettings.BaseMoodDebuff = 10f;
                GauranlenTreeSettings.DurationDays = 5f;
            }
            listingStandard.End();
        }

        public override string SettingsCategory()
        {
            return "Gauranlen Tree Expanded";
        }
    }
}
