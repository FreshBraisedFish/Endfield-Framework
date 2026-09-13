using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BF_Library
{
    public class BF_CompProperties_Charge : CompProperties_AbilityEffect
    {
        public bool chargeToTarget;
        public ThingDef chargeFlyerDef;
        public EffecterDef startEffecterDef;
        public int startEffecterTicks = 30;
        public FleckDef startFleckDef;
        public SoundDef startSoundDef;
        public bool stunTargetDuringCharge;
        public int postChargeStunTicks;
        public int delayTicks;
        public int chargeCount = 1;
        public int chargeInterval;
        public Vector3 landingOffset;
        public float spreadRadius;

        public BF_CompProperties_Charge()
        {
            compClass = typeof(BF_CompAbilityEffect_Charge);
        }
    }

    public class BF_CompAbilityEffect_Charge : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
    {
        private new BF_CompProperties_Charge Props => (BF_CompProperties_Charge)props;

        private bool pending;
        private int ticksLeft;
        private IntVec3 destCell;
        private LocalTargetInfo chargeTarget;
        private int chargesRemaining;
        // 仅用于安全序列化，见 BF_TargetInfoScribe
        private IntVec3 chargeTargetCell = IntVec3.Invalid;
        private Thing chargeTargetThing;

        private ThingDef ChargeFlyer => Props.chargeFlyerDef ?? ThingDefOf.PawnFlyer;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return;
            }

            chargeTarget = target;
            chargesRemaining = Mathf.Max(1, Props.chargeCount);

            IntVec3 destination = ComputeLanding(pawn, target);
            if (!destination.IsValid || destination == pawn.Position)
            {
                return;
            }

            SpawnStartEffects(pawn);
            destCell = destination;

            if (Props.delayTicks > 0)
            {
                ticksLeft = Props.delayTicks;
                pending = true;
                return;
            }

            StartNextCharge(pawn, firstHop: true);
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
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return;
            }
            bool firstHop = chargesRemaining == Mathf.Max(1, Props.chargeCount);
            StartNextCharge(pawn, firstHop);
        }

        private void StartNextCharge(Pawn pawn, bool firstHop)
        {
            if (chargesRemaining <= 0)
            {
                chargeTarget = LocalTargetInfo.Invalid;
                return;
            }
            chargesRemaining--;
            DoChargeJump(pawn, destCell, chargeTarget, firstHop);
        }

        private void DoChargeJump(Pawn pawn, IntVec3 destination, LocalTargetInfo target, bool firstHop)
        {
            if (pawn == null || !pawn.Spawned || pawn.Map == null)
            {
                return;
            }

            if (firstHop && Props.stunTargetDuringCharge && target.Thing is Pawn targetPawn)
            {
                float flightDist = pawn.Position.DistanceTo(destination);
                ThingDef flyerDef = ChargeFlyer;
                float flightSpeed = (flyerDef?.pawnFlyer?.flightSpeed).GetValueOrDefault(12f);
                float flightDurationMin = (flyerDef?.pawnFlyer?.flightDurationMin).GetValueOrDefault(0.5f);
                float flightTime = Mathf.Max(flightDist / flightSpeed, flightDurationMin);
                int stunTicks = Mathf.CeilToInt(flightTime * 60f) + 1;
                targetPawn.stances.stunner.StunFor(stunTicks, pawn, addBattleLog: true, showMote: true);
            }

            if (pawn.jobs != null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
            }

            JumpUtility.DoJump(
                pawn,
                new LocalTargetInfo(destination),
                parent.verb?.ReloadableCompSource,
                parent.verb?.verbProps,
                parent,
                target,
                ChargeFlyer);
        }

        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || !pawn.Spawned)
            {
                return;
            }

            if (chargesRemaining > 0)
            {
                IntVec3 next = ComputeLanding(pawn, chargeTarget);
                if (next.IsValid && next != pawn.Position)
                {
                    destCell = next;
                    if (Props.chargeInterval > 0)
                    {
                        ticksLeft = Props.chargeInterval;
                        pending = true;
                    }
                    else
                    {
                        StartNextCharge(pawn, firstHop: false);
                    }
                    return;
                }
                chargesRemaining = 0;
            }

            if (Props.postChargeStunTicks > 0)
            {
                pawn.stances.stunner.StunFor(Props.postChargeStunTicks, pawn, addBattleLog: true, showMote: true);
            }
            ClearTargetIfDone();
        }

        private void ClearTargetIfDone()
        {
            if (chargesRemaining <= 0)
            {
                chargeTarget = LocalTargetInfo.Invalid;
            }
        }

        private void SpawnStartEffects(Pawn pawn)
        {
            if (Props.startSoundDef != null)
            {
                Props.startSoundDef.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            }
            if (Props.startFleckDef != null)
            {
                FleckMaker.Static(pawn.Position, pawn.Map, Props.startFleckDef);
            }
            if (Props.startEffecterDef != null)
            {
                Effecter effecter = Props.startEffecterDef.Spawn();
                effecter.Trigger(new TargetInfo(pawn.Position, pawn.Map), TargetInfo.Invalid);
                parent.AddEffecterToMaintain(effecter, pawn.Position, Props.startEffecterTicks, pawn.Map);
            }
        }

        private IntVec3 ComputeLanding(Pawn pawn, LocalTargetInfo target)
        {
            IntVec3 baseDest = ChargeDestination(pawn, target);
            Vector3 pos = baseDest.ToVector3Shifted() + Props.landingOffset;
            if (Props.spreadRadius > 0f)
            {
                Vector2 r = Rand.InsideUnitCircle * Props.spreadRadius;
                pos += new Vector3(r.x, 0f, r.y);
            }
            IntVec3 cell = pos.ToIntVec3();
            if (JumpUtility.ValidJumpTarget(pawn, pawn.Map, cell))
            {
                return cell;
            }
            return baseDest;
        }

        private IntVec3 ChargeDestination(Pawn pawn, LocalTargetInfo target)
        {
            if (target.Cell == pawn.Position)
            {
                return pawn.Position;
            }
            if (Props.chargeToTarget)
            {
                return target.Cell;
            }
            float range = parent.verb?.verbProps?.range ?? 0f;
            Vector3 dir = (target.Cell - pawn.Position).ToVector3().normalized;
            IntVec3 fullDest = (pawn.Position.ToVector3() + dir * range).ToIntVec3();
            if (JumpUtility.ValidJumpTarget(pawn, pawn.Map, fullDest))
            {
                return fullDest;
            }
            IntVec3 best = pawn.Position;
            foreach (IntVec3 cell in GenSight.BresenhamCellsBetween(pawn.Position, fullDest))
            {
                if (cell == pawn.Position)
                {
                    continue;
                }
                if (!JumpUtility.ValidJumpTarget(pawn, pawn.Map, cell))
                {
                    break;
                }
                best = cell;
            }
            return best;
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Map == null)
            {
                return false;
            }
            if (!JumpUtility.ValidJumpTarget(pawn, pawn.Map, target.Cell))
            {
                return false;
            }
            return JumpUtility.CanHitTargetFrom(pawn, pawn.Position, target, parent.verb?.verbProps?.range ?? 0f);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Map == null)
            {
                return;
            }
            if (JumpUtility.ValidJumpTarget(pawn, pawn.Map, target.Cell))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref pending, "pending");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
            Scribe_Values.Look(ref destCell, "destCell");
            Scribe_Values.Look(ref chargesRemaining, "chargesRemaining");
            BF_TargetInfoScribe.Look(ref chargeTarget, ref chargeTargetCell, ref chargeTargetThing, "chargeTarget");
        }
    }
}
