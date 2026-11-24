using RimWorld;
using Verse;

namespace Endfield
{
    [DefOf]
    public static class EF_StatDefOf
    {
        public static StatDef EF_RangedDamageFactor;

        static EF_StatDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(EF_StatDefOf));
        }
    }
}