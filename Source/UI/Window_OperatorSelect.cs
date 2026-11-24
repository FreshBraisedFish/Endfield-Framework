using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Endfield
{
    /// <summary>
    /// 干员招募界面：左侧列表（头像/职业图标 + 代号），右侧详情（头像、简介、技能等级与热情、特性）+ 招募按钮。
    /// </summary>
    public class Window_OperatorSelect : Window
    {
        private const float ListWidth = 300f;
        private const float RowHeight = 74f;
        private const float PortraitSize = 112f;

        private readonly Building_RecruitConsole console;
        private readonly List<OperatorDef> defs;

        private Vector2 scrollPos;
        private int selected;

        public override Vector2 InitialSize => new Vector2(860f, 640f);

        public Window_OperatorSelect(Building_RecruitConsole console)
        {
            this.console = console;
            defs = DefDatabase<OperatorDef>.AllDefsListForReading
                .OrderBy(d => d.operatorType != null ? d.operatorType.sortingOrder : 999)
                .ThenBy(d => d.defName)
                .ToList();

            doCloseX = true;
            doCloseButton = false;
            forcePause = false;
            absorbInputAroundWindow = true;
            closeOnCancel = true;
            closeOnClickedOutside = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 34f), "EF_Window_Title".Translate());
            Text.Font = GameFont.Small;

            GUI.color = Color.gray;
            Widgets.Label(new Rect(0f, 34f, inRect.width, 22f), SubtitleText());
            GUI.color = Color.white;

            Rect body = new Rect(0f, 62f, inRect.width, inRect.height - 62f);

            if (defs.Count == 0)
            {
                Widgets.Label(body, "EF_Message_NoDefs".Translate());
                return;
            }

            selected = Mathf.Clamp(selected, 0, defs.Count - 1);

            Rect listRect = new Rect(body.x, body.y, ListWidth, body.height);
            Rect detailRect = new Rect(listRect.xMax + 12f, body.y, body.width - ListWidth - 12f, body.height);

            DrawList(listRect);
            DrawDetail(detailRect, defs[selected]);
        }

        private string SubtitleText()
        {
            string text = console.CostText();
            if (console.cooldownTicks > 0)
            {
                text += "EF_Subtitle_Cooldown".Translate(console.cooldownTicks.ToStringTicksToPeriod());
            }
            if (console.OnCooldown)
            {
                text += "EF_Subtitle_Remaining".Translate(console.CooldownLeft.ToStringTicksToPeriod());
            }
            return text;
        }

        // ------------------------------------------------------------ 左侧列表

        private void DrawList(Rect outRect)
        {
            Rect viewRect = new Rect(0f, 0f, outRect.width - 18f, defs.Count * RowHeight);
            Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

            for (int i = 0; i < defs.Count; i++)
            {
                Rect row = new Rect(0f, i * RowHeight, viewRect.width, RowHeight - 4f);
                DrawListRow(row, defs[i], i == selected, i);
            }

            Widgets.EndScrollView();
        }

        private void DrawListRow(Rect row, OperatorDef def, bool isSelected, int index)
        {
            Widgets.DrawBoxSolid(row, isSelected
                ? new Color(0.24f, 0.30f, 0.34f, 0.95f)
                : new Color(0.15f, 0.16f, 0.18f, 0.85f));

            if (Mouse.IsOver(row))
            {
                Widgets.DrawHighlight(row);
            }

            Rect iconRect = new Rect(row.x + 6f, row.y + 6f, row.height - 12f, row.height - 12f);
            Texture2D icon = def.Portrait ?? def.operatorType?.Icon;
            if (icon != null)
            {
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
            }
            else
            {
                Widgets.DrawBoxSolid(iconRect, new Color(0.28f, 0.30f, 0.34f, 1f));
            }

            float textX = iconRect.xMax + 8f;
            float textWidth = row.xMax - textX - 6f;

            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(textX, row.y + 8f, textWidth, 24f), def.DisplayName);

            GUI.color = new Color(0.72f, 0.78f, 0.84f);
            Widgets.Label(new Rect(textX, row.y + 32f, textWidth, 20f), def.ClassLabel);
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(row))
            {
                selected = index;
            }
        }

        // ------------------------------------------------------------ 右侧详情

        private void DrawDetail(Rect rect, OperatorDef def)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.12f, 0.13f, 0.15f, 0.9f));
            Rect inner = rect.ContractedBy(12f);

            float y = inner.y;

            Rect portraitRect = new Rect(inner.x, y, PortraitSize, PortraitSize);
            Texture2D portrait = def.Portrait ?? def.operatorType?.Icon;
            if (portrait != null)
            {
                GUI.DrawTexture(portraitRect, portrait, ScaleMode.ScaleToFit);
            }
            else
            {
                Widgets.DrawBoxSolid(portraitRect, new Color(0.24f, 0.26f, 0.30f, 1f));
            }

            float rightX = portraitRect.xMax + 12f;
            float rightWidth = inner.xMax - rightX;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rightX, y, rightWidth, 30f), def.DisplayName);
            Text.Font = GameFont.Small;

            string meta = def.GivenName;
            if (!def.ClassLabel.NullOrEmpty())
            {
                meta += "　·　" + def.ClassLabel;
            }
            meta += "　·　" + (def.isMale ? "EF_Gender_Male".Translate() : "EF_Gender_Female".Translate());
            meta += " " + "EF_Age".Translate(def.age);
            GUI.color = new Color(0.75f, 0.80f, 0.86f);
            Widgets.Label(new Rect(rightX, y + 30f, rightWidth, 22f), meta);
            GUI.color = Color.white;

            Widgets.Label(new Rect(rightX, y + 54f, rightWidth, 56f), def.Blurb);

            y = portraitRect.yMax + 10f;
            Widgets.DrawLineHorizontal(inner.x, y, inner.width);
            y += 8f;

            // 技能
            Widgets.Label(new Rect(inner.x, y, inner.width, 22f), "EF_Section_Skills".Translate());
            y += 24f;

            if (def.skills == null || def.skills.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(new Rect(inner.x + 6f, y, inner.width, 20f), "EF_NotDefined".Translate());
                GUI.color = Color.white;
                y += 22f;
            }
            else
            {
                foreach (SkillEntry entry in def.skills)
                {
                    if (entry?.skill == null)
                    {
                        continue;
                    }
                    Rect row = new Rect(inner.x + 6f, y, inner.width - 6f, 22f);

                    Widgets.Label(new Rect(row.x, row.y, 150f, row.height), entry.skill.label);

                    GUI.color = new Color(0.85f, 0.88f, 0.92f);
                    Widgets.Label(new Rect(row.x + 150f, row.y, 46f, row.height), Mathf.Clamp(entry.level, 0, 20).ToString());
                    GUI.color = Color.white;

                    Passion passion = entry.EffectivePassion;
                    string stars = passion == Passion.Major ? "★★" : passion == Passion.Minor ? "★" : "—";
                    GUI.color = passion == Passion.None
                        ? new Color(0.55f, 0.58f, 0.62f)
                        : passion == Passion.Minor ? new Color(0.95f, 0.86f, 0.55f) : new Color(1f, 0.78f, 0.28f);
                    Widgets.Label(new Rect(row.x + 200f, row.y, 60f, row.height), stars);
                    GUI.color = Color.white;

                    y += 22f;
                }
            }

            y += 6f;

            // 特性
            Widgets.Label(new Rect(inner.x, y, inner.width, 22f), "EF_Section_Traits".Translate());
            y += 24f;

            string traitText = "EF_NotDefined".Translate();
            if (def.traits != null && def.traits.Count > 0)
            {
                traitText = string.Join("、", def.traits
                    .Where(t => t?.def != null)
                    .Select(t => TraitLabel(t.def, t.degree)));
            }
            GUI.color = new Color(0.80f, 0.84f, 0.90f);
            Widgets.Label(new Rect(inner.x + 6f, y, inner.width - 6f, 44f), traitText);
            GUI.color = Color.white;

            // 招募按钮
            Rect buttonRect = new Rect(inner.xMax - 120f, inner.yMax - 38f, 120f, 38f);
            if (console.CanRecruit(def, out string reason))
            {
                if (Widgets.ButtonText(buttonRect, "EF_Button_Recruit".Translate()))
                {
                    console.TryRecruit(def);
                }
            }
            else
            {
                GUI.color = new Color(1f, 1f, 1f, 0.45f);
                Widgets.ButtonText(buttonRect, "EF_Button_Recruit".Translate(), active: false);
                GUI.color = Color.white;
                TooltipHandler.TipRegion(buttonRect, reason);
            }
        }

        private static string TraitLabel(TraitDef traitDef, int degree)
        {
            // degreeDatas 里每一项自带 <degree> 值（2/1/-1/-2），不是数组下标，必须按值匹配
            if (traitDef.degreeDatas != null)
            {
                foreach (TraitDegreeData data in traitDef.degreeDatas)
                {
                    if (data.degree == degree)
                    {
                        return data.label;
                    }
                }
            }
            return traitDef.label;
        }
    }
}