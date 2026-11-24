using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace Endfield
{
    /// <summary>开发者菜单里的快捷测试入口。</summary>
    public static class EF_DebugActions
    {
        [DebugAction("Endfield", "Recruit first operator here", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void SpawnFirstOperatorHere()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            OperatorDef def = DefDatabase<OperatorDef>.AllDefsListForReading
                .OrderBy(d => d.defName)
                .FirstOrDefault();

            if (def == null)
            {
                Messages.Message("No operator definitions available.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            Pawn pawn = OperatorGenerator.Generate(def, map, UI.MouseCell());
            if (pawn != null)
            {
                Messages.Message("Spawned: " + pawn.LabelShortCap, MessageTypeDefOf.PositiveEvent, false);
            }
        }
    }
}