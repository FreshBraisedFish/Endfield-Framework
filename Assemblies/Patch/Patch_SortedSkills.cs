using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using AK_DLL;

namespace EndField
{
    [HarmonyPatch(typeof(OperatorDef))]
    [HarmonyPatch("SortedSkills", MethodType.Getter)]
    public static class Patch_SortedSkills
    {
        static readonly HashSet<string> vanillaSkills = new HashSet<string>
        {
            "Animals", "Artistic", "Construction", "Cooking",
            "Crafting", "Intellectual", "Medicine", "Melee",
            "Mining", "Plants", "Shooting", "Social"
        };

        [HarmonyPrefix]
        static bool Prefix(OperatorDef __instance, ref List<SkillAndFire> __result)
        {
            FieldInfo skillsField = AccessTools.Field(typeof(OperatorDef), "skills");
            if (skillsField == null)
            {
                __result = new List<SkillAndFire>();
                return false;
            }

            var raw = skillsField.GetValue(__instance) as System.Collections.IList;
            if (raw == null || raw.Count == 0)
            {
                __result = new List<SkillAndFire>();
                return false;
            }

            var result = new List<SkillAndFire>();
            foreach (var entry in raw)
            {
                Type t = entry.GetType();
                FieldInfo skillField = AccessTools.Field(t, "skill");
                if (skillField == null) continue;

                object val = skillField.GetValue(entry);
                SkillDef def = val as SkillDef;
                string defName = null;
                if (def != null)
                    defName = def.defName;

                if (defName == null)
                {
                    string name = val as string;
                    if (!string.IsNullOrEmpty(name))
                        defName = name;
                }

                if (defName != null && vanillaSkills.Contains(defName))
                    result.Add(entry as SkillAndFire);
            }

            __result = result;
            return false;
        }
    }
}
