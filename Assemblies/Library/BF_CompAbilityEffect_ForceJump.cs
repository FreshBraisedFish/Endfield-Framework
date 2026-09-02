using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace BF_Library
{
    public enum BF_JumpDestination
    {
        Caster,
        Target
    }

    public class BF_CompProperties_ForceJump : CompProperties_AbilityEffect
    {
        public BF_JumpDestination destination = BF_JumpDestination.Caster;
        public float offsetRadius;
        public ThingDef pawnFlyerDef;
        public bool stunDuringFlight = true;
        public int postLandingStunTicks;
        public bool endCurrentJob;
        public int delayTicks;

        public BF_CompProperties_ForceJump()
        {
            compClass = typeof(BF_CompAbilityEffect_ForceJump);
        }
    }

    public class BF_CompAbilityEffect_ForceJump : CompAbilityEffect
    {
        public new BF_CompProperties_ForceJump Props => (BF_CompProperties_ForceJump)props;

        private bool pending;
        private int ticksLeft;
        private LocalTargetInfo pendingTarget;

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }
            return target.Pawn != null;
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn targetPawn = target.Pawn;
            if (targetPawn == null || !targetPawn.Spawned || targetPawn.Map == null)
            {
                Debug.LogWarning($"[BF_ForceJump] No valid target pawn, skipping");
                return;
            }

            if (Props.delayTicks > 0)
            {
                pendingTarget = target;
                ticksLeft = Props.delayTicks;
                pending = true;
                Debug.Log($"[BF_ForceJump] Scheduled jump for {targetPawn.LabelShort} in {Props.delayTicks} ticks");
                return;
            }

            DoJump(targetPawn);
        }

        public override void CompTick()
        {
            if (!pending)
            {
                return;
            }
            ticksLeft--;
            if (ticksLeft > 0)
            {
                return;
            }
            pending = false;
            Pawn targetPawn = pendingTarget.Pawn;
            if (targetPawn == null || !targetPawn.Spawned || targetPawn.Map == null)
            {
                Debug.LogWarning($"[BF_ForceJump] Delayed jump skipped: target no longer valid");
                return;
            }
            DoJump(targetPawn);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref pending, "pending");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
            Scribe_TargetInfo.Look(ref pendingTarget, "pendingTarget");
        }

        private void DoJump(Pawn targetPawn)
        {
            IntVec3 baseCell = Props.destination == BF_JumpDestination.Caster
                ? parent.pawn.Position
                : targetPawn.Position;
            IntVec3 jumpCell = ResolveJumpCell(targetPawn, baseCell);

            if (Props.endCurrentJob && targetPawn.CurJob != null)
            {
                Debug.Log($"[BF_ForceJump] Ending {targetPawn.LabelShort}'s job {targetPawn.CurJob.def.defName}");
                targetPawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }

            StunTarget(targetPawn, jumpCell);

            VerbProperties verbProps = parent.verb?.verbProps ?? new VerbProperties();
            bool success = JumpUtility.DoJump(targetPawn, new LocalTargetInfo(jumpCell), null, verbProps, null, LocalTargetInfo.Invalid, Props.pawnFlyerDef);
            Debug.Log($"[BF_ForceJump] {targetPawn.LabelShort} jumped to {jumpCell} (base={baseCell}, success={success})");
        }

        private IntVec3 ResolveJumpCell(Pawn targetPawn, IntVec3 baseCell)
        {
            Map map = targetPawn.Map;

            if (Props.offsetRadius > 0f)
            {
                Vector2 r = Rand.InsideUnitCircle * Props.offsetRadius;
                IntVec3 desired = (baseCell.ToVector3Shifted() + new Vector3(r.x, 0f, r.y)).ToIntVec3();
                if (IsFreeJumpCell(targetPawn, map, desired))
                {
                    return desired;
                }
            }

            int count = GenRadial.NumCellsInRadius(Mathf.Max(Props.offsetRadius, 1f));
            for (int i = 0; i < count; i++)
            {
                IntVec3 cell = baseCell + GenRadial.RadialPattern[i];
                if (IsFreeJumpCell(targetPawn, map, cell))
                {
                    return cell;
                }
            }
            return targetPawn.Position;
        }

        private bool IsFreeJumpCell(Pawn pawn, Map map, IntVec3 cell)
        {
            return cell.InBounds(map)
                && JumpUtility.ValidJumpTarget(pawn, map, cell)
                && !map.thingGrid.CellContains(cell, ThingCategory.Pawn);
        }

        private void StunTarget(Pawn targetPawn, IntVec3 jumpCell)
        {
            if (!Props.stunDuringFlight && Props.postLandingStunTicks <= 0)
            {
                return;
            }

            float flightDist = targetPawn.Position.DistanceTo(jumpCell);
            ThingDef flyerDef = Props.pawnFlyerDef ?? ThingDefOf.PawnFlyer;
            float flightSpeed = (flyerDef?.pawnFlyer?.flightSpeed).GetValueOrDefault(12f);
            float flightDurationMin = (flyerDef?.pawnFlyer?.flightDurationMin).GetValueOrDefault(0.5f);
            float flightTime = Mathf.Max(flightDist / flightSpeed, flightDurationMin);

            int stunTicks = 0;
            if (Props.stunDuringFlight)
            {
                stunTicks += Mathf.CeilToInt(flightTime * 60f) + 1;
            }
            stunTicks += Props.postLandingStunTicks;
            if (stunTicks <= 0)
            {
                return;
            }
            targetPawn.stances.stunner.StunFor(stunTicks, parent.pawn, addBattleLog: true, showMote: true);
            Debug.Log($"[BF_ForceJump] Stunned {targetPawn.LabelShort} for {stunTicks} ticks (flight={flightTime:F2}s)");
        }
    }
}
