using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace GauranlenTreeExpanded
{
    class RitualObligationTargetWorker_UnfilledGauranlenTree : RitualObligationTargetFilter
    {
        public RitualObligationTargetWorker_UnfilledGauranlenTree()
        {

        }

        public RitualObligationTargetWorker_UnfilledGauranlenTree(RitualObligationTargetFilterDef def)
            : base(def)
        {

        }
        public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
        {
            List<Thing> trees = map.listerThings.ThingsOfDef(ThingDefOf.Plant_TreeGauranlen);
            for (int i = 0; i < trees.Count; i++)
            {
                
                CompTreeConnectionExpanded comp = trees[i].TryGetComp<CompTreeConnectionExpanded>();
                if (comp != null && comp.CanBeConnected && !comp.ConnectionTorn)
                {
                    
                    yield return trees[i];
                }
            }
        }

        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            Thing thing = target.Thing;
            CompTreeConnectionExpanded comp = thing.TryGetComp<CompTreeConnectionExpanded>();
            if (comp == null)
            {
                return false;
            }
            if (!comp.CanBeConnected)
            {
                return "RitualCannotConnectMorePawnsGauranlenTreeExpanded".Translate();
            }
            if (comp.ConnectionTorn)
            {
                return "RitualTargetConnectionTornGauranlenTree".Translate(thing.Named("TREE"), comp.UntornInDurationTicks.ToStringTicksToPeriod()).CapitalizeFirst();
            }
            return true;
        }

        public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
        {
            yield return "RitualTargetUnfilledGaruanlenTreeExpandedInfo".Translate();
        }

    }
}
