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
        public static int FindLandingIndex(Ability ability)
        {
            if (ability?.EffectComps == null)
            {
                return -1;
            }
            for (int i = 0; i < ability.EffectComps.Count; i++)
            {
                if (ability.EffectComps[i] is BF_CompAbilityEffect_ActivateOnLanding)
                {
                    return i;
                }
            }
            return -1;
        }

        public static void TriggerCastPhase(Ability ability, LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (ability?.EffectComps == null)
            {
                return;
            }
            int landingIndex = FindLandingIndex(ability);
            if (landingIndex < 0)
            {
                Debug.Log($"[BF_ActivateOnLanding] activateOnCast without ActivateOnLanding comp, calling full Activate at cast");
                ability.Activate(target, dest);
                return;
            }
            ApplyComps(ability, 0, landingIndex, target, dest);
            StartCooldownLikePreActivate(ability);
            Debug.Log($"[BF_ActivateOnLanding] Cast phase: applied {landingIndex} comp(s) before ActivateOnLanding");
        }

        public static void ApplyComps(Ability ability, int startIndex, int endExclusive, LocalTargetInfo target, LocalTargetInfo dest)
        {
            if (ability?.EffectComps == null)
            {
                return;
            }
            int count = ability.EffectComps.Count;
            for (int i = startIndex; i < endExclusive && i < count; i++)
            {
                ability.EffectComps[i].Apply(target, dest);
            }
        }

        public static void StartCooldownLikePreActivate(Ability ability)
        {
            if (!ability.HasCooldown || ability.OnCooldown)
            {
                return;
            }
            if (ability.def.groupDef != null)
            {
                int num = ability.def.overrideGroupCooldown ? ability.def.cooldownTicksRange.RandomInRange : ability.def.groupDef.cooldownTicks;
                foreach (Ability other in ability.pawn.abilities.AllAbilitiesForReading)
                {
                    other.Notify_GroupStartedCooldown(ability.def.groupDef, num);
                }
            }
            else if (ability.UsesCharges)
            {
                if (ability.def.cooldownPerCharge)
                {
                    if (ability.RemainingCharges < ability.def.charges && ability.CooldownTicksRemaining == 0)
                    {
                        ability.StartCooldown(ability.def.cooldownTicksRange.RandomInRange);
                    }
                }
                else if (ability.RemainingCharges <= 0)
                {
                    ability.StartCooldown(ability.def.cooldownTicksRange.RandomInRange);
                }
            }
            else
            {
                ability.StartCooldown(ability.def.cooldownTicksRange.RandomInRange);
            }
        }

        public void OnJumpCompleted(IntVec3 origin, LocalTargetInfo target)
        {
            bool splitHandled = false;
            if (parent.verb is BF_Verb_Charge charge && charge.ActivateOnCast)
            {
                int landingIndex = FindLandingIndex(parent);
                if (landingIndex >= 0)
                {
                    int count = parent.EffectComps.Count;
                    ApplyComps(parent, landingIndex + 1, count, target, LocalTargetInfo.Invalid);
                    splitHandled = true;
                    Debug.Log($"[BF_ActivateOnLanding] Landing phase: applied {count - landingIndex - 1} comp(s) after ActivateOnLanding");
                }
            }
            if (!splitHandled)
            {
                Debug.Log($"[BF_ActivateOnLanding] {parent.pawn.LabelShort} landed, calling Activate on ability {parent.def.defName}");
                parent.Activate(target, LocalTargetInfo.Invalid);
            }

            if (parent.verb is BF_Verb_Charge charge2 && charge2.PostChargeStunTicks > 0)
            {
                Debug.Log($"[BF_ActivateOnLanding] Stunning caster for {charge2.PostChargeStunTicks} ticks");
                parent.pawn.stances.stunner.StunFor(charge2.PostChargeStunTicks, parent.pawn, addBattleLog: true, showMote: true);
            }
        }
    }
}
