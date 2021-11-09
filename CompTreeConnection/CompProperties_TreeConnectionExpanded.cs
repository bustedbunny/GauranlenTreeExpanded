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
    public class CompProperties_TreeConnectionExpanded : CompProperties
	{
		public PawnKindDef pawnKind;

		public FloatRange initialConnectionStrengthRange;

		public float connectionStrengthLossPerDryadDeath = 0.1f;

		public float radiusToBuildingForConnectionStrengthLoss = 7.9f;

		public int maxDryadsWild;

		public SimpleCurve maxDryadsPerConnectionStrengthCurve;

		public SimpleCurve connectionLossPerLevelCurve;

		public SimpleCurve connectionLossDailyPerBuildingDistanceCurve;

		public SimpleCurve connectionStrengthGainPerPlantSkill;

		public float connectionStrengthGainPerHourPruningBase = 0.01f;

		public Vector3 spawningPodOffset;

		public FloatRange spawningPodSizeRange = FloatRange.One;
		public CompProperties_TreeConnectionExpanded()
        {
            compClass = typeof(CompTreeConnectionExpanded);
        }
    }
}
