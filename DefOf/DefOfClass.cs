using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace GauranlenTreeExpanded
{
    [RimWorld.DefOf]
    public class DefOfClass
    {
        public static RitualBehaviorDef TreeConnectionExpanded;
        public static JobDef CreateAndEnterCocoonExpanded;
        public static JobDef CreateAndEnterHealingPodExpanded;
        public static JobDef ReturnToGauranlenTreeExpanded;
        public static JobDef MergeIntoGaumakerPodExpanded;
        public static JobDef PruneGauranlenTreeExpanded;
        public static ThinkTreeDef DryadExpanded;
        public static ThoughtDef TearedConnectionMemoryExpanded;
        public static DutyDef WanderClose;
        static DefOfClass()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOfClass));
        }

    }
}
