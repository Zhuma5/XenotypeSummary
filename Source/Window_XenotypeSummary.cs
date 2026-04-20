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
            Text.Font = GameFont.Small;

            Rect contentRect = inRect.AtZero();
            contentRect.yMin += 40f;
            contentRect.height -= 40f;

            float colWidth = (contentRect.width - 20f) / 2f;
            Rect leftRect = new Rect(contentRect.x, contentRect.y, colWidth, contentRect.height);
            Rect rightRect = new Rect(contentRect.x + colWidth + 20f, contentRect.y, colWidth, contentRect.height);

            // Left: Descriptions
            DrawDescriptions(leftRect);

            // Right: Stats
            DrawStats(rightRect);
        }

        private void DrawDescriptions(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);


            float height = 0f;
            foreach (var gene in summary.allGenes)
            {
                if (gene.description.NullOrEmpty() && (gene.customEffectDescriptions == null || !gene.customEffectDescriptions.Any())) continue;


                height += 34f; // Header height
                height += Text.CalcHeight(gene.description, innerRect.width) + 5f;
                if (gene.customEffectDescriptions != null)
                {
                    foreach (var effect in gene.customEffectDescriptions)
                        height += Text.CalcHeight("XS_Effects".Translate().CapitalizeFirst() + ": \n" + effect, innerRect.width) + 2f;
                }
                height += 15f; // Padding + Line
            }

            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, height);
            Widgets.BeginScrollView(innerRect, ref scrollPosDescriptions, viewRect);


            float curY = 0f;
            foreach (var gene in summary.allGenes)
            {
                bool hasMainDesc = !gene.description.NullOrEmpty();
                bool hasCustomDescs = gene.customEffectDescriptions != null && gene.customEffectDescriptions.Any();
                if (!hasMainDesc && !hasCustomDescs) continue;

                bool isOverridden = summary.overriddenGenes.Contains(gene);
                Rect headerRect = new Rect(0f, curY, viewRect.width, 32f);

                // Interaction + Highlight

                if (Mouse.IsOver(headerRect)) Widgets.DrawHighlight(headerRect);
                if (Widgets.ButtonInvisible(headerRect))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(gene));
                }

                // Gene Icon

                Rect iconRect = new Rect(headerRect.x, headerRect.y, 32f, 32f);
                if (isOverridden) GUI.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                else GUI.color = gene.IconColor;
                GUI.DrawTexture(iconRect, gene.Icon);
                GUI.color = Color.white;

                // Gene Name

                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                string label = "<b>" + gene.LabelCap + "</b>";
                if (isOverridden) label = (label + " (" + "XS_Suppressed".Translate() + ")").Colorize(ColorLibrary.RedReadable);
                Widgets.Label(new Rect(iconRect.xMax + 5f, headerRect.y, headerRect.width - iconRect.width - 5f, headerRect.height), label);
                Text.Anchor = TextAnchor.UpperLeft;


                curY += 34f;

                // Description
                GUI.color = isOverridden ? new Color(0.8f, 0.8f, 0.8f, 0.6f) : Color.white;
                if (hasMainDesc)
                {
                    float h = Text.CalcHeight(gene.description, viewRect.width);
                    Widgets.Label(new Rect(5f, curY, viewRect.width - 5f, h), gene.description);
                    curY += h + 2f;
                }


                if (hasCustomDescs)
                {
                    foreach (var effect in gene.customEffectDescriptions)
                    {
                        string effectStr = ("XS_Effects".Translate().CapitalizeFirst() + ":").Colorize(ColoredText.TipSectionTitleColor) + $"\n  - {effect.ResolveTags()}";
                        float h = Text.CalcHeight(effectStr, viewRect.width);
                        Widgets.Label(new Rect(5f, curY, viewRect.width - 5f, h), effectStr);
                        curY += h + 2f;
                    }
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

            float bioHeight = BiostatsTable.HeightForBiostats(summary.totalArc);
            float height = bioHeight + 15f;


            if (summary.aptitudeSummaries.Any()) height += 24f + (summary.aptitudeSummaries.Count * 26f) + 10f;
            if (summary.passionMods.Any()) height += 24f + (summary.passionMods.Count * 22f) + 10f;
            if (summary.statSummaries.Any()) height += 24f + (summary.statSummaries.Count * 28f) + 10f;
            if (summary.capacitySummaries.Any()) height += 24f + (summary.capacitySummaries.Count * 26f) + 10f;
            if (summary.damageSummaries.Any()) height += 24f + (summary.damageSummaries.Count * 26f) + 10f;


            int globalLines = 0;
            if (summary.totalPainOffset != 0f) globalLines++;
            if (summary.totalPainFactor != 1f) globalLines++;
            if (summary.totalFoodPoisoningFactor != 1f) globalLines++;
            if (summary.totalSocialFightFactor != 1f) globalLines++;
            if (summary.totalAggroMentalBreakFactor != 1f) globalLines++;
            if (summary.totalPrisonBreakFactor != 1f) globalLines++;
            if (summary.totalRomanceChanceFactor != 1f) globalLines++;
            if (summary.totalLovinMTBFactor != 1f) globalLines++;
            if (summary.totalResourceLossPerDay != 0f) globalLines++;
            if (globalLines > 0) height += 24f + (globalLines * 26f) + 10f;

            if (summary.overdoseFactors.Any() || summary.toleranceFactors.Any())
                height += 24f + (summary.overdoseFactors.Count + summary.toleranceFactors.Count) * 26f + 10f;

            int specialCount = 0;
            if (summary.toxGasImmunity) specialCount++;
            if (summary.vacuumBurnImmunity) specialCount++;
            if (summary.ignoreDarkness) specialCount++;
            if (summary.sterilize) specialCount++;
            if (summary.preventPermanentWounds) specialCount++;
            if (summary.dontMindRawFood) specialCount++;
            specialCount += summary.immunityHediffs.Count;
            specialCount += summary.cannotGiveHediffs.Count;
            if (specialCount > 0) height += 24f + (specialCount * 20f) + 10f;

            if (summary.gainedNeeds.Any() || summary.disabledNeeds.Any())
                height += 24f + (summary.gainedNeeds.Count + summary.disabledNeeds.Count) * 20f + 10f;

            if (summary.abilities.Any()) height += 24f + (summary.abilities.Count * 24f) + 10f;
            if (summary.forcedTraits.Any() || summary.suppressedTraits.Any())
                height += 24f + (summary.forcedTraits.Count + summary.suppressedTraits.Count) * 26f + 10f;


            if (summary.disabledWorkTags != WorkTags.None) height += 24f + 26f + 10f;

            if (summary.moodEffects.Any() || summary.nullifiedThoughts.Any())
                height += 24f + (summary.moodEffects.Count + summary.nullifiedThoughts.Count) * 26f + 10f;

            height += 40f; // Final padding

            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, height);
            Widgets.BeginScrollView(innerRect, ref scrollPosStats, viewRect);

            float curY = 0f;

            // Biostats
            BiostatsTable.Draw(new Rect(0f, curY, viewRect.width, bioHeight), summary.totalCpx, summary.totalMet, summary.totalArc, false, false, -1);
            curY += bioHeight + 15f;

            // Aptitudes (Skills)
            if (summary.aptitudeSummaries.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_Aptitudes".Translate() + "</b>");
                curY += 24f;
                foreach (var apt in summary.aptitudeSummaries.Values.OrderBy(a => a.skill.label))
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), apt.skill.LabelCap, (apt.FinalLevel > 0 ? "+" : "") + apt.FinalLevel, GetSkillTooltip(apt));
                    curY += 26f;
                }
                curY += 10f;
            }

            // Passions
            if (summary.passionMods.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_Passions".Translate() + "</b>");
                curY += 24f;
                foreach (var pm in summary.passionMods)
                {
                    string label = (pm.modType == PassionMod.PassionModType.AddOneLevel) ? "XS_PassionAdd".Translate() : "XS_PassionRemove".Translate();
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + pm.skill.LabelCap + ": " + label);
                    curY += 22f;
                }
                curY += 10f;
            }

            // Stats
            if (summary.statSummaries.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_StatModifiers".Translate() + "</b>");
                curY += 24f;
                foreach (var ss in summary.statSummaries.Values.OrderBy(s => s.stat.label))
                {
                    string valueStr = "";
                    if (ss.FinalOffset != 0) valueStr += (ss.FinalOffset > 0 ? "+" : "") + ss.stat.ValueToString(ss.FinalOffset, ss.stat.toStringNumberSense);
                    if (ss.FinalFactor != 1f)
                    {
                        if (!valueStr.NullOrEmpty()) valueStr += ", ";
                        valueStr += "x" + ss.FinalFactor.ToStringPercent();
                    }

                    DrawStatRow(new Rect(0f, curY, viewRect.width, 26f), ss.stat.LabelCap, valueStr, GetStatTooltip(ss));
                    curY += 28f;
                }
                curY += 10f;
            }

            // Capacities
            if (summary.capacitySummaries.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_Capacities".Translate() + "</b>");
                curY += 24f;
                foreach (var cs in summary.capacitySummaries.Values.OrderBy(c => c.capacity.label))
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

                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), cs.capacity.LabelCap, valStr, GetCapacityTooltip(cs));
                    curY += 26f;
                }
                curY += 10f;
            }

            // Damage Factors
            if (summary.damageSummaries.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_DamageFactors".Translate() + "</b>");
                curY += 24f;
                foreach (var ds in summary.damageSummaries.Values.OrderBy(d => d.damageDef.label))
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), "XS_DamageUnit".Translate(ds.damageDef.LabelCap), "x" + ds.FinalFactor.ToStringPercent(), ds.damageDef.description);
                    curY += 26f;
                }
                curY += 10f;
            }

            // Global Factors
            var globalFactorLines = new List<Pair<string, string>>();
            if (summary.totalPainOffset != 0f) globalFactorLines.Add(new Pair<string, string>("XS_PainOffset".Translate(), (summary.totalPainOffset * 100f).ToString("+#;-#") + "%"));
            if (summary.totalPainFactor != 1f) globalFactorLines.Add(new Pair<string, string>("XS_PainFactor".Translate(), "x" + summary.totalPainFactor.ToStringPercent()));
            if (summary.totalFoodPoisoningFactor != 1f) globalFactorLines.Add(new Pair<string, string>("XS_FoodPoisoningChance".Translate(), "x" + summary.totalFoodPoisoningFactor.ToStringPercent()));
            if (summary.totalSocialFightFactor != 1f) globalFactorLines.Add(new Pair<string, string>("XS_SocialFightChance".Translate(), "x" + summary.totalSocialFightFactor.ToStringPercent()));
            if (summary.totalAggroMentalBreakFactor != 1f) globalFactorLines.Add(new Pair<string, string>("XS_AggroMentalBreakChance".Translate(), "x" + summary.totalAggroMentalBreakFactor.ToStringPercent()));
            if (summary.totalPrisonBreakFactor != 1f) globalFactorLines.Add(new Pair<string, string>("XS_PrisonBreakChance".Translate(), "x" + summary.totalPrisonBreakFactor.ToStringPercent()));
            if (summary.totalRomanceChanceFactor != 1f) globalFactorLines.Add(new Pair<string, string>("XS_RomanceChance".Translate(), "x" + summary.totalRomanceChanceFactor.ToStringPercent()));
            if (summary.totalLovinMTBFactor != 1f) globalFactorLines.Add(new Pair<string, string>("XS_LovinMTB".Translate(), "x" + summary.totalLovinMTBFactor.ToStringPercent()));
            if (summary.totalResourceLossPerDay != 0f) globalFactorLines.Add(new Pair<string, string>("XS_DailyResourceLoss".Translate(), summary.totalResourceLossPerDay.ToString("0.##")));

            if (globalFactorLines.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_GlobalFactors".Translate() + "</b>");
                curY += 24f;
                foreach (var line in globalFactorLines)
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), line.First, line.Second);
                    curY += 26f;
                }
                curY += 10f;
            }

            // Chemical Effects
            if (summary.overdoseFactors.Any() || summary.toleranceFactors.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_ChemicalEffects".Translate() + "</b>");
                curY += 24f;
                foreach (var kvp in summary.overdoseFactors)
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), "XS_OverdoseChance".Translate(kvp.Key.LabelCap), "x" + kvp.Value.ToStringPercent());
                    curY += 26f;
                }
                foreach (var kvp in summary.toleranceFactors)
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), "XS_ToleranceBuildup".Translate(kvp.Key.LabelCap), "x" + kvp.Value.ToStringPercent());
                    curY += 26f;
                }
                curY += 10f;
            }

            // Special Effects / Immunities
            int specialCountRecap = 0;
            if (summary.toxGasImmunity) specialCountRecap++;
            if (summary.vacuumBurnImmunity) specialCountRecap++;
            if (summary.ignoreDarkness) specialCountRecap++;
            if (summary.sterilize) specialCountRecap++;
            if (summary.preventPermanentWounds) specialCountRecap++;
            if (summary.dontMindRawFood) specialCountRecap++;
            specialCountRecap += summary.immunityHediffs.Count;

            if (specialCountRecap > 0 || summary.cannotGiveHediffs.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_SpecialEffects".Translate() + "</b>");
                curY += 24f;
                if (summary.toxGasImmunity) { Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_ToxGasImmunity".Translate()); curY += 20f; }
                if (summary.vacuumBurnImmunity) { Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_VacuumBurnImmunity".Translate()); curY += 20f; }
                if (summary.ignoreDarkness) { Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_IgnoreDarkness".Translate()); curY += 20f; }
                if (summary.sterilize) { Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_Sterilized".Translate()); curY += 20f; }
                if (summary.preventPermanentWounds) { Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_PreventPermanentWounds".Translate()); curY += 20f; }
                if (summary.dontMindRawFood) { Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_DontMindRawFood".Translate()); curY += 20f; }
                foreach (var h in summary.immunityHediffs)
                {
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_ImmuneTo".Translate(h.LabelCap));
                    curY += 20f;
                }
                foreach (var h in summary.cannotGiveHediffs)
                {
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_CausesImmunityTo".Translate(h.LabelCap));
                    curY += 20f;
                }
                curY += 10f;
            }

            // Needs
            if (summary.gainedNeeds.Any() || summary.disabledNeeds.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_NeedsAndConditions".Translate() + "</b>");
                curY += 24f;
                foreach (var n in summary.gainedNeeds)
                {
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_AddsNeed".Translate(n.LabelCap));
                    curY += 20f;
                }
                foreach (var n in summary.disabledNeeds)
                {
                    Widgets.Label(new Rect(0f, curY, viewRect.width, 20f), " - " + "XS_DisablesNeed".Translate(n.LabelCap));
                    curY += 20f;
                }
                curY += 10f;
            }

            // Abilities
            if (summary.abilities.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_GainedAbilities".Translate() + "</b>");
                curY += 24f;
                foreach (var ab in summary.abilities)
                {
                    Rect r = new Rect(0f, curY, viewRect.width, 24f);
                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);


                    Rect iconR = new Rect(r.x, r.y, 22f, 22f);
                    GUI.DrawTexture(iconR, ab.uiIcon);


                    Rect labelR = new Rect(iconR.xMax + 5f, r.y, r.width - iconR.width - 5f, r.height);
                    Widgets.Label(labelR, ab.LabelCap);


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
            if (summary.forcedTraits.Any() || summary.suppressedTraits.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_Traits".Translate() + "</b>");
                curY += 24f;
                foreach (var trData in summary.forcedTraits)
                {
                    Rect r = new Rect(0f, curY, viewRect.width, 24f);

                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);


                    string label = trData.def.DataAtDegree(trData.degree).label;
                    Widgets.Label(r, " - " + "XS_Forced".Translate(label.CapitalizeFirst()));
                    TooltipHandler.TipRegion(r, trData.def.DataAtDegree(trData.degree).description);
                    curY += 26f;
                }
                foreach (var trData in summary.suppressedTraits)
                {
                    Rect r = new Rect(0f, curY, viewRect.width, 24f);

                    if (Mouse.IsOver(r)) Widgets.DrawHighlight(r);

                    string label = trData.def.DataAtDegree(trData.degree).label;
                    Widgets.Label(r, " - " + "XS_SuppressedTrait".Translate(label.CapitalizeFirst()));
                    TooltipHandler.TipRegion(r, trData.def.DataAtDegree(trData.degree).description);
                    curY += 26f;
                }
                curY += 10f;
            }

            // Work Limitations
            if (summary.disabledWorkTags != WorkTags.None)
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_WorkLimitations".Translate() + "</b>");
                curY += 24f;
                string workLabel = summary.disabledWorkTags.LabelTranslated().CapitalizeFirst();
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), " - " + "XS_Disabled".Translate(workLabel));
                curY += 26f;
                curY += 10f;
            }

            // Mood & Thoughts
            if (summary.moodEffects.Any() || summary.nullifiedThoughts.Any())
            {
                Widgets.Label(new Rect(0f, curY, viewRect.width, 24f), "<b>" + "XS_MoodAndThoughts".Translate() + "</b>");
                curY += 24f;
                foreach (var kvp in summary.moodEffects)
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), " - " + "XS_Required".Translate(kvp.Key.LabelCap), kvp.Value.ToStringWithSign("0.##"), kvp.Key.description);
                    curY += 26f;
                }
                foreach (var kvp in summary.nullifiedThoughts)
                {
                    DrawStatRow(new Rect(0f, curY, viewRect.width, 24f), " - " + "XS_Removes".Translate(kvp.Key.LabelCap), kvp.Value, kvp.Key.description);
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