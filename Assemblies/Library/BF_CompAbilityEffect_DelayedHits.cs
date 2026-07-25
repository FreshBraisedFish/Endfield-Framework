using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace BF_Library
{
    public enum ProjectileSpawnPosition
    {
        Target,
        Caster
    }

    public class BF_CompProperties_DelayedHits : CompProperties_AbilityEffect
    {
        public int delayTicks = 60;
        public int hitCount = 1;
        public int hitInterval;
        public ThingDef projectileDef;
        public ProjectileSpawnPosition spawnPosition;
        public Vector3 spawnOffset;
        public float spreadRadius;

        public BF_CompProperties_DelayedHits()
        {
            compClass = typeof(BF_CompAbilityEffect_DelayedHits);
        }

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            if (compClass == null)
            {
                yield return "compClass is null";
            }
        }
    }
    public class BF_CompProperties_DelayedHits_Extra_1 : BF_CompProperties_DelayedHits
    {
        public BF_CompProperties_DelayedHits_Extra_1()
        {
            compClass = typeof(BF_CompAbilityEffect_DelayedHits);
        }
    }

    public class BF_CompProperties_DelayedHits_Extra_2 : BF_CompProperties_DelayedHits
    {
        public BF_CompProperties_DelayedHits_Extra_2()
        {
            compClass = typeof(BF_CompAbilityEffect_DelayedHits);
        }
    }

    public class BF_CompAbilityEffect_DelayedHits : CompAbilityEffect
    {
        private int ticksLeft;
        private int hitsRemaining;
        private int tickBetweenHits;
        private LocalTargetInfo pendingTarget;

        public new BF_CompProperties_DelayedHits Props => (BF_CompProperties_DelayedHits)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Debug.Log($"[BF_DelayedHits] Apply called: target={target}, delayTicks={Props.delayTicks}, hitCount={Props.hitCount}");
            pendingTarget = target;
            ticksLeft = Props.delayTicks;
            hitsRemaining = Props.hitCount;
            tickBetweenHits = Props.hitInterval;
        }

        public override void CompTick()
        {
            if (hitsRemaining <= 0)
            {
                return;
            }
            ticksLeft--;
            if (ticksLeft > 0)
            {
                return;
            }
            FireHit();
            hitsRemaining--;
            ticksLeft = tickBetweenHits;
        }

        private void FireHit()
        {
            ThingDef projectileDef = Props.projectileDef;
            if (projectileDef == null)
            {
                Debug.LogWarning($"[BF_DelayedHits] FireHit skipped: no projectileDef");
                return;
            }
            Pawn pawn = parent.pawn;
            if (!pawn.Spawned || !pendingTarget.IsValid)
            {
                Debug.LogWarning($"[BF_DelayedHits] FireHit skipped: pawn spawned={pawn.Spawned}, target valid={pendingTarget.IsValid}");
                return;
            }

            Vector3 basePos = Props.spawnPosition == ProjectileSpawnPosition.Caster
                ? pawn.DrawPos
                : pendingTarget.Cell.ToVector3Shifted();
            Vector3 spawnPos = basePos + Props.spawnOffset;
            if (Props.spreadRadius > 0f)
            {
                Vector2 r = Rand.InsideUnitCircle * Props.spreadRadius;
                spawnPos += new Vector3(r.x, 0f, r.y);
            }
            IntVec3 spawnCell = spawnPos.ToIntVec3();

            if (!spawnCell.InBounds(pawn.Map))
            {
                Debug.LogWarning($"[BF_DelayedHits] FireHit skipped: spawn cell {spawnCell} out of bounds");
                return;
            }
            Debug.Log($"[BF_DelayedHits] FireHit: projectile={projectileDef.defName}, spawn={Props.spawnPosition}({spawnCell}), target={pendingTarget}, remaining={hitsRemaining}");
            Projectile projectile = (Projectile)GenSpawn.Spawn(
                projectileDef, spawnCell, pawn.Map);
            projectile.Launch(
                pawn, spawnPos,
                pendingTarget, pendingTarget,
                ProjectileHitFlags.IntendedTarget,
                false, parent.verb?.EquipmentSource);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft");
            Scribe_Values.Look(ref hitsRemaining, "hitsRemaining");
            Scribe_Values.Look(ref tickBetweenHits, "tickBetweenHits");
            Scribe_TargetInfo.Look(ref pendingTarget, "pendingTarget");
        }
    }
}
