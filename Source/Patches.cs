using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace XenotypeSummary
{
    [StaticConstructorOnStartup]
    public static class ModInit
    {
        static ModInit()
        {
            var harmony = new Harmony("zhuma.xenotypesummary");
            harmony.PatchAll();
            GeneThoughtCache.Initialize();
            Log.Message("[XenotypeSummary] Applied patches.");
        }
    }

    [HarmonyPatch(typeof(GeneUIUtility), "DrawGenesInfo")]
    public static class Patch_GeneUIUtility_DrawGenesInfo
    {
        public static void Postfix(Rect rect, Thing target, GeneSet pregnancyGenes)
        {
            float x = rect.xMax - 140f - 40f;
            if (Prefs.DevMode) x -= 105f;


            Rect buttonRect = new Rect(x, 5f, 140f, 24f);

            if (Widgets.ButtonText(buttonRect, "XS_Summary".Translate(), true, true, true))
            {
                IEnumerable<GeneDef> genes = GetGenes(target, pregnancyGenes);
                IEnumerable<GeneDef> overridden = GetOverridden(target);


                if (genes.Any())
                {
                    string label = (target as Pawn)?.LabelShortCap ?? (target as GeneSetHolderBase)?.LabelCap ?? "XS_Xenotype".Translate();
                    Find.WindowStack.Add(new Window_XenotypeSummary(genes, "XS_SummaryOf".Translate(label), overridden));
                }
            }
        }

        private static IEnumerable<GeneDef> GetGenes(Thing target, GeneSet genesOverride)
        {
            if (target is Pawn pawn) return pawn.genes.GenesListForReading.Select(g => g.def);
            if (genesOverride != null) return genesOverride.GenesListForReading;
            if (target is GeneSetHolderBase holder && holder.GeneSet != null) return holder.GeneSet.GenesListForReading;
            return Enumerable.Empty<GeneDef>();
        }

        private static IEnumerable<GeneDef> GetOverridden(Thing target)
        {
            if (target is Pawn pawn) return pawn.genes.GenesListForReading.Where(g => g.Overridden).Select(g => g.def);
            return null;
        }
    }

    [HarmonyPatch(typeof(GeneCreationDialogBase), "DoBottomButtons")]
    public static class Patch_GeneCreationDialogBase_DoBottomButtons
    {
        public static void Postfix(GeneCreationDialogBase __instance, Rect rect)
        {
            Rect sumRect = new Rect(rect.x + 160f, rect.y, 140f, rect.height);


            if (Widgets.ButtonText(sumRect, "XS_Summary".Translate(), true, true, true))
            {
                var traverse = Traverse.Create(__instance);
                var genes = traverse.Property("SelectedGenes").GetValue<List<GeneDef>>();
                var overridden = traverse.Field("cachedOverriddenGenes").GetValue<List<GeneDef>>();


                if (genes != null && genes.Any())
                {
                    Find.WindowStack.Add(new Window_XenotypeSummary(genes, "XS_SelectedGenesSummary".Translate(), overridden));
                }
            }
        }
    }


    [HarmonyPatch(typeof(Dialog_InfoCard), "DoWindowContents")]
    public static class Patch_Dialog_InfoCard_DoWindowContents
    {
        public static void Postfix(Dialog_InfoCard __instance, Rect inRect)
        {
            var def = Traverse.Create(__instance).Field("def").GetValue<Def>();
            if (def is XenotypeDef xeno)
            {
                Rect buttonRect = new Rect(inRect.width - 150f, 10f, 140f, 30f);
                if (Widgets.ButtonText(buttonRect, "XS_Summary".Translate(), true, true, true))
                {
                    Find.WindowStack.Add(new Window_XenotypeSummary(xeno.genes, "XS_SummaryOf".Translate(xeno.LabelCap)));
                }
            }
        }
    }
}
