using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

public static class GeneThoughtCache
{
    public static Dictionary<GeneDef, List<ThoughtDef>> RequiredBy = new Dictionary<GeneDef, List<ThoughtDef>>();
    public static Dictionary<GeneDef, List<ThoughtDef>> NullifiedBy = new Dictionary<GeneDef, List<ThoughtDef>>();

    public static void Initialize()
    {
        foreach (var th in DefDatabase<ThoughtDef>.AllDefsListForReading)
        {
            if (th.stages == null) continue;

            if (th.requiredGenes != null)
            {
                foreach (var gene in th.requiredGenes)
                {
                    if (!RequiredBy.ContainsKey(gene)) RequiredBy[gene] = new List<ThoughtDef>();
                    RequiredBy[gene].Add(th);
                }
            }

            if (th.nullifyingGenes != null)
            {
                foreach (var gene in th.nullifyingGenes)
                {
                    if (!NullifiedBy.ContainsKey(gene)) NullifiedBy[gene] = new List<ThoughtDef>();
                    NullifiedBy[gene].Add(th);
                }
            }
        }
    }
}