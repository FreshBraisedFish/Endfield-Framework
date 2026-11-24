using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Endfield
{
    /// <summary>
    /// 干员招募终端。打开选择窗口，从所有 OperatorDef 里点一名干员直接生成。
    /// 消耗与冷却在 ThingDef 的 modExtensions 里配。
    /// </summary>
    public class Building_RecruitConsole : Building
    {
        private int lastRecruitTick = -999999;

        private RecruitConsoleExtension Settings => def.GetModExtension<RecruitConsoleExtension>();

        public int silverCost => Settings != null ? Settings.silverCost : 0;

        public int cooldownTicks => Settings != null ? Settings.cooldownTicks : 600;

        public int CooldownLeft => Mathf.Max(0, lastRecruitTick + cooldownTicks - Find.TickManager.TicksGame);

        public bool OnCooldown => CooldownLeft > 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref lastRecruitTick, "lastRecruitTick", -999999);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            Command_Action recruit = new Command_Action
            {
                defaultLabel = "EF_RecruitGizmo_Label".Translate(),
                defaultDesc = "EF_RecruitGizmo_Desc".Translate(CostText(), Map != null ? CountSilver(Map) : 0),
                icon = OperatorUI.RecruitIcon,
                action = OpenWindow
            };

            if (Map == null)
            {
                recruit.Disable("EF_Reason_NoMap".Translate());
            }
            else if (OnCooldown)
            {
                recruit.Disable("EF_Reason_Cooldown".Translate(CooldownLeft.ToStringTicksToPeriod()));
            }
            else if (silverCost > 0 && CountSilver(Map) < silverCost)
            {
                recruit.Disable("EF_Reason_NotEnoughSilver".Translate(silverCost));
            }

            yield return recruit;
        }

        public string CostText()
        {
            return silverCost > 0 ? "EF_Cost_Silver".Translate(silverCost) : "EF_Cost_Free".Translate();
        }

        private void OpenWindow()
        {
            Find.WindowStack.Add(new Window_OperatorSelect(this));
        }

        public bool CanRecruit(OperatorDef def, out string reason)
        {
            reason = null;
            if (def == null)
            {
                reason = "EF_Reason_InvalidDef".Translate();
                return false;
            }
            if (Map == null)
            {
                reason = "EF_Reason_NoMap".Translate();
                return false;
            }
            if (OnCooldown)
            {
                reason = "EF_Reason_Cooldown".Translate(CooldownLeft.ToStringTicksToPeriod());
                return false;
            }
            if (silverCost > 0 && CountSilver(Map) < silverCost)
            {
                reason = "EF_Reason_NotEnoughSilver".Translate(silverCost);
                return false;
            }
            return true;
        }

        public Pawn TryRecruit(OperatorDef def)
        {
            if (!CanRecruit(def, out string reason))
            {
                Messages.Message(reason, MessageTypeDefOf.RejectInput, false);
                return null;
            }

            if (silverCost > 0)
            {
                ConsumeSilver(Map, silverCost);
            }

            IntVec3 cell = InteractionCell.IsValid ? InteractionCell : Position;
            Pawn pawn = OperatorGenerator.Generate(def, Map, cell);
            if (pawn == null)
            {
                Messages.Message("EF_Message_RecruitFailed".Translate(), MessageTypeDefOf.NegativeEvent, false);
                return null;
            }

            lastRecruitTick = Find.TickManager.TicksGame;
            Messages.Message("EF_Message_Recruited".Translate(pawn.LabelShortCap), MessageTypeDefOf.PositiveEvent, false);
            return pawn;
        }

        private static int CountSilver(Map map)
        {
            return map.resourceCounter.GetCount(ThingDefOf.Silver);
        }

        private static void ConsumeSilver(Map map, int amount)
        {
            int remaining = amount;
            foreach (Thing silver in map.listerThings.ThingsOfDef(ThingDefOf.Silver).ToList())
            {
                if (remaining <= 0)
                {
                    break;
                }
                int take = Mathf.Min(remaining, silver.stackCount);
                Thing taken = silver.SplitOff(take);
                taken.Destroy(DestroyMode.Vanish);
                remaining -= take;
            }
        }
    }

    /// <summary>界面共用的小图标缓存。</summary>
    internal static class OperatorUI
    {
        private static Texture2D recruitIcon;

        internal static Texture2D RecruitIcon
        {
            get
            {
                if (recruitIcon == null)
                {
                    recruitIcon = ContentFinder<Texture2D>.Get("UI/Element/Endfield", false);
                }
                return recruitIcon;
            }
        }
    }
}