using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Endfield
{
    /// <summary>
    /// 把 FA_Extension 写进 UNAGI FacialAnimation 的各个 controller comp。
    /// 全部走反射，没装面部 mod 时自然跳过。
    /// </summary>
    internal static class FaceApplicator
    {
        internal static void Apply(Pawn pawn, FA_Extension ext)
        {
            if (pawn == null || pawn.Destroyed || ext == null)
            {
                return;
            }

            List<ThingComp> comps = pawn.AllComps;
            if (comps == null || comps.Count == 0)
            {
                return;
            }

            foreach (ThingComp comp in comps)
            {
                Type compType = comp.GetType();
                string typeName = compType.FullName ?? string.Empty;
                if (!typeName.Contains("FacialAnimation"))
                {
                    continue;
                }

                PropertyInfo faceTypeProp = compType.GetProperty("FaceType");
                if (faceTypeProp == null)
                {
                    continue;
                }

                bool isEyeball = typeName.Contains("EyeballController");
                string defName = ResolveDefName(typeName, ext);
                if (!string.IsNullOrEmpty(defName))
                {
                    TryAssignDef(comp, faceTypeProp, defName);
                }

                if (isEyeball)
                {
                    ApplyEyeballColor(comp, compType, ext);
                }
            }
        }

        private static string ResolveDefName(string typeName, FA_Extension ext)
        {
            if (typeName.Contains("HeadController"))
            {
                return ext.headTypeDef;
            }
            if (typeName.Contains("EyeballController"))
            {
                return ext.eyeTypeDef;
            }
            if (typeName.Contains("BrowController"))
            {
                return ext.browTypeDef;
            }
            if (typeName.Contains("LidController"))
            {
                return ext.lidTypeDef;
            }
            if (typeName.Contains("MouthController"))
            {
                return ext.mouthTypeDef;
            }
            if (typeName.Contains("SkinController"))
            {
                return ext.skinTypeDef;
            }
            return null;
        }

        private static void TryAssignDef(object comp, PropertyInfo prop, string defName)
        {
            try
            {
                Type defType = prop.PropertyType;
                Type dbType = typeof(DefDatabase<>).MakeGenericType(defType);
                MethodInfo getNamed = dbType.GetMethod(
                    "GetNamedSilentFail",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(string) },
                    null);
                if (getNamed == null)
                {
                    return;
                }
                object def = getNamed.Invoke(null, new object[] { defName });
                if (def != null)
                {
                    prop.SetValue(comp, def, null);
                }
                else
                {
                    Log.Warning("[Endfield] Face part not found: " + defName + " (" + defType.Name + ")");
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Endfield] Failed to set face part: " + e.Message);
            }
        }

        private static void ApplyEyeballColor(object comp, Type compType, FA_Extension ext)
        {
            if (!ext.eyeballColorDef.NullOrEmpty())
            {
                PropertyInfo defProp = compType.GetProperty("eyeColorDef") ?? compType.GetProperty("EyeballColorDef");
                if (defProp != null)
                {
                    TryAssignDef(comp, defProp, ext.eyeballColorDef);
                }
            }

            bool hasPrimary = ext.eyeballColorR >= 0f && ext.eyeballColorG >= 0f && ext.eyeballColorB >= 0f;
            bool hasSecondary = ext.eyeballColorR2 >= 0f && ext.eyeballColorG2 >= 0f && ext.eyeballColorB2 >= 0f;
            if (!hasPrimary && !hasSecondary)
            {
                return;
            }

            Color primary = new Color(
                ext.eyeballColorR,
                ext.eyeballColorG,
                ext.eyeballColorB,
                ext.eyeballColorA >= 0f ? ext.eyeballColorA : 1f);
            Color secondary = hasSecondary
                ? new Color(ext.eyeballColorR2, ext.eyeballColorG2, ext.eyeballColorB2, ext.eyeballColorA2 >= 0f ? ext.eyeballColorA2 : 1f)
                : primary;

            try
            {
                PropertyInfo faceColor = compType.GetProperty("FaceColor");
                if (faceColor != null && faceColor.PropertyType == typeof(Color))
                {
                    faceColor.SetValue(comp, primary, null);
                }

                PropertyInfo faceSecond = compType.GetProperty("FaceSecondColor");
                if (faceSecond != null && faceSecond.PropertyType == typeof(Color))
                {
                    faceSecond.SetValue(comp, secondary, null);
                }

                FieldInfo colorField = compType.GetField("color");
                if (colorField != null && colorField.FieldType == typeof(Color))
                {
                    colorField.SetValue(comp, primary);
                }

                FieldInfo secondField = compType.GetField("secondColor");
                if (secondField != null && secondField.FieldType == typeof(Color))
                {
                    secondField.SetValue(comp, secondary);
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Endfield] Failed to set eyeball color: " + e.Message);
            }
        }
    }
}