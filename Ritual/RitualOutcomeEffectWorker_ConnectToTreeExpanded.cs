using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace GauranlenTreeExpanded
{
    class RitualOutcomeEffectWorker_ConnectToTreeExpanded : RitualOutcomeEffectWorker_FromQuality
    {
        public RitualOutcomeEffectWorker_ConnectToTreeExpanded()
        {

        }

        public RitualOutcomeEffectWorker_ConnectToTreeExpanded(RitualOutcomeEffectDef def)
            :base(def)
        {

        }

		public override void Apply(float progress, Dictionary<Pawn, int> totalPresence, LordJob_Ritual jobRitual)
		{
			Thing thing = jobRitual.selectedTarget.Thing;
			float quality = GetQuality(jobRitual, progress);
			int num = Mathf.Max(1, Mathf.RoundToInt(quality * 50f));
			CompSpawnSubplantDuration compSpawnSubplantDuration = thing.TryGetComp<CompSpawnSubplantDuration>();
			if (compSpawnSubplantDuration != null)
			{
				_ = compSpawnSubplantDuration.Props.subplant;
				foreach (Pawn key in totalPresence.Keys)
				{
					_ = key;
					for (int i = 0; i < num; i++)
					{
						compSpawnSubplantDuration.DoGrowSubplant(force: true);
					}
				}
				compSpawnSubplantDuration.SetupNextSubplantTick();
			}
			Pawn pawn = jobRitual.PawnWithRole("connector");
			CompTreeConnectionExpanded compTreeConnection = thing.TryGetComp<CompTreeConnectionExpanded>();
			if (pawn != null && compTreeConnection != null)
			{
				compTreeConnection.ConnectToPawn(pawn, quality);
				Find.LetterStack.ReceiveLetter("LetterLabelPawnConnected".Translate(thing.Named("TREE")), "LetterTextPawnConnected".Translate(thing.Named("TREE"), pawn.Named("CONNECTOR")), LetterDefOf.RitualOutcomePositive, pawn, null, null, new List<ThingDef> { thing.def });
			}
		}
	}
}
