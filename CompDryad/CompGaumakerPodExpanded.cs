using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GauranlenTreeExpanded
{
	public class CompGaumakerPodExpanded : CompDryadHolderExpanded
	{
		public bool Full => innerContainer.Count >= 3;

		public override void PostDeSpawn(Map map)
		{
			if (Find.TickManager.TicksGame < tickComplete)
			{
				innerContainer.TryDropAll(parent.Position, map, ThingPlaceMode.Near);
			}
		}

		public override void TryAcceptPawn(Pawn p)
		{
			base.TryAcceptPawn(p);
			if (Full)
			{
				_ = base.TreeComp.ConnectedPawns;
				tickComplete = Find.TickManager.TicksGame + (int)(60000f * base.Props.daysToComplete);
			}
		}

		protected override void Complete()
		{
			tickComplete = Find.TickManager.TicksGame;
			if (base.TreeComp != null)
			{
				for (int num = innerContainer.Count - 1; num >= 0; num--)
				{
					Pawn pawn;
					if ((pawn = innerContainer[num] as Pawn) != null)
					{
						base.TreeComp.RemoveDryad(pawn);
						Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
					}
				}
				base.TreeComp.gaumakerPod = null;
				((Plant)GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Plant_PodGauranlen), parent.Position, parent.Map)).Growth = 1f;
			}
			parent.Destroy();
		}

		public override string CompInspectStringExtra()
		{
			string text = base.CompInspectStringExtra();
			if (innerContainer.Count < 3)
			{
				if (!text.NullOrEmpty())
				{
					text += "\n";
				}
				text = text + GenLabel.BestKindLabel(PawnKindDefOf.Dryad_Gaumaker, Gender.Male, plural: true).CapitalizeFirst() + ": " + innerContainer.Count + "/" + 3;
			}
			return text;
		}
	}
}
