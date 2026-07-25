using System;
using Verse;
using RimWorld;
using AK_DLL;

namespace EndField
{
    public static class NameInjector
    {
        public static void Apply(Pawn pawn, FA_Extension ext)
        {
            try
            {
                string lang = LanguageDatabase.activeLanguage.FriendlyNameEnglish;
                if (!lang.StartsWith("English")) return;

                if (!string.IsNullOrEmpty(ext.nameEn) || !string.IsNullOrEmpty(ext.nicknameEn))
                {
                    string first = ext.nameEn ?? "";
                    string last = "";
                    int spaceIdx = first.IndexOf(' ');
                    if (spaceIdx > 0)
                    { last = first.Substring(spaceIdx + 1); first = first.Substring(0, spaceIdx); }
                    string nick = !string.IsNullOrEmpty(ext.nicknameEn) ? ext.nicknameEn : first;
                    pawn.Name = new NameTriple(first, nick, last);
                }
            }
            catch { }
        }

        public static void ApplyDescription(OperatorDef opDef, FA_Extension ext)
        {
            try
            {
                if (!string.IsNullOrEmpty(ext.descriptionEn))
                {
                    string lang = LanguageDatabase.activeLanguage.FriendlyNameEnglish;
                    if (lang.StartsWith("English"))
                        opDef.description = ext.descriptionEn;
                }
            }
            catch { }
        }
    }
}
