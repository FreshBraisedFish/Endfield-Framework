using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BF_Library
{
    public class BF_CompProperties_HediffDetect : CompProperties_AbilityEffect
    {
        public HediffDef detectedHediffDef;
        public HediffDef extraHediff_1;
        public HediffDef extraHediff_2;
        public HediffDef extraHediff_3;
        public HediffDef resultHediffDef;
        public float severityMultiplier = 1f;
        public float severityOffset = 0f;
        public float missingSeverity = 1f;
        public bool useFixedSeverity;
        public float fixedSeverity = 1f;
        public bool applyToTarget = true;
        public bool applyToSelf;
        public bool requireDetected = true;
        public bool removeDetectedHediff;
        public bool validOnlyIfDetected;

        public BF_CompProperties_HediffDetect()
        {
            compClass = typeof(BF_CompAbilityEffect_HediffDetect);
        }

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (detectedHediffDef == null && extraHediff_1 == null && extraHediff_2 == null && extraHediff_3 == null && (requireDetected || removeDetectedHediff))
            {
                yield return "no detected hediffDef (detectedHediffDef/extraHediff_1/2/3) is set";
            }
            if (resultHediffDef == null && !removeDetectedHediff)
            {
                yield return "resultHediffDef is null and removeDetectedHediff is false; the ability will do nothing";
            }
            if (removeDetectedHediff && resultHediffDef != null && resultHediffDef == detectedHediffDef)
            {
                yield return "resultHediffDef equals detectedHediffDef while removeDetectedHediff is true; the hediff would be applied then immediately removed";
            }
        }
    }

    public class BF_CompAbilityEffect_HediffDetect : CompAbilityEffect
    {
        public new BF_CompProperties_HediffDetect Props => (BF_CompProperties_HediffDetect)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            if (Props.applyToSelf)
            {
                ProcessPawn(parent.pawn);
            }
            if (Props.applyToTarget && target.Pawn != null && target.Pawn != parent.pawn)
            {
                ProcessPawn(target.Pawn);
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
                List<Hediff> detected = BF_HediffUtility.GetDetectedHediffs(target.Pawn, Props.detectedHediffDef, Props.extraHediff_1, Props.extraHediff_2, Props.extraHediff_3);
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

        private void ProcessPawn(Pawn pawn)
        {
            if (pawn == null || !pawn.Spawned)
            {
                return;
            }

            List<Hediff> detected = BF_HediffUtility.GetDetectedHediffs(pawn, Props.detectedHediffDef, Props.extraHediff_1, Props.extraHediff_2, Props.extraHediff_3);
            bool hasDetected = detected.Count > 0;

            if (Props.requireDetected && !hasDetected)
            {
                Debug.Log($"[BF_HediffDetect] {pawn.LabelShort} has none of the detected hediffs, skipping");
                return;
            }

            float newSeverity = Props.useFixedSeverity
                ? Props.fixedSeverity
                : (hasDetected ? BF_HediffUtility.SumSeverity(detected) : Props.missingSeverity) * Props.severityMultiplier + Props.severityOffset;

            if (Props.resultHediffDef != null)
            {
                Hediff result = pawn.health.hediffSet.GetFirstHediffOfDef(Props.resultHediffDef);
                if (result == null)
                {
                    result = HediffMaker.MakeHediff(Props.resultHediffDef, pawn);
                    result.Severity = newSeverity;
                    pawn.health.AddHediff(result);
                    Debug.Log($"[BF_HediffDetect] Applied {Props.resultHediffDef.defName} (severity={newSeverity:F2}) to {pawn.LabelShort}");
                }
                else
                {
                    result.Severity = newSeverity;
                    Debug.Log($"[BF_HediffDetect] Set {Props.resultHediffDef.defName} severity to {newSeverity:F2} on {pawn.LabelShort}");
                }
            }
            else
            {
                Debug.Log($"[BF_HediffDetect] No resultHediffDef, only removing {Props.detectedHediffDef?.defName ?? "null"} if present");
            }

            if (Props.removeDetectedHediff && hasDetected)
            {
                for (int i = 0; i < detected.Count; i++)
                {
                    pawn.health.RemoveHediff(detected[i]);
                }
                Debug.Log($"[BF_HediffDetect] Removed {detected.Count} detected hediff(s) from {pawn.LabelShort}");
            }
        }
    }
}
