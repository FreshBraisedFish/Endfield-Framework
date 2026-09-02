using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace BF_Library
{
    public class BF_HediffCompProperties_PeriodicDamage : HediffCompProperties
    {
        public DamageDef damageDef;
        public float damageAmount = 1f;
        public int intervalTicks = 150;
        public float severityDamageMultiplier;
        public BodyPartDef bodyPartDef;
        public BodyPartHeight bodyPartHeight = BodyPartHeight.Undefined;
        public BodyPartDepth bodyPartDepth = BodyPartDepth.Outside;

        public BF_HediffCompProperties_PeriodicDamage()
        {
            compClass = typeof(BF_HediffComp_PeriodicDamage);
        }

        public override IEnumerable<string> ConfigErrors(HediffDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (damageDef == null)
            {
                yield return "damageDef is null";
            }
            if (intervalTicks <= 0)
            {
                yield return "intervalTicks must be greater than 0";
            }
            if (damageAmount <= 0f && severityDamageMultiplier <= 0f)
            {
                yield return "damageAmount and severityDamageMultiplier are both <= 0; no damage will be dealt";
            }
        }
    }

    public class BF_HediffComp_PeriodicDamage : HediffComp
    {
        public BF_HediffCompProperties_PeriodicDamage Props => (BF_HediffCompProperties_PeriodicDamage)props;

        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            if (!Pawn.Spawned || Pawn.Dead)
            {
                return;
            }
            if (!Pawn.IsHashIntervalTick(Props.intervalTicks, delta))
            {
                return;
            }

            float amount = Props.damageAmount + parent.Severity * Props.severityDamageMultiplier;
            if (amount <= 0f)
            {
                return;
            }
            int finalAmount = GenMath.RoundRandom(amount);

            DamageInfo dinfo = new DamageInfo(Props.damageDef, finalAmount, 0f, -1f, Pawn);
            if (Props.bodyPartDef != null)
            {
                BodyPartRecord part = Pawn.health.hediffSet
                    .GetNotMissingParts(Props.bodyPartHeight, Props.bodyPartDepth, null, null)
                    .FirstOrDefault(p => p.def == Props.bodyPartDef);
                if (part == null)
                {
                    Debug.Log($"[BF_PeriodicDamage] {Pawn.LabelShort} has no {Props.bodyPartDef.defName}, skipping hit");
                    return;
                }
                dinfo.SetHitPart(part);
            }
            else
            {
                dinfo.SetBodyRegion(Props.bodyPartHeight, Props.bodyPartDepth);
            }

            Pawn.TakeDamage(dinfo);
            Debug.Log($"[BF_PeriodicDamage] {Pawn.LabelShort} took {finalAmount} {Props.damageDef.defName} damage from {Def.defName}");
        }
    }
}
