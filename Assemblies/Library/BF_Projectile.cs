using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace BF_Library
{
    public interface IBF_ProjectileDamageOverride
    {
        int DamageAmountOverride { get; set; }
        float DamageMultiplier { get; set; }
    }

    public class BF_Projectile : Projectile, IBF_ProjectileDamageOverride
    {
        public int DamageAmountOverride { get; set; } = -1;
        public float DamageMultiplier { get; set; } = 1f;

        public override int DamageAmount => DamageAmountOverride >= 0
            ? DamageAmountOverride
            : Mathf.RoundToInt(base.DamageAmount * DamageMultiplier);
    }

    public class BF_Bullet : Bullet, IBF_ProjectileDamageOverride
    {
        public int DamageAmountOverride { get; set; } = -1;
        public float DamageMultiplier { get; set; } = 1f;

        public override int DamageAmount => DamageAmountOverride >= 0
            ? DamageAmountOverride
            : Mathf.RoundToInt(base.DamageAmount * DamageMultiplier);
    }

    public class BF_Projectile_Explosive : Projectile_Explosive, IBF_ProjectileDamageOverride
    {
        public int DamageAmountOverride { get; set; } = -1;
        public float DamageMultiplier { get; set; } = 1f;

        public override int DamageAmount => DamageAmountOverride >= 0
            ? DamageAmountOverride
            : Mathf.RoundToInt(base.DamageAmount * DamageMultiplier);
    }

    public static class BF_ProjectileUtility
    {
        public static Projectile Spawn(ThingDef projectileDef, IntVec3 cell, Map map, DamageDef damageDefOverride = null, int damageAmountOverride = -1, float damageMultiplier = 1f)
        {
            if (projectileDef == null || map == null || !cell.InBounds(map))
            {
                return null;
            }

            bool overrideAmount = damageAmountOverride >= 0 || !Mathf.Approximately(damageMultiplier, 1f);
            Projectile projectile;

            if (overrideAmount)
            {
                Type overrideType = ResolveOverrideType(projectileDef.thingClass);
                if (overrideType != null)
                {
                    Type originalType = projectileDef.thingClass;
                    projectileDef.thingClass = overrideType;
                    try
                    {
                        projectile = (Projectile)ThingMaker.MakeThing(projectileDef);
                    }
                    finally
                    {
                        projectileDef.thingClass = originalType;
                    }
                    GenSpawn.Spawn(projectile, cell, map);
                    if (projectile is IBF_ProjectileDamageOverride dmg)
                    {
                        if (damageAmountOverride >= 0)
                        {
                            dmg.DamageAmountOverride = damageAmountOverride;
                        }
                        dmg.DamageMultiplier = damageMultiplier;
                    }
                }
                else
                {
                    Log.Warning($"[BF_Projectile] {projectileDef.defName} uses a custom thingClass ({projectileDef.thingClass}); cannot override damage amount, spawning normally.");
                    projectile = (Projectile)GenSpawn.Spawn(projectileDef, cell, map);
                }
            }
            else
            {
                projectile = (Projectile)GenSpawn.Spawn(projectileDef, cell, map);
            }

            if (projectile != null && damageDefOverride != null)
            {
                projectile.damageDefOverride = damageDefOverride;
            }
            return projectile;
        }

        private static Type ResolveOverrideType(Type thingClass)
        {
            if (thingClass == typeof(Projectile))
            {
                return typeof(BF_Projectile);
            }
            if (thingClass == typeof(Bullet))
            {
                return typeof(BF_Bullet);
            }
            if (thingClass == typeof(Projectile_Explosive))
            {
                return typeof(BF_Projectile_Explosive);
            }
            return null;
        }
    }
}
