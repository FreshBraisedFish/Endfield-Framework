using RimWorld;
using UnityEngine;
using Verse;

namespace BF_Library
{
    public class BF_CompProperties_ActivateOnLanding : CompProperties_AbilityEffect
    {
        public BF_CompProperties_ActivateOnLanding()
        {
            compClass = typeof(BF_CompAbilityEffect_ActivateOnLanding);
        }
    }

    public class BF_CompAbilityEffect_ActivateOnLanding : CompAbilityEffect, ICompAbilityEffectOnJumpCompleted
    {
        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            Debug.Log($"[BF_ActivateOnLanding] {parent.pawn.LabelShort} landed, calling Activate on ability {parent.def.defName}");
            parent.Activate(target, LocalTargetInfo.Invalid);

            if (parent.verb is BF_Verb_Charge charge && charge.PostChargeStunTicks > 0)
            {
                Debug.Log($"[BF_ActivateOnLanding] Stunning caster for {charge.PostChargeStunTicks} ticks");
                parent.pawn.stances.stunner.StunFor(charge.PostChargeStunTicks, parent.pawn, addBattleLog: true, showMote: true);
            }
        }
    }
}
