using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace XenotypeSummary
{
    public class Window_XenotypeSummary : Window
    {
        private XenotypeSummaryGenerator summary;
        private Vector2 scrollPosDescriptions;
        private Vector2 scrollPosStats;
        private string header;
        private XenotypeSearchFilter searchFilter = new XenotypeSearchFilter();
        private float lastSearchChangeTime = -1f;
        private const float DebounceDelay = 0.3f;

        // Caching
        private float cachedHeightLeft;
        private float cachedHeightRight;
        private List<GeneData> cachedFilteredGenes = new List<GeneData>();

        // Cache structures for stats side
        private List<XenotypeSummaryGenerator.AptitudeSummary> cachedApts = new List<XenotypeSummaryGenerator.AptitudeSummary>();
        private List<PassionMod> cachedPassions = new List<PassionMod>();
        private List<XenotypeSummaryGenerator.StatSummary> cachedStats = new List<XenotypeSummaryGenerator.StatSummary>();
        private List<XenotypeSummaryGenerator.CapacitySummary> cachedCaps = new List<XenotypeSummaryGenerator.CapacitySummary>();
        private List<XenotypeSummaryGenerator.DamageSummary> cachedDamage = new List<XenotypeSummaryGenerator.DamageSummary>();
        private List<Pair<string, string>> cachedGlobalFactors = new List<Pair<string, string>>();
        private List<ChemicalEffectData> cachedChemicals = new List<ChemicalEffectData>();
        private List<string> cachedSpecialEffects = new List<string>();
        private List<NeedDef> cachedGainedNeeds = new List<NeedDef>();
        private List<NeedDef> cachedDisabledNeeds = new List<NeedDef>();
        private List<AbilityDef> cachedAbilities = new List<AbilityDef>();
        private List<GeneticTraitData> cachedForcedTraits = new List<GeneticTraitData>();
        private List<GeneticTraitData> cachedSuppressedTraits = new List<GeneticTraitData>();
        private List<ThoughtDef> cachedMoodThoughts = new List<ThoughtDef>();
        private List<ThoughtDef> cachedNullifiedThoughts = new List<ThoughtDef>();

        private struct GeneData
        {
            public GeneDef gene;
            public string highlightedLabel;
            public string highlightedDesc;
            public List<string> highlightedCustomDescs;
            public float height;
        }

        private struct ChemicalEffectData
        {
            public ChemicalDef chemical;
            public float overdoseFactor;
            public float toleranceFactor;
            public string highlightedLabel;
        }

        public override Vector2 InitialSize => new Vector2(900f, 700f);

        public Window_XenotypeSummary(IEnumerable<GeneDef> genes, string header = null, IEnumerable<GeneDef> overridden = null)
        {
            this.summary = new XenotypeSummaryGenerator(genes, overridden);
            this.header = header ?? "XS_XenotypeSummary".Translate();
            this.doCloseButton = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), header);

            Rect searchRect = new Rect(inRect.width - 240f, 4f, 180f, 24f);
            searchFilter.DrawSearchBar(searchRect);

            if (searchFilter.isDirty)
            {
                if (!searchFilter.Active)
                {
                    RecalculateCaches(inRect.width);
                    lastSearchChangeTime = -1f;
                }
                else
                {
                    lastSearchChangeTime = Time.time;
                }
                searchFilter.isDirty = false;
            }

            if (lastSearchChangeTime > 0f && Time.time - lastSearchChangeTime > DebounceDelay)
            {
                RecalculateCaches(inRect.width);
                lastSearchChangeTime = -1f;
            }

            Text.Font = GameFont.Small;

            Rect contentRect = inRect.AtZero();
            contentRect.yMin += 45f;
            contentRect.height -= 45f;

            float colWidth = (contentRect.width - 20f) / 2f;
            Rect leftRect = new Rect(contentRect.x, contentRect.y, colWidth, contentRect.height);
            Rect rightRect = new Rect(contentRect.x + colWidth + 20f, contentRect.y, colWidth, contentRect.height);

            DrawDescriptions(leftRect);
            DrawStats(rightRect);
        }

        private void RecalculateCaches(float totalWidth)
        {
            float colWidth = (totalWidth - 20f) / 2f;
            float innerWidth = colWidth - 20f;

            // Left Side: Genes
            cachedFilteredGenes.Clear();
            cachedHeightLeft = 0f;
            foreach (var gene in summary.allGenes)
            {
                if (!searchFilter.GeneMatches(gene)) continue;
                if (gene.description.NullOrEmpty() && (gene.customEffectDescriptions == null || !gene.customEffectDescriptions.Any())) continue;

                var gd = new GeneData
                {
                    gene = gene,
                    highlightedLabel = searchFilter.Highlight(gene.LabelCap),
                    highlightedDesc = searchFilter.Highlight(gene.description),
                    highlightedCustomDescs = gene.customEffectDescriptions?.Select(e => searchFilter.Highlight(e.ResolveTags())).ToList() ?? new List<string>(),
                    height = 34f
                };

                if (!gd.highlightedDesc.NullOrEmpty())
                {
                    gd.height += Text.CalcHeight(gd.highlightedDesc, innerWidth - 16f) + 2f;
                }

                foreach (var hcd in gd.highlightedCustomDescs)
                {
                    string effectStr = ("XS_Effects".Translate().CapitalizeFirst() + ":").Colorize(ColoredText.TipSectionTitleColor) + $"\n  - {hcd}";
                    gd.height += Text.CalcHeight(effectStr, innerWidth - 16f) + 2f;
                }

                gd.height += 15f; // Padding + Line
                cachedFilteredGenes.Add(gd);
                cachedHeightLeft += gd.height;
            }

            // Right Side: Stats side
            cachedApts = summary.aptitudeSummaries.Values.Where(a => searchFilter.Matches(a.skill.label)).OrderBy(a => a.skill.label).ToList();
            cachedPassions = summary.passionMods.Where(p => searchFilter.Matches(p.skill.label)).ToList();
            cachedStats = summary.statSummaries.Values.Where(s => searchFilter.StatMatches(s.stat, "")).OrderBy(s => s.stat.label).ToList();
            cachedCaps = summary.capacitySummaries.Values.Where(c => searchFilter.Matches(c.capacity.label)).OrderBy(c => c.capacity.label).ToList();
            cachedDamage = summary.damageSummaries.Values.Where(d => searchFilter.Matches(d.damageDef.label)).OrderBy(d => d.damageDef.label).ToList();


            cachedGlobalFactors.Clear();
            if (summary.totalPainOffset != 0f && searchFilter.Matches("XS_PainOffset".Translate())) cachedGlobalFactors.Add(new Pair<string, string>("XS_PainOffset".Translate(), (summary.totalPainOffset * 100f).ToString("+#;-#") + "%"));
            if (summary.totalPainFactor != 1f && searchFilter.Matches("XS_PainFactor".Translate())) cachedGlobalFactors.Add(new Pair<string, string>("XS_PainFactor".Translate(), "x" + summary.totalPainFactor.ToStringPercent()));
            if (summary.totalFoodPoisoningFactor != 1f && searchFilter.Matches("XS_FoodPoisoningChance".Translate())) cachedGlobalFactors.Add(new Pair<string, string>("XS_FoodPoisoningChance".Translate(), "x" + summary.totalFoodPoisoningFactor.ToStringPercent()));
            if (summary.totalSocialFightFactor != 1f && searchFilter.Matches("XS_SocialFightChance".Translate())) cachedGlobalFactors.Add(new Pair<string, string>("XS_SocialFightChance".Translate(), "x" + summary.totalSocialFightFactor.ToStringPercent()));
            if (summary.totalAggroMentalBreakFactor != 1f && searchFilter.Matches("XS_AggroMentalBreakChance".Translate())) cachedGlobalFactors.Add(new Pair<string, string>("XS_AggroMentalBreakChance".Translate(), "x" + summary.totalAggroMentalBreakFactor.ToStringPercent()));
            if (summary.totalPrisonBreakFactor != 1f && searchFilter.Matches("XS_PrisonBreakChance".Translate())) cachedGlobalFactors.Add(new Pair<string, string>("XS_PrisonBreakChance".Translate(), "x" + summary.totalPrisonBreakFactor.ToStringPercent()));
            if (summary.totalRomanceChanceFactor != 1f && searchFilter.Matches("XS_RomanceChance".Translate())) cachedGlobalFactors.Add(new Pair<string, string>("XS_RomanceChance".Translate(), "x" + summary.totalRomanceChanceFactor.ToStringPercent()));
            if (summary.totalLovinMTBFactor != 1f && searchFilter.Matches("XS_LovinMTB".Translate())) cachedGlobalFactors.Add(new Pair<string, string>("XS_LovinMTB".Translate(), "x" + summary.totalLovinMTBFactor.ToStringPercent()));
            if (summary.totalResourceLossPerDay != 0f && searchFilter.Matches("XS_DailyResourceLoss".Translate())) cachedGlobalFactors.Add(new Pair<string, string>("XS_DailyResourceLoss".Translate(), summary.totalResourceLossPerDay.ToString("0.##")));

            cachedChemicals.Clear();
            var allChems = summary.overdoseFactors.Keys.Union(summary.toleranceFactors.Keys).ToList();
            foreach (var chem in allChems)
            {
                if (searchFilter.Matches(chem.label))
                {
                    cachedChemicals.Add(new ChemicalEffectData
                    {
                        chemical = chem,
                        overdoseFactor = summary.overdoseFactors.TryGetValue(chem, 1f),
                        toleranceFactor = summary.toleranceFactors.TryGetValue(chem, 1f),
                        highlightedLabel = searchFilter.Highlight(chem.LabelCap)
                    });
                }
            }

            cachedSpecialEffects.Clear();
            if (summary.toxGasImmunity && searchFilter.Matches("XS_ToxGasImmunity".Translate())) cachedSpecialEffects.Add(searchFilter.Highlight("XS_ToxGasImmunity".Translate()));
            if (summary.vacuumBurnImmunity && searchFilter.Matches("XS_VacuumBurnImmunity".Translate())) cachedSpecialEffects.Add(searchFilter.Highlight("XS_VacuumBurnImmunity".Translate()));
            if (summary.ignoreDarkness && searchFilter.Matches("XS_IgnoreDarkness".Translate())) cachedSpecialEffects.Add(searchFilter.Highlight("XS_IgnoreDarkness".Translate()));
            if (summary.sterilize && searchFilter.Matches("XS_Sterilized".Translate())) cachedSpecialEffects.Add(searchFilter.Highlight("XS_Sterilized".Translate()));
            if (summary.preventPermanentWounds && searchFilter.Matches("XS_PreventPermanentWounds".Translate())) cachedSpecialEffects.Add(searchFilter.Highlight("XS_PreventPermanentWounds".Translate()));
            if (summary.dontMindRawFood && searchFilter.Matches("XS_DontMindRawFood".Translate())) cachedSpecialEffects.Add(searchFilter.Highlight("XS_DontMindRawFood".Translate()));
            foreach (var h in summary.immunityHediffs) if (searchFilter.Matches(h.label)) cachedSpecialEffects.Add(searchFilter.Highlight("XS_ImmuneTo".Translate(h.LabelCap)));
            foreach (var h in summary.cannotGiveHediffs) if (searchFilter.Matches(h.label)) cachedSpecialEffects.Add(searchFilter.Highlight("XS_CausesImmunityTo".Translate(h.LabelCap)));

            cachedGainedNeeds = summary.gainedNeeds.Where(n => searchFilter.Matches(n.label)).ToList();
            cachedDisabledNeeds = summary.disabledNeeds.Where(n => searchFilter.Matches(n.label)).ToList();
            cachedAbilities = summary.abilities.Where(a => searchFilter.AbilityMatches(a)).ToList();
            cachedForcedTraits = summary.forcedTraits.Where(t => searchFilter.Matches(t.def.label)).ToList();
            cachedSuppressedTraits = summary.suppressedTraits.Where(t => searchFilter.Matches(t.def.label)).ToList();
            cachedMoodThoughts = summary.moodEffects.Keys.Where(t => searchFilter.Matches(t.label)).ToList();
            cachedNullifiedThoughts = summary.nullifiedThoughts.Keys.Where(t => searchFilter.Matches(t.label)).ToList();

            // Calculate Right Height
            float bioHeight = BiostatsTable.HeightForBiostats(summary.totalArc);
            cachedHeightRight = bioHeight + 15f;
            if (cachedApts.Any()) cachedHeightRight += 24f + (cachedApts.Count * 26f) + 10f;
            if (cachedPassions.Any()) cachedHeightRight += 24f + (cachedPassions.Count * 22f) + 10f;
            if (cachedStats.Any()) cachedHeightRight += 24f + (cachedStats.Count * 28f) + 10f;
            if (cachedCaps.Any()) cachedHeightRight += 24f + (cachedCaps.Count * 26f) + 10f;
            if (cachedDamage.Any()) cachedHeightRight += 24f + (cachedDamage.Count * 26f) + 10f;
            if (cachedGlobalFactors.Any()) cachedHeightRight += 24f + (cachedGlobalFactors.Count * 26f) + 10f;
            if (cachedChemicals.Any()) cachedHeightRight += 24f + (cachedChemicals.Count * 26f) + 10f;
            if (cachedSpecialEffects.Any()) cachedHeightRight += 24f + (cachedSpecialEffects.Count * 20f) + 10f;
            if (cachedGainedNeeds.Any() || cachedDisabledNeeds.Any()) cachedHeightRight += 24f + (cachedGainedNeeds.Count + cachedDisabledNeeds.Count) * 20f + 10f;
            if (cachedAbilities.Any()) cachedHeightRight += 24f + (cachedAbilities.Count * 24f) + 10f;
            if (cachedForcedTraits.Any() || cachedSuppressedTraits.Any()) cachedHeightRight += 24f + (cachedForcedTraits.Count + cachedSuppressedTraits.Count) * 26f + 10f;
            if (summary.disabledWorkTags != WorkTags.None && searchFilter.Matches("XS_Disabled".Translate())) cachedHeightRight += 24f + 26f + 10f;
            if (cachedMoodThoughts.Any() || cachedNullifiedThoughts.Any()) cachedHeightRight += 24f + (cachedMoodThoughts.Count + cachedNullifiedThoughts.Count) * 26f + 10f;


            cachedHeightRight += 40f;
        }

        private void DrawDescriptions(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);

            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, cachedHeightLeft);
            Widgets.BeginScrollView(innerRect, ref scrollPosDescriptions, viewRect);

            float curY = 0f;
            foreach (var gd in cachedFilteredGenes)
            {
                bool isOverridden = summary.overriddenGenes.Contains(gd.gene);
                Rect headerRect = new Rect(0f, curY, viewRect.width, 32f);

                if (Mouse.IsOver(headerRect)) Widgets.DrawHighlight(headerRect);
                if (Widgets.ButtonInvisible(headerRect))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(gd.gene));
                }

                Rect iconRect = new Rect(headerRect.x, headerRect.y, 32f, 32f);
                if (isOverridden) GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                else GUI.color = gd.gene.IconColor;
                GUI.DrawTexture(iconRect, gd.gene.Icon);
                GUI.color = Color.white;

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                string label = "<b>" + gd.highlightedLabel + "</b>";
                if (isOverridden) label = (label + " (" + "XS_Suppressed".Translate() + ")").Colorize(ColorLibrary.RedReadable);
                Widgets.Label(new Rect(iconRect.xMax + 5f, headerRect.y, headerRect.width - iconRect.width - 5f, headerRect.height), label);
                Text.Anchor = TextAnchor.UpperLeft;

                curY += 34f;

                GUI.color = isOverridden ? new Color(0.8f, 0.8f, 0.8f, 0.6f) : Color.white;
                if (!gd.highlightedDesc.NullOrEmpty())
                {
                    float h = Text.CalcHeight(gd.highlightedDesc, viewRect.width);
                    Widgets.Label(new Rect(5f, curY, viewRect.width - 5f, h), gd.highlightedDesc);
                    curY += h + 2f;
                }

                foreach (var hcd in gd.highlightedCustomDescs)
                {
                    string effectStr = ("XS_Effects".Translate().CapitalizeFirst() + ":").Colorize(ColoredText.TipSectionTitleColor) + $"\n  - {hcd}";
                    float h = Text.CalcHeight(effectStr, viewRect.width);
                    Widgets.Label(new Rect(5f, curY, viewRect.width - 5f, h), effectStr);
                    curY += h + 2f;
                }
                GUI.color = Color.white;

                curY += 5f;
                Widgets.DrawLineHorizontal(0f, curY, viewRect.width);
                curY += 10f;
            }

            Widgets.EndScrollView();
        }

        private void DrawStats(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);

            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, cachedHeightRight);
            Widgets.BeginScrollView(innerRect, ref scrollPosStats, viewRect);

            float curY = 0f;

            // Biostats
            float bioHeight = BiostatsTable.HeightForBiostats(summary.totalArc);
            BiostatsTable.Draw(new Rect(0f, curY, viewRect.width, bioHeight), summary.totalCpx, summary.totalMet, summary.totalArc, false, false, -1);
            curY += bioHeight + 15f;

            // Aptitudes
            if (cachedApts.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_Aptitudes".Translate() + "</b>");
                curY += 24f;
                foreach (var apt in cachedApts)
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), searchFilter.Highlight(apt.skill.LabelCap), (apt.FinalLevel > 0 ? "+" : "") + apt.FinalLevel, GetSkillTooltip(apt));
                    curY += 26f;
                }
                curY += 10f;
            }

            // Passions
            if (cachedPassions.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_Passions".Translate() + "</b>");
                curY += 24f;
                foreach (var pm in cachedPassions)
                {
                    string label = (pm.modType == PassionMod.PassionModType.AddOneLevel) ? "XS_PassionAdd".Translate() : "XS_PassionRemove".Translate();
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + searchFilter.Highlight(pm.skill.LabelCap) + ": " + label);
                    curY += 22f;
                }
                curY += 10f;
            }

            // Stats
            if (cachedStats.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_StatModifiers".Translate() + "</b>");
                curY += 24f;
                foreach (var ss in cachedStats)
                {
                    string valueStr = "";
                    if (ss.FinalOffset != 0) valueStr += (ss.FinalOffset > 0 ? "+" : "") + ss.stat.ValueToString(ss.FinalOffset, ss.stat.toStringNumberSense);
                    if (ss.FinalFactor != 1f)
                    {
                        if (!valueStr.NullOrEmpty()) valueStr += ", ";
                        valueStr += "x" + ss.FinalFactor.ToStringPercent();
                    }

                    DrawStatRow(new Rect(0f, curY, viewRect.width, 26f), searchFilter.Highlight(ss.stat.LabelCap), valueStr, GetStatTooltip(ss));
                    curY += 28f;
                }
                curY += 10f;
            }

            // Capacities
            if (cachedCaps.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_Capacities".Translate() + "</b>");
                curY += 24f;
                foreach (var cs in cachedCaps)
                {
                    string valStr = "";
                    if (cs.FinalOffset != 0f) valStr += (cs.FinalOffset > 0 ? "+" : "") + cs.FinalOffset.ToStringPercent();
                    if (cs.FinalFactor != 1f)
                    {
                        if (!valStr.NullOrEmpty()) valStr += ", ";
                        valStr += "x" + cs.FinalFactor.ToStringPercent();
                    }
                    if (cs.FinalMax.HasValue)
                    {
                        if (!valStr.NullOrEmpty()) valStr += ", ";
                        valStr += "max " + cs.FinalMax.Value.ToStringPercent();
                    }

                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), searchFilter.Highlight(cs.capacity.LabelCap), valStr, GetCapacityTooltip(cs));
                    curY += 26f;
                }
                curY += 10f;
            }

            // Damage Factors
            if (cachedDamage.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_DamageFactors".Translate() + "</b>");
                curY += 24f;
                foreach (var ds in cachedDamage)
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), searchFilter.Highlight("XS_DamageUnit".Translate(ds.damageDef.LabelCap)), "x" + ds.FinalFactor.ToStringPercent(), ds.damageDef.description);
                    curY += 26f;
                }
                curY += 10f;
            }

            // Global Factors
            if (cachedGlobalFactors.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_GlobalFactors".Translate() + "</b>");
                curY += 24f;
                foreach (var line in cachedGlobalFactors)
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), searchFilter.Highlight(line.First), line.Second);
                    curY += 26f;
                }
                curY += 10f;
            }

            // Chemicals
            if (cachedChemicals.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_ChemicalEffects".Translate() + "</b>");
                curY += 24f;
                foreach (var chem in cachedChemicals)
                {
                    if (chem.overdoseFactor != 1f)
                    {
                        DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), "XS_OverdoseChance".Translate(chem.highlightedLabel), "x" + chem.overdoseFactor.ToStringPercent());
                        curY += 26f;
                    }
                    if (chem.toleranceFactor != 1f)
                    {
                        DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), "XS_ToleranceBuildup".Translate(chem.highlightedLabel), "x" + chem.toleranceFactor.ToStringPercent());
                        curY += 26f;
                    }
                }
                curY += 10f;
            }

            // Special Effects
            if (cachedSpecialEffects.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_SpecialEffects".Translate() + "</b>");
                curY += 24f;
                foreach (var effect in cachedSpecialEffects)
                {
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + effect);
                    curY += 20f;
                }
                curY += 10f;
            }

            // Needs
            if (cachedGainedNeeds.Any() || cachedDisabledNeeds.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_NeedsAndConditions".Translate() + "</b>");
                curY += 24f;
                foreach (var n in cachedGainedNeeds)
                {
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + searchFilter.Highlight("XS_AddsNeed".Translate(n.LabelCap)));
                    curY += 20f;
                }
                foreach (var n in cachedDisabledNeeds)
                {
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + searchFilter.Highlight("XS_DisablesNeed".Translate(n.LabelCap)));
                    curY += 20f;
                }
                curY += 10f;
            }

            // Abilities
            if (cachedAbilities.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_GainedAbilities".Translate() + "</b>");
                curY += 24f;
                foreach (var ab in cachedAbilities)
                {
                    Rect r = new Rect(0f, curY, viewRect.width, 24f);
                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                    Rect iconR = new Rect(r.x, r.y, 22f, 22f);
                    GUI.DrawTexture(iconR, ab.uiIcon);

                    Rect labelR = new Rect(iconR.xMax + 5f, r.y, r.width - iconR.width - 5f, r.height);
                    Widgets.Label(labelR, searchFilter.Highlight(ab.LabelCap));

                    if (Widgets.ButtonInvisible(r))
                    {
                        Find.WindowStack.Add(new Dialog_InfoCard(ab));
                    }
                    TooltipHandler.TipRegion(r, ab.description);
                    curY += 24f;
                }
                curY += 10f;
            }

            // Traits
            if (cachedForcedTraits.Any() || cachedSuppressedTraits.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_Traits".Translate() + "</b>");
                curY += 24f;
                foreach (var trData in cachedForcedTraits)
                {
                    Rect r = new Rect(0f, curY, viewRect.width, 24f);
                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                    string label = trData.def.DataAtDegree(trData.degree).label;
                    Widgets.Label(r, " - " + searchFilter.Highlight("XS_Forced".Translate(label.CapitalizeFirst())));
                    TooltipHandler.TipRegion(r, trData.def.DataAtDegree(trData.degree).description);
                    curY += 26f;
                }
                foreach (var trData in cachedSuppressedTraits)
                {
                    Rect r = new Rect(0f, curY, viewRect.width, 24f);
                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                    string label = trData.def.DataAtDegree(trData.degree).label;
                    Widgets.Label(r, " - " + searchFilter.Highlight("XS_SuppressedTrait".Translate(label.CapitalizeFirst())));
                    TooltipHandler.TipRegion(r, trData.def.DataAtDegree(trData.degree).description);
                    curY += 26f;
                }
                curY += 10f;
            }

            // Work
            if (summary.disabledWorkTags != WorkTags.None && searchFilter.Matches("XS_Disabled".Translate()))
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_WorkLimitations".Translate() + "</b>");
                curY += 24f;
                string workLabel = summary.disabledWorkTags.LabelTranslated().CapitalizeFirst();
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), " - " + searchFilter.Highlight("XS_Disabled".Translate(workLabel)));
                curY += 26f;
                curY += 10f;
            }

            // Mood
            if (cachedMoodThoughts.Any() || cachedNullifiedThoughts.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_MoodAndThoughts".Translate() + "</b>");
                curY += 24f;
                foreach (var def in cachedMoodThoughts)
                {
                    float val = summary.moodEffects[def];
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), " - " + searchFilter.Highlight("XS_Required".Translate(def.LabelCap)), val.ToStringWithSign("0.##"), def.description);
                    curY += 26f;
                }
                foreach (var def in cachedNullifiedThoughts)
                {
                    string val = summary.nullifiedThoughts[def];
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), " - " + searchFilter.Highlight("XS_Removes".Translate(def.LabelCap)), val, def.description);
                    curY += 26f;
                }
                curY += 10f;
            }

            Widgets.EndScrollView();
        }

        private void DrawStatRow(Rect rect, string label, string value, string tooltip = null)
        {
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            Widgets.Label(rect.LeftPart(0.7f), label);
            Widgets.Label(rect.RightPart(0.3f), value);
            if (!tooltip.NullOrEmpty()) TooltipHandler.TipRegion(rect, tooltip);
        }

        private string GetStatTooltip(XenotypeSummaryGenerator.StatSummary ss)
        {
            string text = ss.stat.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + ss.stat.description;
            if (ss.offsets.Any() || ss.factors.Any())
            {
                text += "\n\n" + "StatModifiers".Translate() + ":";
                foreach (var off in ss.offsets)
                {
                    text += $"\n  - {off.gene.LabelCap}: {ss.stat.ValueToString(off.value, ss.stat.toStringNumberSense, true)}";
                }
                foreach (var fac in ss.factors)
                {
                    text += $"\n  - {fac.gene.LabelCap}: x{fac.value.ToStringPercent()}";
                }
            }
            return text;
        }

        private string GetCapacityTooltip(XenotypeSummaryGenerator.CapacitySummary cs)
        {
            string text = cs.capacity.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + cs.capacity.description;
            if (cs.offsets.Any() || cs.factors.Any() || cs.setMaxes.Any())
            {
                text += "\n\n" + "StatModifiers".Translate() + ":";
                foreach (var off in cs.offsets)
                    text += $"\n  - {off.gene.LabelCap}: {off.value.ToStringWithSign("0.##")}%";
                foreach (var fac in cs.factors)
                    text += $"\n  - {fac.gene.LabelCap}: x{fac.value.ToStringPercent()}";
                foreach (var mx in cs.setMaxes)
                    text += $"\n  - {mx.gene.LabelCap}: max {mx.value.ToStringPercent()}";
            }
            return text;
        }

        private string GetSkillTooltip(XenotypeSummaryGenerator.AptitudeSummary apt)
        {
            string text = apt.skill.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n\n" + apt.skill.description;
            text += "\n\n" + "StatModifiers".Translate() + ":";
            foreach (var a in apt.aptitudes)
                text += $"\n  - {a.gene.LabelCap}: {(a.value >= 0 ? "+" : "")}{a.value}";
            return text;
        }
    }
}