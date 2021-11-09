using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace GauranlenTreeExpanded
{
    public class RitualObligationTargetWorker_ConnectedGauranlenTree : RitualObligationTargetFilter
    {
        private bool Enabled => GauranlenTreeSettings.EnableDisconnectionRitual;
        public RitualObligationTargetWorker_ConnectedGauranlenTree()
        {
        }

        public RitualObligationTargetWorker_ConnectedGauranlenTree(RitualObligationTargetFilterDef def)
            : base(def)
        {
        }

        public override IEnumerable<TargetInfo> GetTargets(RitualObligation obligation, Map map)
        {
            if (Enabled)
            {
                List<Thing> trees = map.listerThings.ThingsOfDef(ThingDefOf.Plant_TreeGauranlen);
                for (int i = 0; i < trees.Count; i++)
                {
                    CompTreeConnectionExpanded compTreeConnection = trees[i].TryGetComp<CompTreeConnectionExpanded>();
                    if (compTreeConnection != null && compTreeConnection.Connected)
                    {
                        yield return trees[i];
                    }
                }
            }
        }

        protected override RitualTargetUseReport CanUseTargetInternal(TargetInfo target, RitualObligation obligation)
        {
            if (Enabled)
            {
                Thing thing = target.Thing;
                CompTreeConnectionExpanded compTreeConnection = thing.TryGetComp<CompTreeConnectionExpanded>();
                if (compTreeConnection == null)
                {
                    return false;
                }
                if (!compTreeConnection.Connected)
                {
                    return false;
                }
                return true;
            }
            return false;

        }

        public override IEnumerable<string> GetTargetInfos(RitualObligation obligation)
        {
            yield return "RitualTargetConnectedGaruanlenTreeExpandedInfo".Translate();
        }
    }
}
