using RimWorld;
using UnityEngine;
using Verse;

namespace BF_Library
{
    public class BF_CompProperties_RemoveAndDamage : CompProperties_AbilityEffect
    {
        public HediffDef hediffDef;
        public DamageDef damageDef;
        public int baseDamage = 10;
        public float severityMultiplier = 1f;
        public bool applyToTarget = true;
        public bool applyToSelf;

        public BF_CompProperties_RemoveAndDamage()
        {
            compClass = typeof(BF_CompAbilityEffect_RemoveAndDamage);
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

            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(Props.hediffDef);
            if (hediff == null)
            {
                Debug.Log($"[BF_RemoveAndDamage] {pawn.LabelShort} has no hediff {Props.hediffDef.defName}, skipping");
                return;
            }

            float severity = hediff.Severity;
            pawn.health.RemoveHediff(hediff);

            int totalDamage = Props.baseDamage + Mathf.RoundToInt(severity * Props.severityMultiplier);

            DamageInfo dinfo = new DamageInfo(Props.damageDef, totalDamage, 0f, -1f, parent.pawn);
            dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
            pawn.TakeDamage(dinfo);

            Debug.Log($"[BF_RemoveAndDamage] Removed {Props.hediffDef.defName} (severity={severity:F2}) from {pawn.LabelShort}, dealt {totalDamage} {Props.damageDef.defName} damage");
        }
    }
}
