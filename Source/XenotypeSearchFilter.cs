using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;

namespace XenotypeSummary
{
    public class XenotypeSearchFilter
    {
        private string filterText = "";
        public string FilterText
        {
            get => filterText;
            set
            {
                if (filterText != value)
                {
                    filterText = value;
                    isDirty = true;
                }
            }
        }

        public bool isDirty = true;

        private readonly string highlightColor = "#ffff00";

        public bool Active => !filterText.NullOrEmpty();

        public void DrawSearchBar(Rect rect)
        {
            // RMB to clear
            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && rect.Contains(Event.current.mousePosition))
            {
                FilterText = "";
                Event.current.Use();
            }

            Text.Font = GameFont.Tiny;
            FilterText = Widgets.TextField(rect, FilterText);
            Text.Font = GameFont.Small;

            if (Active && Widgets.ButtonImage(new Rect(rect.xMax + 5f, rect.y + 4f, 14f, 14f), TexButton.CloseXSmall))
            {
                FilterText = "";
            }
        }

        public bool Matches(string text)
        {
            if (!Active) return true;
            if (text == null) return false;
            return text.IndexOf(FilterText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public string Highlight(string text)
        {
            if (!Active || text.NullOrEmpty()) return text;

            try
            {
                string pattern = Regex.Escape(FilterText);
                return Regex.Replace(text, pattern, m => $"<color={highlightColor}>{m.Value}</color>", RegexOptions.IgnoreCase);
            }
            catch
            {
                return text;
            }
        }

        // Filtering logic
        public bool GeneMatches(GeneDef gene)
        {
            if (!Active) return true;
            if (gene == null) return false;
            if (Matches(gene.label)) return true;
            if (Matches(gene.description)) return true;
            if (gene.customEffectDescriptions != null && gene.customEffectDescriptions.Any(e => Matches(e))) return true;
            return false;
        }

        public bool AbilityMatches(AbilityDef ability)
        {
            if (!Active) return true;
            if (ability == null) return false;
            return Matches(ability.label) || Matches(ability.description);
        }

        public bool StatMatches(StatDef stat, string valueStr)
        {
            if (!Active) return true;
            if (stat == null) return Matches(valueStr);
            return Matches(stat.label) || Matches(valueStr);
        }
    }
}
