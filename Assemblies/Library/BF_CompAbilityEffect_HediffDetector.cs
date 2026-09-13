using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BF_Library
{
    public class BF_CompProperties_HediffDetector : CompProperties_AbilityEffect
    {
        // 检测
        public HediffDef detectedHediffDef;
        public HediffDef extraHediff_1;
        public HediffDef extraHediff_2;
        public HediffDef extraHediff_3;
        public bool requireDetected = true;
        public bool validOnlyIfDetected;

        // 移除检测到的 Hediff
        public bool removeDetectedHediff;
        public bool removeApplyToTarget = true;
        public bool removeApplyToSelf;

        // 结果 Hediff
        public HediffDef resultHediffDef;
        public float severityMultiplier = 1f;
        public float severityOffset = 0f;
        public float missingSeverity = 1f;
        public bool useFixedSeverity;
        public float fixedSeverity = 1f;
        public bool hediffApplyToTarget = true;
        public bool hediffApplyToSelf;

        // 伤害
        public DamageDef damageDef;
        public int baseDamage = 10;
        public float damageSeverityMultiplier = 1f;
        public bool damageApplyToTarget = true;
        public bool damageApplyToSelf;

        public BF_CompProperties_HediffDetector()
        {
            compClass = typeof(BF_CompAbilityEffect_HediffDetector);
        }

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            bool hasDetector = detectedHediffDef != null || extraHediff_1 != null || extraHediff_2 != null || extraHediff_3 != null;
            if (!hasDetector && (requireDetected || validOnlyIfDetected || removeDetectedHediff))
            {
                yield return "no detected hediffDef (detectedHediffDef/extraHediff_1/2/3) is set";
            }
            if (damageDef == null && resultHediffDef == null && !removeDetectedHediff)
            {
                yield return "damageDef and resultHediffDef are both null and removeDetectedHediff is false; the ability will do nothing";
            }
            if (removeDetectedHediff && resultHediffDef != null && resultHediffDef == detectedHediffDef)
            {
                yield return "resultHediffDef equals detectedHediffDef while removeDetectedHediff is true; the hediff would be applied then immediately removed";
            }
        }
    }

    public class BF_CompAbilityEffect_HediffDetector : CompAbilityEffect
    {
        public new BF_CompProperties_HediffDetector Props => (BF_CompProperties_HediffDetector)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn self = parent.pawn;
            Pawn other = target.Pawn;

            if (other == self)
            {
                bool any = Props.removeApplyToSelf || Props.removeApplyToTarget
                    || Props.damageApplyToSelf || Props.damageApplyToTarget
                    || Props.hediffApplyToSelf || Props.hediffApplyToTarget;
                if (self != null && any)
                {
                    ProcessPawn(self,
                        Props.removeApplyToSelf || Props.removeApplyToTarget,
                        Props.damageApplyToSelf || Props.damageApplyToTarget,
                        Props.hediffApplyToSelf || Props.hediffApplyToTarget);
                }
                return;
            }

            if (self != null && (Props.removeApplyToSelf || Props.damageApplyToSelf || Props.hediffApplyToSelf))
            {
                ProcessPawn(self, Props.removeApplyToSelf, Props.damageApplyToSelf, Props.hediffApplyToSelf);
            }
            if (other != null && (Props.removeApplyToTarget || Props.damageApplyToTarget || Props.hediffApplyToTarget))
            {
                ProcessPawn(other, Props.removeApplyToTarget, Props.damageApplyToTarget, Props.hediffApplyToTarget);
            }
        }

        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (!base.Valid(target, throwMessages))
            {
                return false;
            }
            if (Props.validOnlyIfDetected && target.Pawn != null)
            {
                List<Hediff> detected = GetDetected(target.Pawn);
                if (detected.Count == 0)
                {
                    if (throwMessages)
                    {
                        Messages.Message($"{parent.pawn.LabelShort}'s ability requires one of the detected hediffs on the target.", parent.pawn, MessageTypeDefOf.RejectInput, historical: false);
                    }
                    return false;
                }
            }
            return true;
        }

        private List<Hediff> GetDetected(Pawn pawn)
        {
            return BF_HediffUtility.GetDetectedHediffs(pawn, Props.detectedHediffDef, Props.extraHediff_1, Props.extraHediff_2, Props.extraHediff_3);
        }

        private void ProcessPawn(Pawn pawn, bool doRemove, bool doDamage, bool doHediff)
        {
            if (pawn == null || !pawn.Spawned)
            {
                return;
            }

            List<Hediff> detected = GetDetected(pawn);
            bool hasDetected = detected.Count > 0;

            if (Props.requireDetected && !hasDetected)
            {
                return;
            }

            float totalSeverity = BF_HediffUtility.SumSeverity(detected);

            if (doDamage && Props.damageDef != null)
            {
                int totalDamage = Props.baseDamage + Mathf.RoundToInt(totalSeverity * Props.damageSeverityMultiplier);
                DamageInfo dinfo = new DamageInfo(Props.damageDef, totalDamage, 0f, -1f, parent.pawn);
                dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
                pawn.TakeDamage(dinfo);
            }

            if (doHediff && Props.resultHediffDef != null)
            {
                float newSeverity = Props.useFixedSeverity
                    ? Props.fixedSeverity
                    : (hasDetected ? totalSeverity : Props.missingSeverity) * Props.severityMultiplier + Props.severityOffset;

                Hediff result = pawn.health.hediffSet.GetFirstHediffOfDef(Props.resultHediffDef);
                if (result == null)
                {
                    result = HediffMaker.MakeHediff(Props.resultHediffDef, pawn);
                    result.Severity = newSeverity;
                    pawn.health.AddHediff(result);
                }
                else
                {
                    result.Severity = newSeverity;
                }
            }

            if (doRemove && Props.removeDetectedHediff && hasDetected)
            {
                for (int i = 0; i < detected.Count; i++)
                {
                    pawn.health.RemoveHediff(detected[i]);
                }
            }
        }
    }
}
