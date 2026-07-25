using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace BF_Library
{
    public class BF_CompProperties_EquippableAbilityMulti : CompProperties
    {
        public List<AbilityDef> abilityDefs;

        public BF_CompProperties_EquippableAbilityMulti()
        {
            compClass = typeof(BF_CompEquippableAbilityMulti);
        }
    }

    public class BF_CompEquippableAbilityMulti : ThingComp
    {
        public BF_CompProperties_EquippableAbilityMulti Props => (BF_CompProperties_EquippableAbilityMulti)props;

        public override void Notify_Equipped(Pawn pawn)
        {
            if (Props.abilityDefs.NullOrEmpty())
            {
                return;
            }
            foreach (AbilityDef def in Props.abilityDefs)
            {
                if (def != null && !pawn.abilities.abilities.Any((Ability a) => a.def == def))
                {
                    pawn.abilities.GainAbility(def);
                }
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            if (Props.abilityDefs.NullOrEmpty())
            {
                return;
            }
            foreach (AbilityDef def in Props.abilityDefs)
            {
                if (def != null)
                {
                    pawn.abilities.RemoveAbility(def);
                }
            }
        }
    }
}
