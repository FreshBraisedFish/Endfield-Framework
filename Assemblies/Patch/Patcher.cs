using HarmonyLib;
using Verse;

namespace EndField
{
    [StaticConstructorOnStartup]
    public static class Patcher
    {
        static Patcher()
        {
            new Harmony("EndField.Patch").PatchAll();
        }
    }
}
