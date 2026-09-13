using Verse;

namespace BF_Library
{
    internal static class BF_TargetInfoScribe
    {
        // 手动序列化 LocalTargetInfo，避免原版 Scribe_TargetInfo 在目标失效时
        // 产生悬挂的 Thing 引用，读档时报
        // “Could not get load ID ... /pendingTarget/thing”。
        // Thing 只在可安全引用时才写入，否则退化为只保存 cell。
        public static void Look(ref LocalTargetInfo target, ref IntVec3 cell, ref Thing thing, string label)
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                cell = target.IsValid ? target.Cell : IntVec3.Invalid;
                thing = target.Thing;
                if (thing != null && (thing.Destroyed || !thing.SpawnedOrAnyParentSpawned))
                {
                    thing = null;
                }
                Scribe_Values.Look(ref cell, label + "Cell", IntVec3.Invalid);
                Scribe_References.Look(ref thing, label + "Thing");
                return;
            }

            Scribe_Values.Look(ref cell, label + "Cell", IntVec3.Invalid);
            Scribe_References.Look(ref thing, label + "Thing");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                target = thing != null
                    ? new LocalTargetInfo(thing)
                    : (cell.IsValid ? new LocalTargetInfo(cell) : LocalTargetInfo.Invalid);
            }
        }
    }
}
