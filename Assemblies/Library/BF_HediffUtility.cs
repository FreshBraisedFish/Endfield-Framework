using System.Collections.Generic;
using Verse;

namespace BF_Library
{
    public static class BF_HediffUtility
    {
        public static List<Hediff> GetDetectedHediffs(Pawn pawn, params HediffDef[] defs)
        {
            List<Hediff> result = new List<Hediff>();
            if (pawn?.health?.hediffSet == null)
            {
                return result;
            }
            for (int i = 0; i < defs.Length; i++)
            {
                if (defs[i] == null)
                {
                    continue;
                }
                Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(defs[i]);
                if (hediff != null)
                {
                    result.Add(hediff);
                }
            }
            return result;
        }

        public static float SumSeverity(List<Hediff> hediffs)
        {
            float sum = 0f;
            for (int i = 0; i < hediffs.Count; i++)
            {
                sum += hediffs[i].Severity;
            }
            return sum;
        }
    }
}
