using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace XenotypeSummary
{
    public class XenotypeSummaryGenerator
    {
        public List<GeneDef> allGenes;
        public HashSet<GeneDef> overriddenGenes;


        public List<string> descriptions = new List<string>();
        public Dictionary<StatDef, StatSummary> statSummaries = new Dictionary<StatDef, StatSummary>();
        public Dictionary<PawnCapacityDef, CapacitySummary> capacitySummaries = new Dictionary<PawnCapacityDef, CapacitySummary>();
        public Dictionary<SkillDef, AptitudeSummary> aptitudeSummaries = new Dictionary<SkillDef, AptitudeSummary>();
        public Dictionary<DamageDef, DamageSummary> damageSummaries = new Dictionary<DamageDef, DamageSummary>();


        public float totalPainOffset = 0f;
        public float totalPainFactor = 1f;
        public float totalFoodPoisoningFactor = 1f;
        public float totalSocialFightFactor = 1f;
        public float totalAggroMentalBreakFactor = 1f;
        public float totalPrisonBreakFactor = 1f;
        public float totalRomanceChanceFactor = 1f;
        public float totalLovinMTBFactor = 1f;

        // Other specific aggregations
        public List<AbilityDef> abilities = new List<AbilityDef>();
        public List<GeneticTraitData> forcedTraits = new List<GeneticTraitData>();
        public List<GeneticTraitData> suppressedTraits = new List<GeneticTraitData>();

        public int totalMet;
        public int totalCpx;
        public int totalArc;

        // Immunities & Special
        public bool toxGasImmunity;
        public bool vacuumBurnImmunity;
        public bool ignoreDarkness;
        public bool sterilize;
        public bool preventPermanentWounds;
        public bool dontMindRawFood;
        public HashSet<HediffDef> immunityHediffs = new HashSet<HediffDef>();
        public HashSet<HediffDef> cannotGiveHediffs = new HashSet<HediffDef>();
        public List<PassionMod> passionMods = new List<PassionMod>();


        public WorkTags disabledWorkTags = WorkTags.None;
        public Dictionary<ChemicalDef, float> overdoseFactors = new Dictionary<ChemicalDef, float>();
        public Dictionary<ChemicalDef, float> toleranceFactors = new Dictionary<ChemicalDef, float>();
        public HashSet<NeedDef> gainedNeeds = new HashSet<NeedDef>();
        public HashSet<NeedDef> disabledNeeds = new HashSet<NeedDef>();
        public float totalResourceLossPerDay = 0f;
        public Dictionary<ThoughtDef, float> moodEffects = new Dictionary<ThoughtDef, float>();
        public Dictionary<ThoughtDef, string> nullifiedThoughts = new Dictionary<ThoughtDef, string>();

        public XenotypeSummaryGenerator(IEnumerable<GeneDef> genes, IEnumerable<GeneDef> overridden = null)
        {
            this.overriddenGenes = overridden?.ToHashSet() ?? new HashSet<GeneDef>();
            this.allGenes = genes.OrderBy(g => g.displayCategory?.displayPriorityInXenotype ?? 0).ThenBy(g => g.displayOrderInCategory).ToList();
            Process();
        }

        private void Process()
        {
            foreach (var gene in allGenes)
            {
                bool active = !overriddenGenes.Contains(gene);

                // Descriptions
                if (!gene.description.NullOrEmpty())
                    descriptions.Add(gene.description);
                if (gene.customEffectDescriptions != null)
                    descriptions.AddRange(gene.customEffectDescriptions);

                if (active)
                {
                    totalMet += gene.biostatMet;
                    totalCpx += gene.biostatCpx;
                    totalArc += gene.biostatArc;
                }

                // Stats
                if (active && gene.statOffsets != null)
                {
                    foreach (var mod in gene.statOffsets)
                    {
                        if (!statSummaries.ContainsKey(mod.stat)) statSummaries[mod.stat] = new StatSummary { stat = mod.stat };
                        statSummaries[mod.stat].offsets.Add(new Contributor<float> { gene = gene, value = mod.value });
                    }
                }
                if (active && gene.statFactors != null)
                {
                    foreach (var mod in gene.statFactors)
                    {
                        if (!statSummaries.ContainsKey(mod.stat)) statSummaries[mod.stat] = new StatSummary { stat = mod.stat };
                        statSummaries[mod.stat].factors.Add(new Contributor<float> { gene = gene, value = mod.value });
                    }
                }

                // Capacities
                if (active && gene.capMods != null)
                {
                    foreach (var mod in gene.capMods)
                    {
                        if (!capacitySummaries.ContainsKey(mod.capacity)) capacitySummaries[mod.capacity] = new CapacitySummary { capacity = mod.capacity };
                        var cs = capacitySummaries[mod.capacity];
                        if (mod.offset != 0f) cs.offsets.Add(new Contributor<float> { gene = gene, value = mod.offset });
                        if (mod.postFactor != 1f) cs.factors.Add(new Contributor<float> { gene = gene, value = mod.postFactor });
                        if (mod.setMax != 999f) cs.setMaxes.Add(new Contributor<float> { gene = gene, value = mod.setMax });
                    }
                }

                // Aptitudes
                if (active && gene.aptitudes != null)
                {
                    foreach (var apt in gene.aptitudes)
                    {
                        if (!aptitudeSummaries.ContainsKey(apt.skill)) aptitudeSummaries[apt.skill] = new AptitudeSummary { skill = apt.skill };
                        aptitudeSummaries[apt.skill].aptitudes.Add(new Contributor<int> { gene = gene, value = apt.level });
                    }
                }

                // Damage Factors
                if (active && gene.damageFactors != null)
                {
                    foreach (var df in gene.damageFactors)
                    {
                        if (!damageSummaries.ContainsKey(df.damageDef)) damageSummaries[df.damageDef] = new DamageSummary { damageDef = df.damageDef };
                        damageSummaries[df.damageDef].factors.Add(new Contributor<float> { gene = gene, value = df.factor });
                    }
                }

                // Special fields
                if (active)
                {
                    totalPainOffset += gene.painOffset;
                    totalPainFactor *= gene.painFactor;
                    totalFoodPoisoningFactor *= gene.foodPoisoningChanceFactor;
                    totalSocialFightFactor *= gene.socialFightChanceFactor;
                    totalAggroMentalBreakFactor *= gene.aggroMentalBreakSelectionChanceFactor;
                    totalPrisonBreakFactor *= gene.prisonBreakMTBFactor;
                    totalRomanceChanceFactor *= gene.missingGeneRomanceChanceFactor;
                    totalLovinMTBFactor *= gene.lovinMTBFactor;
                }

                // Abilities
                if (active && gene.abilities != null)
                {
                    foreach (var ab in gene.abilities)
                    {
                        if (!abilities.Contains(ab)) abilities.Add(ab);
                    }
                }

                // Traits
                if (active && gene.forcedTraits != null)
                {
                    foreach (var tr in gene.forcedTraits)
                    {
                        if (!forcedTraits.Any(x => x.def == tr.def && x.degree == tr.degree)) forcedTraits.Add(tr);
                    }
                }
                if (active && gene.suppressedTraits != null)
                {
                    foreach (var tr in gene.suppressedTraits)
                    {
                        if (!suppressedTraits.Any(x => x.def == tr.def && x.degree == tr.degree)) suppressedTraits.Add(tr);
                    }
                }

                // Immunities & Special
                if (active)
                {
                    if (gene.immuneToToxGasExposure) toxGasImmunity = true;
                    if (gene.immuneToVacuumBurns) vacuumBurnImmunity = true;
                    if (gene.ignoreDarkness) ignoreDarkness = true;
                    if (gene.sterilize) sterilize = true;
                    if (gene.preventPermanentWounds) preventPermanentWounds = true;
                    if (gene.dontMindRawFood) dontMindRawFood = true;
                    if (gene.makeImmuneTo != null)
                    {
                        foreach (var h in gene.makeImmuneTo) immunityHediffs.Add(h);
                    }
                    if (gene.hediffGiversCannotGive != null)
                    {
                        foreach (var h in gene.hediffGiversCannotGive) cannotGiveHediffs.Add(h);
                    }
                    if (gene.passionMod != null) passionMods.Add(gene.passionMod);


                    disabledWorkTags |= gene.disabledWorkTags;
                    if (gene.chemical != null)
                    {
                        if (gene.overdoseChanceFactor != 1f)
                        {
                            if (!overdoseFactors.ContainsKey(gene.chemical)) overdoseFactors[gene.chemical] = 1f;
                            overdoseFactors[gene.chemical] *= gene.overdoseChanceFactor;
                        }
                        if (gene.toleranceBuildupFactor != 1f)
                        {
                            if (!toleranceFactors.ContainsKey(gene.chemical)) toleranceFactors[gene.chemical] = 1f;
                            toleranceFactors[gene.chemical] *= gene.toleranceBuildupFactor;
                        }
                    }
                    if (gene.enablesNeeds != null)
                    {
                        foreach (var n in gene.enablesNeeds) gainedNeeds.Add(n);
                    }
                    if (gene.disablesNeeds != null)
                    {
                        foreach (var n in gene.disablesNeeds) disabledNeeds.Add(n);
                    }
                    totalResourceLossPerDay += gene.resourceLossPerDay;

                    // Mood interactions
                    if (GeneThoughtCache.RequiredBy.TryGetValue(gene, out var reqThoughts))
                    {
                        foreach (var th in reqThoughts)
                        {
                            var stage = th.stages.FirstOrDefault(s => s.baseMoodEffect != 0f);
                            if (stage != null) moodEffects[th] = stage.baseMoodEffect;
                        }
                    }

                    if (GeneThoughtCache.NullifiedBy.TryGetValue(gene, out var nullThoughts))
                    {
                        foreach (var th in nullThoughts)
                        {
                            var stage = th.stages.FirstOrDefault(s => s.baseMoodEffect != 0f);
                            if (stage != null) nullifiedThoughts[th] = stage.LabelCap + " (" + stage.baseMoodEffect.ToStringWithSign("0.##") + ")";
                        }
                    }
                }
            }
        }

        public class Contributor<T>
        {
            public GeneDef gene;
            public T value;
        }

        public class StatSummary
        {
            public StatDef stat;
            public List<Contributor<float>> offsets = new List<Contributor<float>>();
            public List<Contributor<float>> factors = new List<Contributor<float>>();


            public float FinalOffset => offsets.Sum(o => o.value);
            public float FinalFactor => factors.Aggregate(1f, (acc, f) => acc * f.value);
        }

        public class CapacitySummary
        {
            public PawnCapacityDef capacity;
            public List<Contributor<float>> offsets = new List<Contributor<float>>();
            public List<Contributor<float>> factors = new List<Contributor<float>>();
            public List<Contributor<float>> setMaxes = new List<Contributor<float>>();


            public float FinalOffset => offsets.Sum(o => o.value);
            public float FinalFactor => factors.Aggregate(1f, (acc, f) => acc * f.value);
            public float? FinalMax => setMaxes.Any() ? setMaxes.Min(m => m.value) : (float?)null;
        }

        public class AptitudeSummary
        {
            public SkillDef skill;
            public List<Contributor<int>> aptitudes = new List<Contributor<int>>();
            public int FinalLevel => aptitudes.Sum(a => a.value);
        }

        public class DamageSummary
        {
            public DamageDef damageDef;
            public List<Contributor<float>> factors = new List<Contributor<float>>();
            public float FinalFactor => factors.Aggregate(1f, (acc, f) => acc * f.value);
        }
    }
}
