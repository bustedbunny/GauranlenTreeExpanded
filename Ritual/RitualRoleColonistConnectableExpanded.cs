using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using HarmonyLib;
using Verse.AI.Group;

namespace GauranlenTreeExpanded
{
    
    class RitualRoleColonistConnectableExpanded : RitualRoleColonistConnectable
	{
		public override bool AppliesToPawn(Pawn p, out string reason, LordJob_Ritual ritual = null, RitualRoleAssignments assignments = null, Precept_Ritual precept = null, bool skipReason = false)
		{
			if (p.connections.ConnectedThings.Any(x => x.def == ThingDefOf.Plant_TreeGauranlen))
            {
				reason = TranslatorFormattedStringExtensions.Translate("PawnIsAlreadyConnectedToThatTree", p.Name.ToStringFull);
				return false;
            }
			return base.AppliesToPawn(p, out reason, ritual, assignments, precept, skipReason);
		}
	}
    
}
