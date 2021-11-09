using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace GauranlenTreeExpanded
{
    public class GauranlenMossExpandedComp : ThingComp
    {
        private Plant tree;

        public void SetupParentTree(Plant tree)
        {
            this.tree = tree;
        }
        public override void CompTick()
        {
            if (parent.IsHashIntervalTick(12000))
            {
                if (tree != null && tree.Spawned)
                {
                    int num = IntVec3Utility.ManhattanDistanceFlat(parent.Position, tree.Position);
                    if (num > 35)
                    {
                        parent.Kill();
                        return;
                    }
                }
                if (tree == null)
                {
                    parent.Kill();
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref tree, "tree");
        }
    }
}
