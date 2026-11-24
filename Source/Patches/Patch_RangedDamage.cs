using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Endfield
{
    [StaticConstructorOnStartup]
    public static class EF_Harmony
    {
        static EF_Harmony()
        {
            new Harmony("BraisedFish.Endfield").PatchAll(typeof(EF_Harmony).Assembly);
        }
    }

    /// <summary>
    /// 让 EF_RangedDamageFactor 真正作用到弹丸伤害上。
    /// 挂在 Projectile.DamageAmount 的 getter 上：所有走 Projectile 的远程攻击
    /// （枪械、炮塔、迫击炮、BF_CompAbilityEffect_DelayedHits 生成的弹丸）都会经过这里。
    /// </summary>
    [HarmonyPatch]
    public static class Patch_ProjectileDamageAmount
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.PropertyGetter(typeof(Projectile), nameof(Projectile.DamageAmount));
        }

        private static void Postfix(Projectile __instance, ref int __result)
        {
            if (__result <= 0)
            {
                return;
            }

            // 只缩放由 pawn 发射的弹丸；炮塔、陷阱、无 launcher 的弹丸保持原样
            if (!(__instance.Launcher is Pawn pawn))
            {
                return;
            }

            float factor = pawn.GetStatValue(EF_StatDefOf.EF_RangedDamageFactor);
            if (Mathf.Approximately(factor, 1f))
            {
                return;
            }

            __result = Mathf.Max(1, Mathf.RoundToInt(__result * factor));
        }
    }
}