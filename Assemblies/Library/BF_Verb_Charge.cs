using RimWorld;
using RimWorld.Utility;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace BF_Library
{
    public class BF_VerbProperties_Charge : VerbProperties
    {
        public bool chargeToTarget;
        public ThingDef chargeFlyerDef;
        public EffecterDef startEffecterDef;
        public int startEffecterTicks = 30;
        public FleckDef startFleckDef;
        public SoundDef startSoundDef;
        public bool stunTargetDuringCharge;
        public int postChargeStunTicks;
        public bool activateOnCast;
        public int delayTicks;
    }

    public class BF_Verb_Charge : Verb_CastAbility
    {
        private BF_VerbProperties_Charge ChargeProps => verbProps as BF_VerbProperties_Charge;

        private bool ChargeToTarget => ChargeProps?.chargeToTarget ?? false;

        private ThingDef ChargeFlyer => ChargeProps?.chargeFlyerDef ?? ThingDefOf.PawnFlyer;

        public int PostChargeStunTicks => ChargeProps?.postChargeStunTicks ?? 0;

        public bool ActivateOnCast => ChargeProps?.activateOnCast ?? false;

        public int ChargeDelayTicks => ChargeProps?.delayTicks ?? 0;

        protected override bool TryCastShot()
        {
            Debug.Log($"[BF_Charge] TryCastShot started: caster={caster}, target={currentTarget}");

            IntVec3 dest = ChargeDestination();
            Debug.Log($"[BF_Charge] ChargeDestination={dest}, isValid={dest.IsValid}");

            if (!dest.IsValid || dest == caster.Position)
            {
                Debug.Log($"[BF_Charge] Invalid destination, aborting charge");
                return false;
            }
            if (ActivateOnCast)
            {
                BF_CompAbilityEffect_ActivateOnLanding.TriggerCastPhase(ability, currentTarget, currentDestination);
            }
            SpawnStartEffects();

            if (ChargeDelayTicks > 0)
            {
                BF_CompAbilityEffect_DelayedJump delayed = ability?.CompOfType<BF_CompAbilityEffect_DelayedJump>();
                if (delayed != null)
                {
                    delayed.Schedule(dest, currentTarget, ChargeDelayTicks);
                    Debug.Log($"[BF_Charge] Delayed jump scheduled in {ChargeDelayTicks} ticks to {dest}");
                    return true;
                }
                Debug.LogWarning("[BF_Charge] delayTicks > 0 but no BF_CompProperties_DelayedJump comp on the ability; jumping immediately");
            }

            DoChargeJump(dest, currentTarget);
            return true;
        }

        public void DoChargeJump(IntVec3 dest, LocalTargetInfo target)
        {
            if (CasterPawn == null || !CasterPawn.Spawned || CasterPawn.Map == null)
            {
                Debug.LogWarning($"[BF_Charge] DoChargeJump skipped: caster not on map");
                return;
            }

            if (ChargeProps?.stunTargetDuringCharge == true && target.Thing is Pawn targetPawn)
            {
                float flightDist = CasterPawn.Position.DistanceTo(dest);
                ThingDef flyerDef = ChargeFlyer;
                float flightSpeed = (flyerDef?.pawnFlyer?.flightSpeed).GetValueOrDefault(12f);
                float flightDurationMin = (flyerDef?.pawnFlyer?.flightDurationMin).GetValueOrDefault(0.5f);
                float flightTime = Mathf.Max(flightDist / flightSpeed, flightDurationMin);
                int stunTicks = Mathf.CeilToInt(flightTime * 60f) + 1;
                targetPawn.stances.stunner.StunFor(stunTicks, CasterPawn, addBattleLog: true, showMote: true);
                Debug.Log($"[BF_Charge] Stunned {targetPawn.LabelShort} for {stunTicks} ticks ({flightTime:F2}s flight)");
            }

            if (CasterPawn?.jobs != null)
            {
                Debug.Log($"[BF_Charge] Ending current job to prevent double-cast after landing");
                CasterPawn.jobs.EndCurrentJob(JobCondition.Succeeded);
            }

            Debug.Log($"[BF_Charge] Starting jump to {dest}");
            JumpUtility.DoJump(
                CasterPawn,
                new LocalTargetInfo(dest),
                base.ReloadableCompSource,
                verbProps,
                ability,
                target,
                ChargeFlyer);
        }

        private void SpawnStartEffects()
        {
            if (ChargeProps == null)
            {
                return;
            }
            if (ChargeProps.startSoundDef != null)
            {
                ChargeProps.startSoundDef.PlayOneShot(new TargetInfo(caster.Position, caster.Map));
            }
            if (ChargeProps.startFleckDef != null)
            {
                FleckMaker.Static(caster.Position, caster.Map, ChargeProps.startFleckDef);
            }
            if (ChargeProps.startEffecterDef != null)
            {
                Effecter effecter = ChargeProps.startEffecterDef.Spawn();
                effecter.Trigger(new TargetInfo(caster.Position, caster.Map), TargetInfo.Invalid);
                ability.AddEffecterToMaintain(effecter, caster.Position, ChargeProps.startEffecterTicks, caster.Map);
            }
        }

        public IntVec3 ChargeDestination()
        {
            if (currentTarget.Cell == caster.Position)
            {
                return caster.Position;
            }
            if (ChargeToTarget)
            {
                return currentTarget.Cell;
            }
            Vector3 dir = (currentTarget.Cell - caster.Position).ToVector3().normalized;
            IntVec3 fullDest = (caster.Position.ToVector3() + dir * verbProps.range).ToIntVec3();
            if (JumpUtility.ValidJumpTarget(CasterPawn, caster.Map, fullDest))
            {
                return fullDest;
            }
            IntVec3 best = caster.Position;
            foreach (IntVec3 cell in GenSight.BresenhamCellsBetween(caster.Position, fullDest))
            {
                if (cell == caster.Position)
                {
                    continue;
                }
                if (!JumpUtility.ValidJumpTarget(CasterPawn, caster.Map, cell))
                {
                    break;
                }
                best = cell;
            }
            return best;
        }

        public override bool CanHitTarget(LocalTargetInfo targ)
        {
            if (!JumpUtility.ValidJumpTarget(CasterPawn, caster.Map, targ.Cell))
            {
                return false;
            }
            if (!CanHitTargetFrom(caster.Position, targ))
            {
                return false;
            }
            return true;
        }

        public override bool CanHitTargetFrom(IntVec3 root, LocalTargetInfo targ)
        {
            return JumpUtility.CanHitTargetFrom(CasterPawn, root, targ, verbProps.range);
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!IsApplicableTo(target, showMessages))
            {
                return false;
            }
            for (int i = 0; i < ability.EffectComps.Count; i++)
            {
                if (!ability.EffectComps[i].Valid(target, showMessages))
                {
                    return false;
                }
            }
            if (!CanHitTargetFrom(caster.Position, target))
            {
                if (showMessages)
                {
                    Messages.Message("CannotUseAbility".Translate(ability.def.label) + ": " + "AbilityOutOfRange".Translate(), new LookTargets(caster, target.ToTargetInfo(caster.Map)), MessageTypeDefOf.RejectInput, historical: false);
                }
                return false;
            }
            return true;
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            if (verbProps.range > 0f)
            {
                verbProps.DrawRadiusRing(caster.Position, this);
            }
            if (CanHitTarget(target))
            {
                GenDraw.DrawTargetHighlightWithLayer(target.CenterVector3, AltitudeLayer.MetaOverlays);
            }
            if (target.IsValid)
            {
                ability.DrawEffectPreviews(target);
            }
        }

        public override void OnGUI(LocalTargetInfo target)
        {
            if (CanHitTarget(target) && IsApplicableTo(target) && ValidateTarget(target, showMessages: false))
            {
                base.OnGUI(target);
            }
            else
            {
                GenUI.DrawMouseAttachment(TexCommand.CannotShoot);
            }
            DrawAttachmentExtraLabel(target);
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            ability.QueueCastingJob(target, null);
        }
    }
}
