using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace GauranlenTreeExpanded
{
    public class RitualRoleTearConnection : RitualRoleColonistConnectable
    {
        public override bool AppliesToPawn(Pawn p, out string reason, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
        {
            if (p?.connections?.ConnectedThings.Any(x => x.def == ThingDefOf.Plant_TreeGauranlen) ?? false)
            {
                reason = "what?";
                return true;
            }
            reason = "idk";
            return false;
        }
    }
}
