using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Markup;

namespace Orla
{
    // Interface text lives in Strings/<language>.json, embedded in the executable. English is the fallback for
    // any key a translation has not covered yet.
    public static class Text
    {
        public static readonly string[] Languages = { "pt-BR", "en", "es", "fr", "de", "it" };

        // Each language named in itself, as people look for their own language in a list.
        public static string nativeName(string language)
        {
            switch (language)
            {
                case "pt-BR": return "Português (Brasil)";
                case "es": return "Español";
                case "fr": return "Français";
                case "de": return "Deutsch";
                case "it": return "Italiano";
                default: return "English";
            }
        }
        static Dictionary<string, string> strings, fallback;

        public static string Language { get; private set; }

        public static void load(string setting)
        {
            Language = resolve(setting);
            fallback = read("en");
            strings = Language == "en" ? fallback : read(Language);
        }

        public static string resolve(string setting)
        {
            if (Array.IndexOf(Languages, setting) >= 0)
                return setting;
            // Follows the Windows display language; any Portuguese uses the Brazilian text, anything else unknown English.
            string system = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            foreach (string language in Languages)
                if (language.StartsWith(system, StringComparison.OrdinalIgnoreCase))
                    return language;
            return "en";
        }

        public static Dictionary<string, string> read(string language)
        {
            using (Stream s = typeof(Text).Assembly.GetManifestResourceStream("Orla.Strings." + language + ".json"))
            using (var reader = new StreamReader(s))
                return Store.json().Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
        }

        public static string get(string key)
        {
            if (strings == null)
                load("system");
            if (strings.TryGetValue(key, out string value) || fallback.TryGetValue(key, out value))
                return value;
            return key;
        }

        public static string format(string key, params object[] args)
        {
            return String.Format(get(key), args);
        }
    }

    // XAML markup extension that looks up interface text by key, written as l:T in XAML.
    [MarkupExtensionReturnType(typeof(string))]
    public class T : MarkupExtension
    {
        public T(string key)
        {
            Key = key;
        }

        public string Key { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return Text.get(Key);
        }
    }
}
