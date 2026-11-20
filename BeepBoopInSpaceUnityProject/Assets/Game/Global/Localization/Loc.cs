using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Global.Localization
{
    public enum ELanguage
    {
        French = 0,
        English = 1,
    }

    /// <summary>
    /// The game's languages. Texts are written in French in the UXML, the assets and the code, and that text is the key of
    /// the translation (<see cref="Translations"/>): a text missing from the table stays in French. Machines of an online
    /// room send each other the French text, each shows it in its own language.
    /// </summary>
    public static class Loc
    {
        private static readonly ConditionalWeakTable<TextElement, string> s_sourceTexts = new();

        // The translations of the texts with a value: "Il reste {0} places" also translates "Il reste 2 places".
        private static readonly List<(Regex pattern, string translation)> s_patterns = Translations.English
            .Where(pair => pair.Key.Contains("{0}"))
            .Select(pair => (new Regex("^" + Regex.Escape(pair.Key).Replace(@"\{0}", "(.*)") + "$", RegexOptions.Singleline), pair.Value))
            .ToList();

        public static ELanguage Language { get; private set; } = ELanguage.French;

        /// <summary>The language of a player who hasn't chosen: French for French systems, English for the others.</summary>
        public static ELanguage SystemLanguage =>
            Application.systemLanguage == UnityEngine.SystemLanguage.French ? ELanguage.French : ELanguage.English;

        public static void SetLanguage(ELanguage language)
        {
            Language = language;
        }

        /// <summary>The text in the current language.</summary>
        public static string T(string french)
        {
            if (Language == ELanguage.French || string.IsNullOrEmpty(french))
                return french;
            if (Translations.English.TryGetValue(french, out var english))
                return english;
            foreach (var (pattern, translation) in s_patterns)
            {
                var match = pattern.Match(french);
                if (match.Success)
                    return translation.Replace("{0}", T(match.Groups[1].Value));
            }
            return french;
        }

        /// <summary>A player's badge: J1 in French, P1 in English.</summary>
        public static string PlayerBadge(int number)
        {
            return Language == ELanguage.French ? $"J{number}" : $"P{number}";
        }

        public static string BotBadge => Language == ELanguage.French ? "IA" : "CPU";

        /// <summary>Translates the texts under the element, from the texts they first had: it can be called again after a language change.</summary>
        public static void Translate(VisualElement root)
        {
            root.Query<TextElement>().ForEach(element =>
            {
                if (!s_sourceTexts.TryGetValue(element, out var source))
                {
                    source = element.text;
                    s_sourceTexts.Add(element, source);
                }
                // No text of its own: the code fills it (a volume's value), and it stays as it is.
                if (!string.IsNullOrEmpty(source))
                    element.text = T(source);
            });
        }
    }
}
