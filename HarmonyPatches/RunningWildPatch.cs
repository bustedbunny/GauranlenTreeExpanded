using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;

namespace GauranlenTreeExpanded
{
    [HarmonyPatch(typeof(MentalBreakWorker_RunWild), "TryStart")]
    public static class MentalBreakWorker_RunWildPatch
    {

        public static void Postfix(bool __result ,Pawn pawn)
        {
            if (__result)
            {
                for(int i = pawn.connections.ConnectedThings.Count - 1; i >= 0; i--)
                {
                    CompTreeConnectionExpanded comp = pawn.connections.ConnectedThings[i].TryGetComp<CompTreeConnectionExpanded>();
                    if (comp != null && pawn != null)
                    {
                        comp.TearConnection(pawn);
                        pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(DefOfClass.TearedConnectionMemoryExpanded);
                        pawn?.connections?.Notify_ConnectedThingDestroyed(pawn.connections.ConnectedThings[i]);
                    }
                }
                

            }
        }

    }
}
