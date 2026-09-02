using RimWorld;
using UnityEngine;
using Verse;

namespace BF_Library
{
    public class BF_CompProperties_DelayedJump : CompProperties_AbilityEffect
    {
        public BF_CompProperties_DelayedJump()
        {
            compClass = typeof(BF_CompAbilityEffect_DelayedJump);
        }
    }

    public class BF_CompAbilityEffect_DelayedJump : CompAbilityEffect
    {
        private bool pending;
        private int ticksLeft;
        private IntVec3 destCell;
        private LocalTargetInfo abilityTarget;

        public void Schedule(IntVec3 dest, LocalTargetInfo target, int ticks)
        {
            destCell = dest;
            abilityTarget = target;
            ticksLeft = ticks;
            pending = true;
            Debug.Log($"[BF_DelayedJump] Scheduled jump in {ticks} ticks to {dest}");
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
            if (parent.verb is BF_Verb_Charge charge)
            {
                charge.DoChargeJump(destCell, abilityTarget);
            }
            else
            {
                Debug.LogWarning("[BF_DelayedJump] Ability verb is not BF_Verb_Charge, jump aborted");
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref pending, "pending");
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
            Scribe_Values.Look(ref destCell, "destCell");
            Scribe_TargetInfo.Look(ref abilityTarget, "abilityTarget");
        }
    }
}
