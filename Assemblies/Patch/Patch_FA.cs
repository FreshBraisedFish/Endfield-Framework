using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using AK_DLL;

namespace EndField
{
    public class FA_Extension : DefModExtension
    {
        public string headTypeDef;
        public string eyeTypeDef;
        public string browTypeDef;
        public string lidTypeDef;
        public string mouthTypeDef;
        public string skinTypeDef;
        public string eyeballColorDef;
        public string eyeballShapeDef;
        public float eyeballColorR = -1f;
        public float eyeballColorG = -1f;
        public float eyeballColorB = -1f;
        public float eyeballColorA = -1f;
        public float eyeballColorR2 = -1f;
        public float eyeballColorG2 = -1f;
        public float eyeballColorB2 = -1f;
        public float eyeballColorA2 = -1f;
        public string nameEn;
        public string nicknameEn;
        public string descriptionEn;
    }

    [HarmonyPatch]
    public static class Patch_RecruitOperator
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(OperatorDef), "Recruit",
                new Type[] { typeof(IntVec3), typeof(Map) });
        }

        static void Postfix(OperatorDef __instance, IntVec3 intVec, Map map, Pawn __result)
        {
            if (__instance == null) return;

            FA_Extension ext = __instance.GetModExtension<FA_Extension>();
            if (ext == null) return;

            Pawn pawn = __result;
            if (pawn == null) return;

            FA_ApplyHelper.ApplyFAParts(pawn, ext);
            NameInjector.Apply(pawn, ext);
            NameInjector.ApplyDescription(__instance, ext);
        }
    }

    static class FA_ApplyHelper
    {
        internal static void ApplyFAParts(Pawn pawn, FA_Extension ext)
        {
            if (pawn == null || pawn.Destroyed) return;

            List<ThingComp> comps = pawn.AllComps;
            if (comps == null || comps.Count == 0) return;

            foreach (ThingComp comp in comps)
            {
                Type compType = comp.GetType();
                string compTypeName = compType.FullName ?? "";
                if (!compTypeName.Contains("FacialAnimation")) continue;

                PropertyInfo faceTypeProp = compType.GetProperty("FaceType");
                if (faceTypeProp == null) continue;

                Type propType = faceTypeProp.PropertyType;
                Type dbType = typeof(DefDatabase<>).MakeGenericType(propType);
                MethodInfo getNamed = dbType.GetMethod("GetNamedSilentFail",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { typeof(string) }, null);
                if (getNamed == null) continue;

                bool isEyeball = compTypeName.Contains("EyeballController");
                string defNameToSet = null;
                if (compTypeName.Contains("HeadController"))
                    defNameToSet = ext.headTypeDef;
                else if (isEyeball)
                    defNameToSet = ext.eyeTypeDef;
                else if (compTypeName.Contains("BrowController"))
                    defNameToSet = ext.browTypeDef;
                else if (compTypeName.Contains("LidController"))
                    defNameToSet = ext.lidTypeDef;
                else if (compTypeName.Contains("MouthController"))
                    defNameToSet = ext.mouthTypeDef;
                else if (compTypeName.Contains("SkinController"))
                    defNameToSet = ext.skinTypeDef;

                if (!string.IsNullOrEmpty(defNameToSet))
                {
                    object faceTypeDef = getNamed.Invoke(null, new object[] { defNameToSet });
                    if (faceTypeDef != null)
                        faceTypeProp.SetValue(comp, faceTypeDef, null);
                }

                if (isEyeball)
                    SetEyeballColor(comp, ext);
            }
        }

        static void SetEyeballColor(object comp, FA_Extension ext)
        {
            Type t = comp.GetType();

            if (!string.IsNullOrEmpty(ext.eyeballColorDef))
            {
                PropertyInfo defProp = t.GetProperty("eyeColorDef") ?? t.GetProperty("EyeballColorDef");
                if (defProp != null)
                {
                    Type cdType = defProp.PropertyType;
                    Type dbT = typeof(DefDatabase<>).MakeGenericType(cdType);
                    MethodInfo gn = dbT.GetMethod("GetNamedSilentFail",
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new Type[] { typeof(string) }, null);
                    if (gn != null)
                    {
                        object cd = gn.Invoke(null, new object[] { ext.eyeballColorDef });
                        if (cd != null) defProp.SetValue(comp, cd, null);
                    }
                }
            }

            bool hasP = ext.eyeballColorR >= 0f && ext.eyeballColorG >= 0f && ext.eyeballColorB >= 0f;
            bool hasS = ext.eyeballColorR2 >= 0f && ext.eyeballColorG2 >= 0f && ext.eyeballColorB2 >= 0f;
            if (!hasP && !hasS) return;

            Color primary = new Color(ext.eyeballColorR, ext.eyeballColorG, ext.eyeballColorB, ext.eyeballColorA >= 0f ? ext.eyeballColorA : 1f);
            Color secondary = hasS ? new Color(ext.eyeballColorR2, ext.eyeballColorG2, ext.eyeballColorB2, ext.eyeballColorA2 >= 0f ? ext.eyeballColorA2 : 1f) : primary;

            PropertyInfo fp = t.GetProperty("FaceColor");
            if (fp != null && fp.PropertyType == typeof(Color))
                fp.SetValue(comp, primary, null);

            FieldInfo cf = t.GetField("color");
            if (cf != null && cf.FieldType == typeof(Color))
                cf.SetValue(comp, primary);

            FieldInfo sf = t.GetField("secondColor");
            if (sf != null && sf.FieldType == typeof(Color))
                sf.SetValue(comp, hasS ? secondary : primary);

            PropertyInfo sp = t.GetProperty("FaceSecondColor");
            if (sp != null && sp.PropertyType == typeof(Color))
                sp.SetValue(comp, hasS ? secondary : primary, null);
        }
    }
}