using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace GauranlenTreeExpanded
{
    [HarmonyPatch(typeof(MinifiedTree), "RecordTreeDeath")]
    public static class MinifiedTreePatch
    {

        public static void Postfix(MinifiedTree __instance)
        {       
            if (__instance?.InnerTree.AllComps != null)
            {
                for (int i = 0; i < __instance.InnerTree.AllComps.Count; i++)
                {
                    __instance.InnerTree.AllComps[i].PostDestroy(DestroyMode.Vanish, __instance.MapHeld);
                }
            }
        }

    }


}
