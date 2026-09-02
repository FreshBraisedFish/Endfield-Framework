using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BF_Library
{
    public class BF_CompProperties_RemoveAndDamage : CompProperties_AbilityEffect
    {
        public HediffDef hediffDef;
        public HediffDef extraHediff_1;
        public HediffDef extraHediff_2;
        public HediffDef extraHediff_3;
        public DamageDef damageDef;
        public int baseDamage = 10;
        public float severityMultiplier = 1f;
        public bool applyToTarget = true;
        public bool applyToSelf;

        public BF_CompProperties_RemoveAndDamage()
        {
            compClass = typeof(BF_CompAbilityEffect_RemoveAndDamage);
        }

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (hediffDef == null && extraHediff_1 == null && extraHediff_2 == null && extraHediff_3 == null)
            {
                yield return "at least one detected hediffDef (hediffDef/extraHediff_1/2/3) must be set";
            }
            if (damageDef == null)
            {
                yield return "damageDef is null";
            }
        }
    }

    public class BF_CompAbilityEffect_RemoveAndDamage : CompAbilityEffect
    {
        public new BF_CompProperties_RemoveAndDamage Props => (BF_CompProperties_RemoveAndDamage)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (Props.applyToSelf)
            {
                ProcessPawn(parent.pawn, Props.applyToTarget ? target.Pawn : null);
            }
            else if (Props.applyToTarget && target.Pawn != null && target.Pawn != parent.pawn)
            {
                ProcessPawn(target.Pawn, null);
            }
        }

        private void ProcessPawn(Pawn pawn, Pawn secondaryPawn)
        {
            if (pawn == null || !pawn.Spawned)
            {
                return;
            }

            List<Hediff> detected = BF_HediffUtility.GetDetectedHediffs(pawn, Props.hediffDef, Props.extraHediff_1, Props.extraHediff_2, Props.extraHediff_3);
            if (detected.Count == 0)
            {
                Debug.Log($"[BF_RemoveAndDamage] {pawn.LabelShort} has none of the detected hediffs, skipping");
                return;
            }

            float totalSeverity = BF_HediffUtility.SumSeverity(detected);
            for (int i = 0; i < detected.Count; i++)
            {
                pawn.health.RemoveHediff(detected[i]);
            }

            int totalDamage = Props.baseDamage + Mathf.RoundToInt(totalSeverity * Props.severityMultiplier);

            DamageInfo dinfo = new DamageInfo(Props.damageDef, totalDamage, 0f, -1f, parent.pawn);
            dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
            pawn.TakeDamage(dinfo);

            Debug.Log($"[BF_RemoveAndDamage] Removed {detected.Count} hediff(s) (severity={totalSeverity:F2}) from {pawn.LabelShort}, dealt {totalDamage} {Props.damageDef.defName} damage");
        }
    }
}
