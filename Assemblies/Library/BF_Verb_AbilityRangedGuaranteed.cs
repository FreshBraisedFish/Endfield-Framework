using RimWorld;
using Verse;
using Verse.AI;

namespace BF_Library
{
    public class BF_Verb_AbilityRangedGuaranteed : Verb_CastAbility
    {
        protected override bool TryCastShot()
        {
            FireProjectile();
            return ability.Activate(currentTarget, currentDestination);
        }

        protected void FireProjectile()
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
    }
}
