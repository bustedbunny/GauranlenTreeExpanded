using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace GauranlenTreeExpanded
{
    public class RitualOutcomeEffectWorker_TearConnection : RitualOutcomeEffectWorker_FromQuality
    {
        public override bool SupportsAttachableOutcomeEffect => false;

        public RitualOutcomeEffectWorker_TearConnection()
        {
        }
        public RitualOutcomeEffectWorker_TearConnection(RitualOutcomeEffectDef def)
            : base(def)
        {
        }

        public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
        {
            Thing thing = jobRitual.selectedTarget.Thing;
            CompTreeConnectionExpanded comp = thing.TryGetComp<CompTreeConnectionExpanded>();
            Pawn pawn = jobRitual.PawnWithRole("disconnector");
            if (comp != null && pawn != null)
            {
                comp.TearConnection(pawn);
                pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(DefOfClass.TearedConnectionMemoryExpanded);
                pawn?.connections?.Notify_ConnectedThingDestroyed(thing);
            }

        }
    }

    [HarmonyPatch(typeof(Pawn_ConnectionsTracker), "Notify_ConnectedThingDestroyed")]
    public class Notify_ConnectedThingDestroyedPatch
    {
        public static bool Prefix(Thing thing, List<Thing> ___connectedThings)
        {
            if (!thing.Destroyed)
            {
                ___connectedThings.Remove(thing);
                return false;
            }
            return true;
        }
    }
}
