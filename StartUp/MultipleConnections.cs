using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using UnityEngine;
using HarmonyLib;

namespace GauranlenTreeExpanded
{
    [StaticConstructorOnStartup]
    public class StartUpClass
    {
        static StartUpClass()
        {

            var harmony = new Harmony("GauranlenTreeExpandedMod.patch");
            harmony.PatchAll();

            foreach (ThinkTreeDef item in DefDatabase<ThinkTreeDef>.AllDefs)
            {
                if (item.defName == "Dryad")
                {
                    item.thinkRoot = DefOfClass.DryadExpanded.thinkRoot;
                }
            }
            foreach(ThoughtDef item in DefDatabase<ThoughtDef>.AllDefs)
            {
                if (item.defName == "TearedConnectionMemoryExpanded")
                {
                    item.durationDays = GauranlenTreeSettings.DurationDays;
                    foreach (ThoughtStage stage in item.stages)
                    {
                        stage.baseMoodEffect = -1 * GauranlenTreeSettings.BaseMoodDebuff;

                    }
                }
            }
        }
    }
}
