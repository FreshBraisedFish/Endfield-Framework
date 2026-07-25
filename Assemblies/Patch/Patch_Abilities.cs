using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using AK_DLL;

namespace EndField
{
    public class EF_AbilityExtension : DefModExtension
    {
        public List<string> extraAbilities;
    }

    [HarmonyPatch]
    public static class Patch_AddExtraAbilities
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(OperatorDef), "Recruit_NoMap", Type.EmptyTypes);
        }

        static void Postfix(OperatorDef __instance, ref Pawn __result)
        {
            if (__result == null || __result.abilities == null) return;

            EF_AbilityExtension ext = __instance.GetModExtension<EF_AbilityExtension>();
            if (ext == null || ext.extraAbilities == null || ext.extraAbilities.Count == 0) return;

            foreach (string abilityDefName in ext.extraAbilities)
            {
                AbilityDef def = DefDatabase<AbilityDef>.GetNamedSilentFail(abilityDefName);
                if (def == null)
                {
                    Log.Warning("[EF] AbilityDef '" + abilityDefName + "' not found, skipping.");
                    continue;
                }
                try
                {
                    __result.abilities.GainAbility(def);
                }
                catch (Exception ex)
                {
                    Log.Error("[EF] Failed to add ability '" + abilityDefName + "': " + ex.Message);
                }
            }
        }
    }
}
