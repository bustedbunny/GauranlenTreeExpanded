using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using HarmonyLib;
using UnityEngine;
using RimWorld.Planet;

namespace GauranlenTreeExpanded
{
    public class WorkGiver_ChangeTreeModeExpanded : WorkGiver_Scanner
    {
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerThings.ThingsMatching(ThingRequest.ForDef(ThingDefOf.Plant_TreeGauranlen));
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!ModsConfig.IdeologyActive)
            {
                return false;
            }
            CompTreeConnectionExpanded compTreeConnection = t.TryGetComp<CompTreeConnectionExpanded>();
            if (compTreeConnection == null || !compTreeConnection.ConnectedPawns.Contains(pawn) || compTreeConnection.Mode == compTreeConnection.desiredMode)
            {
                return false;
            }
            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Job job = JobMaker.MakeJob(JobDefOf.ChangeTreeMode, t);
            job.playerForced = forced;
            return job;
        }
    }

    public class JobDriver_ChangeTreeModeExpanded : JobDriver
    {
        private const int WaitTicks = 120;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.WaitWith(TargetIndex.A, 120, useProgressBar: true);
            yield return Toils_General.Do(delegate
            {
                job.targetA.Thing.TryGetComp<CompTreeConnectionExpanded>().FinalizeMode(pawn);
            }).PlaySustainerOrSound(SoundDefOf.DryadCasteSet);
            yield return Toils_General.Wait(60, TargetIndex.A);
        }

    }

	public class WorkGiver_PruneGauranlenTreeExpanded : WorkGiver_Scanner
	{
		public const float MaxConnectionStrengthForAutoPruning = 0.99f;

		public override bool Prioritized => true;

		public override float GetPriority(Pawn pawn, TargetInfo t)
		{
			if (!t.HasThing)
			{
				return 0f;
			}
			CompTreeConnectionExpanded compTreeConnection = t.Thing.TryGetComp<CompTreeConnectionExpanded>();
			if (compTreeConnection == null)
			{
				return 0f;
			}
			return compTreeConnection.DesiredConnectionStrength - compTreeConnection.GetConnectionStrength(pawn);
		}

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
		{
			return pawn.Map.listerThings.ThingsMatching(ThingRequest.ForDef(ThingDefOf.Plant_TreeGauranlen));
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			CompTreeConnectionExpanded compTreeConnection = t.TryGetComp<CompTreeConnectionExpanded>();
			if (compTreeConnection == null)
			{
				return false;
			}
			if (!compTreeConnection.ConnectedPawns.Contains(pawn))
			{
				return false;
			}
			if (!compTreeConnection.ShouldBePrunedNow(forced, pawn))
			{
				return false;
			}
			if (t.IsForbidden(pawn) || !pawn.CanReserve(t, 1, -1, null, forced))
			{
				return false;
			}
			return true;
		}

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			Job job = JobMaker.MakeJob(DefOfClass.PruneGauranlenTreeExpanded, t, pawn);
			job.playerForced = forced;
			return job;
		}
	}

	public class JobDriver_PruneGauranlenTreeExpanded : JobDriver
	{
		private int numPositions = 1;

		private const TargetIndex TreeIndex = TargetIndex.A;

		private const TargetIndex AdjacentCellIndex = TargetIndex.B;

		private int DurationTicks = 2500;

		private const int MaxPositions = 8;

		private CompTreeConnectionExpanded TreeConnection => job.GetTarget(TargetIndex.A).Thing.TryGetComp<CompTreeConnectionExpanded>();

		public override void Notify_Starting()
		{
			base.Notify_Starting();
			float num = TreeConnection.DesiredConnectionStrength - TreeConnection.GetConnectionStrength(pawn);
			numPositions = Mathf.Min(8, Mathf.CeilToInt(num / TreeConnection.ConnectionStrengthGainPerHourOfPruning(pawn)) + 1);
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
			//return pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			int ticks = Mathf.RoundToInt(DurationTicks / pawn.GetStatValue(StatDefOf.PruningSpeed));
			Toil findAdjacentCell = Toils_General.Do(delegate
			{
				job.targetB = GetAdjacentCell(job.GetTarget(TargetIndex.A).Thing);
			});
			Toil goToAdjacentCell = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell).FailOn(() => TreeConnection.GetConnectionStrength() >= TreeConnection.DesiredConnectionStrength);
			Toil prune = Toils_General.WaitWith(TargetIndex.A, ticks, maintainPosture: false).WithEffect(EffecterDefOf.Harvest_MetaOnly, TargetIndex.A).WithEffect(EffecterDefOf.GauranlenDebris, TargetIndex.A)
				.PlaySustainerOrSound(SoundDefOf.Interact_Prune);
			prune.WithProgressBarToilDelay(TargetIndex.B);
			prune.AddPreTickAction(delegate
			{
				TreeConnection.Prune(pawn);
				pawn.skills?.Learn(SkillDefOf.Plants, 0.085f);
				if (TreeConnection.GetConnectionStrength() >= TreeConnection.DesiredConnectionStrength)
				{
					ReadyForNextToil();
				}
			});
			prune.activeSkill = () => SkillDefOf.Plants;
			for (int i = 0; i < numPositions; i++)
			{
				yield return findAdjacentCell;
				yield return goToAdjacentCell;
				yield return prune;
			}
		}

		private IntVec3 GetAdjacentCell(Thing treeThing)
		{
			if ((from x in GenAdj.CellsAdjacent8Way(treeThing)
				 where x.InBounds(pawn.Map) && !x.Fogged(pawn.Map) && !x.IsForbidden(pawn) && pawn.CanReserveAndReach(x, PathEndMode.OnCell, Danger.Some)
				 select x).TryRandomElement(out var result))
			{
				return result;
			}
			return treeThing.Position;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref numPositions, "numPositions", 1);
		}
	}

	public class JobGiver_ReturnToGauranlenTreeExpanded : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn.connections == null || pawn.connections.ConnectedThings.NullOrEmpty())
			{
				return null;
			}
			foreach (Thing connectedThing in pawn.connections.ConnectedThings)
			{
				CompTreeConnectionExpanded compTreeConnection = connectedThing.TryGetComp<CompTreeConnectionExpanded>();
				if (compTreeConnection != null && compTreeConnection.ShouldReturnToTree(pawn) && pawn.CanReach(connectedThing, PathEndMode.Touch, Danger.Deadly))
				{
					return JobMaker.MakeJob(DefOfClass.ReturnToGauranlenTreeExpanded, connectedThing);
				}
			}
			return null;
		}
	}

	public class JobGiver_CreateAndEnterCocoonExpanded : JobGiver_CreateAndEnterDryadHolderExpanded
	{
		public override JobDef JobDef => DefOfClass.CreateAndEnterCocoonExpanded;

		public override bool ExtraValidator(Pawn pawn, CompTreeConnectionExpanded connectionComp)
		{
			if (connectionComp.DryadKind != pawn.kindDef)
			{
				return true;
			}
			return base.ExtraValidator(pawn, connectionComp);
		}
	}

	public class JobGiver_CreateAndEnterHealingPodExpanded : JobGiver_CreateAndEnterDryadHolderExpanded
	{
		public override JobDef JobDef => DefOfClass.CreateAndEnterHealingPodExpanded;

		public override bool ExtraValidator(Pawn pawn, CompTreeConnectionExpanded connectionComp)
		{
			if (pawn.mindState != null && pawn.mindState.returnToHealingPod)
			{
				if (HealthAIUtility.ShouldSeekMedicalRest(pawn))
				{
					return true;
				}
				if (pawn.health.hediffSet.GetMissingPartsCommonAncestors().Any())
				{
					return true;
				}
			}
			return base.ExtraValidator(pawn, connectionComp);
		}
	}

	public class JobGiver_MergeIntoGaumakerPodExpanded : ThinkNode_JobGiver
	{
		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!ModsConfig.IdeologyActive)
			{
				return null;
			}
			if (pawn.connections == null || pawn.connections.ConnectedThings.NullOrEmpty())
			{
				return null;
			}
			foreach (Thing connectedThing in pawn.connections.ConnectedThings)
			{
				CompTreeConnectionExpanded compTreeConnection = connectedThing.TryGetComp<CompTreeConnectionExpanded>();
				if (compTreeConnection != null && compTreeConnection.ShouldEnterGaumakerPod(pawn) && pawn.CanReach(compTreeConnection.gaumakerPod, PathEndMode.Touch, Danger.Deadly))
				{
					return JobMaker.MakeJob(DefOfClass.MergeIntoGaumakerPodExpanded, connectedThing, compTreeConnection.gaumakerPod);
				}
			}
			return null;
		}
	}

	public abstract class JobGiver_CreateAndEnterDryadHolderExpanded : ThinkNode_JobGiver
	{
		public const int SquareRadius = 4;

		public abstract JobDef JobDef { get; }

		public virtual bool ExtraValidator(Pawn pawn, CompTreeConnectionExpanded connectionComp)
		{
			return false;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (!ModsConfig.IdeologyActive)
			{
				return null;
			}
			if (pawn.connections == null || pawn.connections.ConnectedThings.NullOrEmpty())
			{
				return null;
			}
			foreach (Thing connectedThing in pawn.connections.ConnectedThings)
			{
				CompTreeConnectionExpanded compTreeConnection = connectedThing.TryGetComp<CompTreeConnectionExpanded>();
				if (compTreeConnection != null && ExtraValidator(pawn, compTreeConnection) && !connectedThing.IsForbidden(pawn) && pawn.CanReach(connectedThing, PathEndMode.Touch, Danger.Deadly) && CellFinder.TryFindRandomCellNear(connectedThing.Position, pawn.Map, 4, (IntVec3 c) => GauranlenUtility.CocoonAndPodCellValidator(c, pawn.Map), out var _))
				{
					return JobMaker.MakeJob(JobDef, connectedThing);
				}
			}
			return null;
		}
	}

	public class JobDriver_CreateAndEnterCocoonExpanded : JobDriver_CreateAndEnterDryadHolderExpanded
	{
		public override Toil EnterToil()
		{
			return Toils_General.Do(delegate
			{
				GenSpawn.Spawn(ThingDefOf.DryadCocoon, job.targetB.Cell, pawn.Map).TryGetComp<CompDryadCocoonExpanded>().TryAcceptPawn(pawn);
			});
		}
	}

	public abstract class JobDriver_CreateAndEnterDryadHolderExpanded : JobDriver
	{
		private const int TicksToCreate = 200;

		private CompTreeConnectionExpanded TreeComp => job.targetA.Thing.TryGetComp<CompTreeConnectionExpanded>();

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOn(() => TreeComp.ShouldReturnToTree(pawn));
			yield return Toils_General.Do(delegate
			{
				if (!CellFinder.TryFindRandomCellNear(job.GetTarget(TargetIndex.A).Cell, pawn.Map, 4, (IntVec3 c) => GauranlenUtility.CocoonAndPodCellValidator(c, pawn.Map), out var result))
				{
					Log.Error("Could not find cell to place dryad holder. Dryad=" + pawn.GetUniqueLoadID());
				}
				job.targetB = result;
			});
			yield return Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			yield return Toils_General.Wait(200).WithProgressBarToilDelay(TargetIndex.B).FailOnDespawnedOrNull(TargetIndex.B);
			yield return EnterToil();
		}

		public abstract Toil EnterToil();
	}

	public class JobDriver_CreateAndEnterHealingPodExpanded : JobDriver_CreateAndEnterDryadHolderExpanded
	{
		public override Toil EnterToil()
		{
			return Toils_General.Do(delegate
			{
				GenSpawn.Spawn(ThingDefOf.DryadHealingPod, job.targetB.Cell, pawn.Map).TryGetComp<CompDryadHealingPodExpanded>().TryAcceptPawn(pawn);
			});
		}
	}

	public class JobDriver_ReturnToGauranlenTreeExpanded : JobDriver
	{
		private const int WaitTicks = 180;

		private CompTreeConnectionExpanded TreeComp => job.targetA.Thing.TryGetComp<CompTreeConnectionExpanded>();

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOn(() => !TreeComp.ShouldReturnToTree(pawn));
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.WaitWith(TargetIndex.A, 180, useProgressBar: true).WithEffect(EffecterDefOf.GauranlenLeaves, TargetIndex.A).PlaySustainerOrSound(SoundDefOf.Interact_Sow);
			yield return Toils_General.Do(delegate
			{
				TreeComp.RemoveDryad(pawn);
				pawn.DeSpawn();
				Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
			});
		}
	}

	public class JobDriver_MergeIntoGaumakerPodExpanded : JobDriver
	{
		private const int WaitTicks = 120;

		private CompTreeConnectionExpanded TreeComp => job.targetA.Thing.TryGetComp<CompTreeConnectionExpanded>();

		private CompGaumakerPod GaumakerPod => job.targetB.Thing.TryGetComp<CompGaumakerPod>();

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedOrNull(TargetIndex.A);
			this.FailOnDespawnedOrNull(TargetIndex.B);
			this.FailOn(() => GaumakerPod.Full);
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
			yield return Toils_General.WaitWith(TargetIndex.B, 120, useProgressBar: true);
			yield return Toils_General.Do(delegate
			{
				GaumakerPod.TryAcceptPawn(pawn);
			});
		}
	}


}
