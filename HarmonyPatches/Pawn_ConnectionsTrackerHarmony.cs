using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using HarmonyLib;
using RimWorld;

namespace GauranlenTreeExpanded
{
    [HarmonyPatch(typeof(Pawn_ConnectionsTracker), "Notify_PawnKilled")]
    public static class Pawn_ConnectionsTrackerHarmony
    {
        
        public static void Prefix(Pawn_ConnectionsTracker __instance, Pawn ___pawn, List<Thing> ___connectedThings)
        {
            List<Thing> list = ___connectedThings;
            for (int num = ___connectedThings.Count - 1; num >= 0; num--)
            {
                CompTreeConnectionExpanded comp = ___connectedThings[num].TryGetComp<CompTreeConnectionExpanded>();
                if (comp != null)
                {
                    comp.Notify_PawnDied(___pawn);
                    ___connectedThings.RemoveAt(num);
                }
            }
        }

    }
    /*
    [HarmonyPatch(typeof(Pawn_ConnectionsTracker), "ExposeData")]
    public static class Pawn_ConnectionsTrackerExposeData
    {

        public static void Postfix(Pawn_ConnectionsTracker __instance, Pawn ___pawn, List<Thing> ___connectedThings)
        {
            //if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                for (int i = ___connectedThings.Count - 1; i >= 0; i--)
                {
                    CompTreeConnection comp = ___connectedThings[i].TryGetComp<CompTreeConnection>();
                    if (comp != null)
                    {
                        Log.Message("did it run?");
                        comp.PostDestroy(DestroyMode.Vanish, ___pawn.MapHeld);
                        ___connectedThings.RemoveAt(i);
                    }

                }
            }


        }

    }
    */
}
