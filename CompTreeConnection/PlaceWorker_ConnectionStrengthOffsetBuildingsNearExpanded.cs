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
    class PlaceWorker_ConnectionStrengthOffsetBuildingsNearExpanded : PlaceWorker_ConnectionStrengthOffsetBuildingsNear
	{
		public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
		{
			if (!ModsConfig.IdeologyActive)
			{
				return;
			}
		//	CompProperties_TreeConnectionExpanded compProperties_TreeConnection = (CompProperties_TreeConnectionExpanded)def.CompDefFor<CompTreeConnectionExpanded>();
			List<Thing> list = Find.CurrentMap.listerArtificialBuildingsForMeditation.GetForCell(center, GauranlenTreeSettings.BuildingRadius);
			GenDraw.DrawRadiusRing(center, GauranlenTreeSettings.BuildingRadius, Color.white);
			if (list.NullOrEmpty())
			{
				return;
			}
			int num = 0;
			foreach (Thing item in list)
			{
				if (num++ > 10)
				{
					break;
				}
				GenDraw.DrawLineBetween(GenThing.TrueCenter(center, Rot4.North, def.size, def.Altitude), item.TrueCenter(), SimpleColor.Red);
			}
		}
	}
}
