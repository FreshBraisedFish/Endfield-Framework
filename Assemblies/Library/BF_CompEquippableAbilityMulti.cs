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
        // 只记录“本 comp 实际授予”的技能；卸下时也只移除这些，避免误删其他来源的同名技能。
        private Pawn grantedTo;
        private List<AbilityDef> grantedDefs = new List<AbilityDef>();

        public BF_CompProperties_EquippableAbilityMulti Props => (BF_CompProperties_EquippableAbilityMulti)props;

        // 当前装备该物品的 Pawn（没被装备时为 null）
        private Pawn EquippedPawn => (parent.ParentHolder as Pawn_EquipmentTracker)?.pawn;

        public override void Notify_Equipped(Pawn pawn)
        {
            if (pawn == null || Props.abilityDefs.NullOrEmpty())
            {
                return;
            }

            // 直接换人（理论上原版会先触发 Notify_Unequipped）：先把上一任的授予记录清干净。
            if (grantedTo != null && grantedTo != pawn)
            {
                RemoveGrantedFrom(grantedTo);
            }

            grantedTo = pawn;
            foreach (AbilityDef def in Props.abilityDefs)
            {
                if (def == null || pawn.abilities.abilities.Any((Ability a) => a.def == def))
                {
                    continue;
                }
                pawn.abilities.GainAbility(def);
                grantedDefs.Add(def);
            }
        }

        public override void Notify_Unequipped(Pawn pawn)
        {
            if (pawn == null || grantedTo != pawn)
            {
                return;
            }
            RemoveGrantedFrom(pawn);
        }

        private void RemoveGrantedFrom(Pawn pawn)
        {
            for (int i = grantedDefs.Count - 1; i >= 0; i--)
            {
                AbilityDef def = grantedDefs[i];
                if (def != null)
                {
                    pawn.abilities.RemoveAbility(def);
                }
            }
            grantedDefs.Clear();
            grantedTo = null;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_References.Look(ref grantedTo, "grantedTo");
            Scribe_Collections.Look(ref grantedDefs, "grantedDefs", LookMode.Def);
            if (grantedDefs == null)
            {
                grantedDefs = new List<AbilityDef>();
            }

            // 原版加载装备时不会重新调用 Notify_Equipped，这里按当前装备状态重建记录，
            // 保证读档后卸下武器仍能删掉它授予的技能（兼容没有这两个字段的老存档）。
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Pawn pawn = EquippedPawn;
                if (pawn == null)
                {
                    grantedTo = null;
                    grantedDefs.Clear();
                }
                else if (grantedTo != pawn)
                {
                    grantedTo = pawn;
                    grantedDefs.Clear();
                    foreach (AbilityDef def in Props.abilityDefs)
                    {
                        if (def != null && pawn.abilities.abilities.Any((Ability a) => a.def == def))
                        {
                            grantedDefs.Add(def);
                        }
                    }
                }
            }
        }
    }
}
