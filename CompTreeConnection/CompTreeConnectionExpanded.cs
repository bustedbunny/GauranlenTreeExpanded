using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace GauranlenTreeExpanded
{
    public class CompTreeConnectionExpanded : ThingComp
    {
        private float MaxBonusDryad => GauranlenTreeSettings.MaxBonusDryad;
        private float SpawnDays => GauranlenTreeSettings.SpawnDays;
        private int ConnectionTornTicks => GauranlenTreeSettings.ConnectionTornTicks;
        private float MaxBuildingRadius => GauranlenTreeSettings.BuildingRadius;
        private List<Pawn> connectedPawns = new List<Pawn> { };
        public List<Pawn> ConnectedPawns => connectedPawns;
        private int nextUntornTick = -1;
        private int spawnTick = -1;
        private int lastSubPlantTick = -1;
        public bool ConnectionTorn => nextUntornTick >= Find.TickManager.TicksGame;

        public bool HasProductionMode => desiredMode != null;

        public int UntornInDurationTicks => nextUntornTick - Find.TickManager.TicksGame;
        private float ConnectionStrength
        {
            get
            {
                if (!Connected)
                {
                    return 0;
                }
                float num = 0;
                foreach (Pawn pawn in connectedPawns)
                {
                    if (connectionStrength.TryGetValue(pawn, out float value))
                    {
                        num += value;
                    }

                }
                return num / connectedPawns.Count;
            }
        }
        private Dictionary<Pawn, float> connectionStrength = new Dictionary<Pawn, float>();
        public float GetConnectionStrength(Pawn pawn = null)
        {
            if (pawn != null)
            {
                if (connectionStrength.TryGetValue(pawn, out float value))
                {
                    return value;
                }
            }
            return ConnectionStrength;
        }
        public void SetConnectionStrength(Pawn pawn, float value)
        {
            lock (connectionStrength)
            {
                connectionStrength.SetOrAdd(pawn, value);
            }
        }
        private List<Pawn> dryads = new List<Pawn>();
        private Dictionary<Pawn, int> lastPrunedTicks = new Dictionary<Pawn, int> { };
        public int GetLastPrunedTick(Pawn pawn)
        {
            if (pawn != null)
            {
                return lastPrunedTicks.TryGetValue(pawn);
            }
            return 0;
        }
        public void SetLastPrunedTick(Pawn pawn, int value)
        {
            lock (lastPrunedTicks)
            {
                lastPrunedTicks.SetOrAdd(pawn, value);
            }
        }
        public float DesiredConnectionStrength = 0.5f;
        private GauranlenTreeModeDef currentMode;
        private Effecter leafEffecter;
        public Thing gaumakerPod;
        private Material cachedPodMat;
        private Lord cachedLordJob;
        private int lordJobCoolDown = 0;

        public bool CanBeConnected => connectedPawns.Count < 4;
        private float ClosestDistanceToBlockingBuilding(List<Thing> buildings)
        {
            float num = float.PositiveInfinity;
            for (int i = 0; i < buildings.Count; i++)
            {
                float num2 = buildings[i].Position.DistanceTo(parent.Position);
                if (num2 < num)
                {
                    num = num2;
                }
            }
            return num;
        }

        public int MaxDryads
        {
            get
            {
                if (!Connected)
                {
                    return Props?.maxDryadsWild ?? 0;
                }
                int num = 0;
                foreach (Pawn pawn in connectedPawns)
                {
                    num += (int)Props.maxDryadsPerConnectionStrengthCurve.Evaluate(GetConnectionStrength(pawn));
                }
                num = (int)Math.Round(num / connectedPawns.Count * GenMath.LerpDouble(1, 4, 1, MaxBonusDryad, connectedPawns.Count));
                return num;

            }
        }
        private void SpawnDryad()
        {
            spawnTick = Find.TickManager.TicksGame + (int)(60000f * SpawnDays);
            Pawn dryad = GenerateNewDryad(Props.pawnKind);
            GenSpawn.Spawn(dryad, parent.Position, parent.Map).Rotation = Rot4.South;
            EffecterDefOf.DryadSpawn.Spawn(parent.Position, parent.Map).Cleanup();
            SoundDefOf.Pawn_Dryad_Spawn.PlayOneShot(SoundInfo.InMap(dryad));
        }
        public Pawn GenerateNewDryad(PawnKindDef dryadCaste)
        {
            PawnGenerationRequest request = default;
            request.KindDef = dryadCaste;
            request.Newborn = true;
            request.FixedGender = Gender.Male;
            Pawn dryad = PawnGenerator.GeneratePawn(request);
            ResetDryad(dryad);
            dryad.connections?.ConnectTo(parent);
            dryads.Add(dryad);
            return dryad;
        }

        private void ResetDryad(Pawn dryad)
        {
            if (Connected && dryad.Faction != connectedPawns[0]?.Faction)
            {
                dryad.SetFaction(connectedPawns[0]?.Faction);
            }
            if (dryad.training == null)
            {
                return;
            }
            foreach (TrainableDef allDef in DefDatabase<TrainableDef>.AllDefs)
            {
                if (dryad.training.CanAssignToTrain(allDef).Accepted)
                {
                    dryad.training.SetWantedRecursive(allDef, checkOn: true);

                    foreach (Pawn pawn in connectedPawns)
                    {
                        int num = 0;
                        foreach (Pawn pawn2 in dryads)
                        {
                            if (pawn2?.playerSettings.Master == pawn)
                            {
                                num++;
                            }
                        }
                        if ((int)Props.maxDryadsPerConnectionStrengthCurve.Evaluate(GetConnectionStrength(pawn)) <= num)
                        {
                            continue;
                        }
                        dryad.training.Train(allDef, pawn, complete: true);
                        if (allDef == TrainableDefOf.Release)
                        {
                            dryad.playerSettings.followDrafted = true;
                        }
                        break;
                    }

                }
            }
        }
        public bool Connected => (connectedPawns.Count > 0);
        public CompProperties_TreeConnectionExpanded Props => (CompProperties_TreeConnectionExpanded)props;
        public GauranlenTreeModeDef Mode => currentMode;

        private List<Thing> BuildingsReducingConnectionStrength
        {
            get
            {
                return parent.Map.listerArtificialBuildingsForMeditation.GetForCell(parent.Position, MaxBuildingRadius);
            }
        }


        public void Prune(Pawn pawn)
        {
            SetLastPrunedTick(pawn, Find.TickManager.TicksGame);
            float num = GetConnectionStrength(pawn) + connectedPawns.Count * ConnectionStrengthGainPerHourOfPruning(pawn) / GauranlenTreeSettings.PruningDuration;
            SetConnectionStrength(pawn, num);
            if (lastSubPlantTick < Find.TickManager.TicksGame)
            {
                parent.TryGetComp<CompSpawnSubplantDurationExpanded>()?.DoGrowSubplant(true);
                lastSubPlantTick = Find.TickManager.TicksGame + 15000;
            }

        }
        public bool ShouldBePrunedNow(bool forced, Pawn pawn)
        {
            if (ConnectionStrength >= DesiredConnectionStrength || GetConnectionStrength(pawn)>= DesiredConnectionStrength)
            {
                return false;
            }
            if (!forced)
            {
                if (ConnectionStrength >= DesiredConnectionStrength - 0.03f)
                {
                    return false;
                }
                if (Find.TickManager.TicksGame < GetLastPrunedTick(pawn) + 10000)
                {
                    return false;
                }
            }
            return true;
        }
        public float ConnectionStrengthGainPerHourOfPruning(Pawn pawn)
        {
            float num = Props.connectionStrengthGainPerHourPruningBase * pawn.GetStatValue(StatDefOf.PruningSpeed);
            if (Props.connectionStrengthGainPerPlantSkill != null)
            {
                num *= Props.connectionStrengthGainPerPlantSkill.Evaluate(pawn.skills.GetSkill(SkillDefOf.Plants).Level);
            }
            return num;
        }
        /*
        private string ConnectionStrengthToMaxDryadsDesc()
        {
            string text = (string)("MaxDryadsBasedOnConnectionStrength".Translate() + ":\n -  " + "Unconnected".Translate() + ": ") + Props.maxDryadsWild;
            foreach (CurvePoint item in Props.maxDryadsPerConnectionStrengthCurve)
            {
                text = text + (string)("\n -  " + "ConnectionStrengthDisplay".Translate(item.x.ToStringPercent()) + ": ") + item.y;
            }
            return text;
        }
        */
        public float PruningHoursToMaintain(float desired, Pawn pawn)
        {
            float num = Props.connectionLossPerLevelCurve.Evaluate(desired);
            List<Thing> buildingsReducingConnectionStrength = BuildingsReducingConnectionStrength;
            if (buildingsReducingConnectionStrength.Any())
            {
                num += Props.connectionLossDailyPerBuildingDistanceCurve.Evaluate(ClosestDistanceToBlockingBuilding(buildingsReducingConnectionStrength));
            }
            return num / ConnectionStrengthGainPerHourOfPruning(pawn);
        }
        public float ConnectionStrengthLossPerDay(Pawn pawn)
        {

            float num = Props.connectionLossPerLevelCurve.Evaluate(connectionStrength.TryGetValue(pawn));
            List<Thing> buildingsReducingConnectionStrength = BuildingsReducingConnectionStrength;
            if (parent.Spawned && buildingsReducingConnectionStrength.Any())
            {
                num += Props.connectionLossDailyPerBuildingDistanceCurve.Evaluate(ClosestDistanceToBlockingBuilding(buildingsReducingConnectionStrength));
            }
            return num / connectedPawns.Count;
        }
        public float ConnectionStrengthLossPerDayAll()
        {
            float num = 0;
            foreach (Pawn pawn in connectedPawns)
            {
                num += ConnectionStrengthLossPerDay(pawn);
            }
            return num;
        }

        private bool TryGetGaumakerCell(out IntVec3 cell)
        {
            cell = IntVec3.Invalid;
            if (CellFinder.TryFindRandomCellNear(parent.Position, parent.Map, 3, (IntVec3 c) => GauranlenUtility.CocoonAndPodCellValidator(c, parent.Map, ThingDefOf.Plant_PodGauranlen), out cell) || CellFinder.TryFindRandomCellNear(parent.Position, parent.Map, 3, (IntVec3 c) => GauranlenUtility.CocoonAndPodCellValidator(c, parent.Map, ThingDefOf.Plant_TreeGauranlen), out cell))
            {
                return true;
            }
            return false;
        }

        private Gizmo_PruningConfigExpanded pruningGizmo;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Connected)
            {
                Command_Action command_Action = new Command_Action
                {
                    defaultLabel = "ChangeMode".Translate(),
                    defaultDesc = "ChangeModeDesc".Translate(parent.Named("TREE")),
                    icon = ((Mode == null) ? ContentFinder<Texture2D>.Get("UI/Gizmos/UpgradeDryads") : Widgets.GetIconFor(Mode.pawnKindDef.race)),
                    action = delegate
                    {
                        Find.WindowStack.Add(new Dialog_ChangeDryadCasteExpanded(parent));
                    }
                };
                bool flag = false;
                foreach (Pawn pawn in connectedPawns)
                {
                    if (pawn.Spawned || pawn.Map == parent.Map)
                    {
                        flag = true;
                    }
                }
                if (!flag)
                {
                    if (connectedPawns.Count > 1)
                    {
                        command_Action.Disable("AllConnectedPawnsAreAway".Translate());
                    }
                    else
                    {
                        command_Action.Disable("ConnectedPawnAway".Translate(connectedPawns[0].Named("PAWN")));
                    }
                }
                yield return command_Action;

                if (pruningGizmo == null)
                {
                    pruningGizmo = new Gizmo_PruningConfigExpanded(this);
                }
                yield return pruningGizmo;

                if (dryads.Count > 0)
                {
                    Command_Action command_Action1 = new Command_Action
                    {
                        defaultLabel = "DefendTreeLabelExpanded".Translate(),
                        defaultDesc = "DefendTreeExpandedDesc".Translate(),
                        icon = ContentFinder<Texture2D>.Get("UI/Commands/Draft", true),
                        action = delegate
                        {
                            if (cachedLordJob == null)
                            {
                                foreach (Pawn dryad in dryads)
                                {
                                    dryad.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                                }
                                cachedLordJob = LordMaker.MakeNewLord(dryads[0].Faction, new LordJob_DefendPoint(parent.Position, 5), parent.Map, dryads);
                                lordJobCoolDown = Find.TickManager.TicksGame + 12000;
                            }
                        }
                    };
                    if (cachedLordJob != null)
                    {
                        command_Action1.Disable("CooldownTime".Translate() + " " + (lordJobCoolDown - Find.TickManager.TicksGame).ToStringTicksToPeriod());
                    }
                    yield return command_Action1;
                }


            }

            if (Prefs.DevMode)
            {
                Command_Action command_Action2 = new Command_Action
                {
                    defaultLabel = "DEV: Spawn dryad",
                    action = delegate
                    {
                        SpawnDryad();
                    }
                };
                yield return command_Action2;
                Command_Action command_Action3 = new Command_Action
                {
                    defaultLabel = "DEV: Connection strength -10%",
                    action = delegate
                    {
                        foreach (Pawn pawn in connectedPawns)
                        {
                            SetConnectionStrength(pawn, GetConnectionStrength(pawn) - 0.1f);
                        }
                    }
                };
                yield return command_Action3;
                Command_Action command_Action4 = new Command_Action
                {
                    defaultLabel = "DEV: Connection strength +10%",
                    action = delegate
                    {
                        foreach (Pawn pawn in connectedPawns)
                        {
                            SetConnectionStrength(pawn, GetConnectionStrength(pawn) + 0.1f);
                        }
                    }
                };
                yield return command_Action4;
                Command_Action command_Action5 = new Command_Action
                {
                    defaultLabel = "DEV: spawn subplant",
                    action = delegate
                    {
                        parent?.TryGetComp<CompSpawnSubplantDurationExpanded>()?.DoGrowSubplant(true);
                    }
                };
                yield return command_Action5;
            }

        }


        public override void CompTick()
        {
            
            if (!ModsConfig.IdeologyActive || !parent.Spawned)
            {
                return;
            }
            int currentTick = Find.TickManager.TicksGame;
            
            if (cachedLordJob != null)
            {
                if (currentTick > lordJobCoolDown)
                {
                    for (int i = cachedLordJob.ownedPawns.Count -1; i >= 0; i--)
                    {
                        cachedLordJob.Notify_PawnLost(cachedLordJob.ownedPawns[i], PawnLostCondition.Undefined);
                    }
                    cachedLordJob = null;
                }
            }
            if (currentTick >= spawnTick)
            {
                SpawnDryad();
            }
            if (Connected)
            {
                if (leafEffecter == null)
                {
                    leafEffecter = EffecterDefOf.GauranlenLeavesBatch.Spawn();
                    leafEffecter.Trigger(parent, parent);
                }
                leafEffecter?.EffectTick(parent, parent);

                foreach (Pawn pawn in connectedPawns)
                {
                    if (currentTick - lastPrunedTicks.TryGetValue(pawn) > 1)
                    {
                        float num = connectionStrength.TryGetValue(pawn) - ConnectionStrengthLossPerDay(pawn) / 60000f;
                        SetConnectionStrength(pawn, num);
                    }

                }

            }

            if (!parent.IsHashIntervalTick(300))
            {
                return;
            }
            if (Mode == GauranlenTreeModeDefOf.Gaumaker && dryads.Count >= 3)
            {
                if (gaumakerPod == null && TryGetGaumakerCell(out var cell))
                {
                    gaumakerPod = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.GaumakerCocoon), cell, parent.Map);
                }
            }
            else if (gaumakerPod != null && !gaumakerPod.Destroyed)
            {
                gaumakerPod.Destroy();
                gaumakerPod = null;
            }

        }

        public string AffectingBuildingsDescription(string descKey)
        {
            List<Thing> buildingsReducingConnectionStrength = BuildingsReducingConnectionStrength;
            if (buildingsReducingConnectionStrength.Count > 0)
            {
                IEnumerable<string> source = buildingsReducingConnectionStrength.Select((Thing c) => GenLabel.ThingLabel(c, 1, includeHp: false)).Distinct();
                TaggedString taggedString = descKey.Translate() + ": " + source.Take(3).ToCommaList().CapitalizeFirst();
                if (source.Count() > 3)
                {
                    taggedString += " " + "Etc".Translate();
                }
                return taggedString;
            }
            return null;
        }

        private int SpawningDurationTicks => (int)(60000f * SpawnDays);
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!ModLister.CheckIdeology("Tree connection"))
            {
                parent.Destroy();
            }
            else if (!respawningAfterLoad)
            {
                foreach (Pawn pawn in connectedPawns)
                {
                    SetLastPrunedTick(pawn, Find.TickManager.TicksGame);
                }
                spawnTick = Find.TickManager.TicksGame + SpawningDurationTicks;
            }
        }
        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            leafEffecter?.Cleanup();
            leafEffecter = null;

            for (int i = dryads.Count - 1; i >= 0; i--)
            {
                dryads[i].connections?.Notify_ConnectedThingDestroyed(parent);
                dryads[i].forceNoDeathNotification = true;
                dryads[i].Kill(null, null);
                dryads[i].forceNoDeathNotification = false;
            }
            if (Connected && connectedPawns[0].Faction == Faction.OfPlayer)
            {
                foreach (Pawn pawn in connectedPawns)
                {
                    Find.LetterStack.ReceiveLetter("LetterLabelConnectedTreeDestroyed".Translate(parent.Named("TREE")), "LetterTextConnectedTreeDestroyed".Translate(parent.Named("TREE"), pawn.Named("CONNECTEDPAWN")), LetterDefOf.NegativeEvent, pawn);
                    pawn?.connections?.Notify_ConnectedThingDestroyed(parent);
                }
                for (int i = connectedPawns.Count - 1; i >= 0; i--)
                {
                    TearConnection(connectedPawns[i]);
                    
                }
            }
        }



        public void ConnectToPawn(Pawn pawn, float ritualQuality)
        {
            if (!ConnectionTorn)
            {
                connectedPawns.Add(pawn);
                pawn.connections?.ConnectTo(parent);
                SetConnectionStrength(pawn, Props.initialConnectionStrengthRange.LerpThroughRange(ritualQuality));
                SetLastPrunedTick(pawn, 0);
                for (int i = 0; i < dryads.Count; i++)
                {
                    ResetDryad(dryads[i]);
                    dryads[i].MentalState?.RecoverFromState();
                }
            }
        }

        public GauranlenTreeModeDef desiredMode;
        public void FinalizeMode(Pawn pawn)
        {
            currentMode = desiredMode;
            if (Connected)
            {
                MoteMaker.MakeStaticMote((pawn.Position.ToVector3Shifted() + parent.Position.ToVector3Shifted()) / 2f, parent.Map, ThingDefOf.Mote_GauranlenCasteChanged);
            }
        }

        public bool ShouldEnterGaumakerPod(Pawn dryad)
        {
            if (gaumakerPod == null || gaumakerPod.Destroyed)
            {
                return false;
            }
            if (dryads.NullOrEmpty() || dryads.Count < 3 || !dryads.Contains(dryad))
            {
                return false;
            }
            tmpDryads.Clear();
            for (int i = 0; i < dryads.Count; i++)
            {
                if (dryads[i].kindDef == PawnKindDefOf.Dryad_Gaumaker)
                {
                    tmpDryads.Add(dryads[i]);
                }
            }
            if (tmpDryads.Count < 3)
            {
                tmpDryads.Clear();
                return false;
            }
            tmpDryads.SortBy((Pawn x) => -x.ageTracker.AgeChronologicalTicks);
            for (int j = 0; j < 3; j++)
            {
                if (tmpDryads[j] == dryad)
                {
                    tmpDryads.Clear();
                    return true;
                }
            }
            tmpDryads.Clear();
            return false;
        }
        public void RemoveDryad(Pawn oldDryad)
        {
            dryads.Remove(oldDryad);
        }

        public void TearConnection(Pawn pawn)
        {
            Messages.Message("MessageConnectedPawnDied".Translate(parent.Named("TREE"), pawn.Named("PAWN"), ConnectionTornTicks.ToStringTicksToDays().Named("DURATION")), parent, MessageTypeDefOf.NegativeEvent);

            for (int i = 0; i < dryads.Count; i++)
            {
                ResetDryad(dryads[i]);
            }

            SoundDefOf.GauranlenConnectionTorn.PlayOneShot(SoundInfo.InMap(parent));
            nextUntornTick = Find.TickManager.TicksGame + ConnectionTornTicks;
            connectedPawns.Remove(pawn);
            lastPrunedTicks.Remove(pawn);
            connectionStrength.Remove(pawn);

            if (!Connected)
            {
                currentMode = null;
            }

        }
        public void Notify_PawnDied(Pawn p)
        {
            if (connectedPawns.Contains(p))
            {
                TearConnection(p);
                return;
            }
            if (Connected)
            {
                for (int i = 0; i < dryads.Count; i++)
                {
                    if (p == dryads[i])
                    {
                        foreach (Pawn pawn in connectedPawns)
                        {
                            pawn.needs?.mood?.thoughts?.memories.TryGainMemory(ThoughtDefOf.DryadDied);
                            float num = GetConnectionStrength(pawn) - Props.connectionStrengthLossPerDryadDeath;
                            SetConnectionStrength(pawn, num);
                        }
                        dryads.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public override void PostDraw()
        {
            if (dryads.Count < MaxDryads)
            {
                Matrix4x4 matrix = default;
                Vector3 pos = parent.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.BuildingOnTop.AltitudeFor()) + Props.spawningPodOffset;
                float num = Props.spawningPodSizeRange.LerpThroughRange(1f - (float)spawnTick - (float)Find.TickManager.TicksGame / (float)SpawningDurationTicks);
                matrix.SetTRS(pos, Quaternion.identity, new Vector3(num, 1f, num));
                Graphics.DrawMesh(MeshPool.plane10, matrix, PodMat, 0);
            }
        }
        private Material PodMat
        {
            get
            {
                if (cachedPodMat == null)
                {
                    cachedPodMat = MaterialPool.MatFrom("Things/Building/Misc/DryadFormingPod/DryadFormingPod", ShaderDatabase.Cutout);
                }
                return cachedPodMat;
            }
        }

        private readonly List<Pawn> tmpDryads = new List<Pawn>();
        public PawnKindDef DryadKind => Mode?.pawnKindDef ?? PawnKindDefOf.Dryad_Basic;
        public bool ShouldReturnToTree(Pawn dryad)
        {
            if (dryads.NullOrEmpty() || !dryads.Contains(dryad))
            {
                return false;
            }
            foreach (Pawn pawn in connectedPawns)
            {
                if (dryad.connections != null && dryad.playerSettings.Master == pawn)
                {
                    if ((int)Props.maxDryadsPerConnectionStrengthCurve.Evaluate(GetConnectionStrength(pawn)) < dryads.Where(x => x.playerSettings.Master == pawn).Count())
                    {
                        return true;
                    }
                }
            }
            int num = dryads.Count - MaxDryads;
            if (num <= 0)
            {
                return false;
            }
            tmpDryads.Clear();
            tmpDryads.AddRange(dryads);
            tmpDryads.SortBy((Pawn x) => x.kindDef == DryadKind, (Pawn x) => x.ageTracker.AgeChronologicalTicks);
            for (int i = 0; i < num; i++)
            {
                if (tmpDryads[i] == dryad)
                {
                    tmpDryads.Clear();
                    return true;
                }
            }
            tmpDryads.Clear();
            return false;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            foreach (Pawn pawn in connectedPawns)
            {
                pawn?.connections?.DrawConnectionLine(parent);
            }

        }

        private float MinConnectionStrengthForSingleDryad
        {
            get
            {
                foreach (CurvePoint point in Props.maxDryadsPerConnectionStrengthCurve.Points)
                {
                    if (point.y > 0f)
                    {
                        return point.x;
                    }
                }
                return 0f;
            }
        }
        public override string CompInspectStringExtra()
        {
            string text = base.CompInspectStringExtra();
            if (!text.NullOrEmpty())
            {
                text += "\n";
            }
            if (ConnectionTorn)
            {
                text = text + "ConnectionTorn".Translate(UntornInDurationTicks.ToStringTicksToPeriod()).Resolve() + "\n";
            }
            string text2 = string.Empty;
            if (dryads.Count < MaxDryads)
            {
                text2 = "SpawningDryadIn".Translate(NamedArgumentUtility.Named(Props.pawnKind, "DRYAD"), (spawnTick - Find.TickManager.TicksGame).ToStringTicksToPeriod().Named("TIME")).Resolve();
            }
            if (Connected)
            {
                //   "Nobody".Translate().CapitalizeFirst()).Resolve();
                text = text + "ConnectedPawn".Translate().Resolve() + ": ";
                for (int i = 0; i < connectedPawns.Count; i++)
                {
                    text += connectedPawns[i].NameFullColored;
                    if (i != connectedPawns.Count - 1)
                    {
                        text += ", ";
                    }
                    else
                    {
                        text += ".";
                    }

                }
                foreach (Pawn pawn in connectedPawns)
                {
                    if (lastPrunedTicks.TryGetValue(pawn) >= 0 && Find.TickManager.TicksGame - lastPrunedTicks.TryGetValue(pawn) <= 60)
                    {
                        text = string.Concat(text, "\n", pawn.NameShortColored, ": ", "PruningConnectionStrength".Translate(), ": ", "PerHour".Translate(ConnectionStrengthGainPerHourOfPruning(pawn).ToStringPercent()).Resolve());
                    }
                }

                if (HasProductionMode && Mode != desiredMode)
                {
                    if (connectedPawns.Count == 1)
                    {
                        text = text + "\n" + "WaitingForConnectorToChangeCaste".Translate(connectedPawns[0].Named("CONNECTEDPAWN")).Resolve();
                    }
                    else
                    {
                        text = text + "\n" + "WaitingForConnectorsToChangeCaste".Translate().Resolve();
                    }

                }
                if (Mode != null)
                {
                    text += string.Concat("\n", "GauranlenTreeMode".Translate(), ": ") + Mode.LabelCap;
                }
                if (!text2.NullOrEmpty())
                {
                    text = text + "\n" + text2;
                }
                if (MaxDryads > 0)
                {
                    text = string.Concat(text, "\n", "DryadPlural".Translate(), $" ({dryads.Count}/{MaxDryads})");
                    if (dryads.Count > 0)
                    {
                        text = text + ": " + dryads.Select((Pawn x) => x.NameShortColored.Resolve()).ToCommaList().CapitalizeFirst();
                    }
                }
                else
                {
                    text = text + "\n" + "NotEnoughConnectionStrengthForSingleDryad".Translate(MinConnectionStrengthForSingleDryad.ToStringPercent()).Colorize(ColorLibrary.RedReadable);
                }
                if (!HasProductionMode)
                {
                    text = text + "\n" + "AlertGauranlenTreeWithoutDryadTypeLabel".Translate().Colorize(ColorLibrary.RedReadable);
                }
                if (Mode == GauranlenTreeModeDefOf.Gaumaker && MaxDryads < 3)
                {
                    text = text + "\n" + "ConnectionStrengthTooWeakForGaumakerPod".Translate().Colorize(ColorLibrary.RedReadable);
                }
                string text3 = AffectingBuildingsDescription("ConnectionStrengthAffectedBy");
                if (!text3.NullOrEmpty())
                {
                    text = text + "\n" + text3;
                }
            }
            else if (!text2.NullOrEmpty())
            {
                text += text2;
            }
            if (text.NullOrEmpty())
            {
                return "";
            }
            return text.Trim();
        }

        List<float> list1;
        List<int> list2;
        List<Pawn> list3;
        List<Pawn> list4;
        public override void PostExposeData()
        {
            Scribe_Defs.Look(ref currentMode, "currentMode");
            Scribe_Defs.Look(ref desiredMode, "desiredMode");
            Scribe_Values.Look(ref nextUntornTick, "nextUntornTick", -1);
            Scribe_Values.Look(ref spawnTick, "spawnTick", -1);
            Scribe_Values.Look(ref DesiredConnectionStrength, "DesiredConnectionStrength", 0.5f);
            Scribe_Values.Look(ref lastSubPlantTick, "lastSubPlantTick", -1);
            Scribe_Deep.Look(ref cachedLordJob, "cachedLordJob");
            Scribe_Values.Look(ref lordJobCoolDown, "lordJobCoolDown", 0);
            Scribe_References.Look(ref gaumakerPod, "gaumakerPod");
            Scribe_Collections.Look(ref lastPrunedTicks, "lastPrunedTicks", LookMode.Reference, LookMode.Value, ref list3, ref list2);
            Scribe_Collections.Look(ref connectionStrength, "connectionStrength", LookMode.Reference, LookMode.Value, ref list4, ref list1);
            Scribe_Collections.Look(ref connectedPawns, "connectedPawns", LookMode.Reference);
            Scribe_Collections.Look(ref dryads, "dryads", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                dryads.RemoveAll((Pawn x) => x?.Dead ?? true);
                connectionStrength ??= new Dictionary<Pawn, float>();
                lastPrunedTicks ??= new Dictionary<Pawn, int>();
                connectedPawns ??= new List<Pawn>();
                
            }
        }

    }
}
