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
        public static readonly string[] Languages = { "pt-BR", "en" };
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
            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt" ? "pt-BR" : "en";
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
