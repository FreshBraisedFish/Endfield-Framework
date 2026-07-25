using RimWorld;
using Verse;
using Verse.AI;

namespace BF_Library
{
    public class BF_Verb_MultiHit : Verb_CastAbility
    {
        private int shotsFired;

        protected override bool TryCastShot()
        {
            FireProjectile();
            shotsFired++;
            if (shotsFired >= verbProps.burstShotCount)
            {
                shotsFired = 0;
                return ability.Activate(currentTarget, currentDestination);
            }
            return true;
        }

        private void FireProjectile()
        {
            ThingDef projectileDef = verbProps.defaultProjectile;
            if (projectileDef == null)
            {
                return;
            }
            IntVec3 cell = currentTarget.Cell;
            if (!cell.InBounds(caster.Map))
            {
                return;
            }
            Projectile projectile = (Projectile)GenSpawn.Spawn(
                projectileDef, cell, caster.Map);
            projectile.Launch(
                caster, cell.ToVector3Shifted(),
                currentTarget, currentTarget,
                ProjectileHitFlags.IntendedTarget,
                false, EquipmentSource);
        }

        public override void Reset()
        {
            base.Reset();
            shotsFired = 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref shotsFired, "shotsFired");
        }
    }
}
