using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Orla
{
    // A global keyboard shortcut, stored as invariant text such as "Control+Alt+Space".
    public struct Shortcut
    {
        public const string Default = "Control+Alt+Space";

        public ModifierKeys Modifiers;
        public Key Key;

        public bool IsValid => Key != Key.None && KeyInterop.VirtualKeyFromKey(Key) != 0 &&
                               (Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != 0;

        public static Shortcut parse(string text)
        {
            var result = new Shortcut();
            foreach (string part in (text ?? Default).Split('+'))
            {
                if (Enum.TryParse(part, out ModifierKeys modifier) && modifier != ModifierKeys.None)
                    result.Modifiers |= modifier;
                else if (Enum.TryParse(part, out Key key))
                    result.Key = key;
            }
            return result.IsValid ? result : parse(Default);
        }

        public override string ToString()
        {
            var parts = new List<string>();
            foreach (ModifierKeys m in new[] { ModifierKeys.Control, ModifierKeys.Alt, ModifierKeys.Shift, ModifierKeys.Windows })
                if ((Modifiers & m) != 0)
                    parts.Add(m.ToString());
            parts.Add(Key.ToString());
            return String.Join("+", parts);
        }

        // As people read it, in the interface language: "Ctrl+Alt+Espaço", "Strg+Alt+Leertaste".
        public string display()
        {
            var parts = new List<string>();
            if ((Modifiers & ModifierKeys.Control) != 0)
                parts.Add(Text.get("key.ctrl"));
            if ((Modifiers & ModifierKeys.Alt) != 0)
                parts.Add("Alt");
            if ((Modifiers & ModifierKeys.Shift) != 0)
                parts.Add(Text.get("key.shift"));
            if ((Modifiers & ModifierKeys.Windows) != 0)
                parts.Add("Win");
            parts.Add(Key == Key.Space ? Text.get("key.space") : keyName(Key));
            return String.Join("+", parts);
        }

        static string keyName(Key key)
        {
            if (key >= Key.D0 && key <= Key.D9)
                return ((char)('0' + (key - Key.D0))).ToString();
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
                return "Num " + (key - Key.NumPad0);
            return key.ToString();
        }

        public uint nativeModifiers =>
            ((Modifiers & ModifierKeys.Alt) != 0 ? Native.MOD_ALT : 0) | ((Modifiers & ModifierKeys.Control) != 0 ? Native.MOD_CONTROL : 0) |
            ((Modifiers & ModifierKeys.Shift) != 0 ? Native.MOD_SHIFT : 0) | ((Modifiers & ModifierKeys.Windows) != 0 ? Native.MOD_WIN : 0);

        public uint virtualKey => (uint)KeyInterop.VirtualKeyFromKey(Key);

        static readonly Key[] modifierKeys = { Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt, Key.LeftShift, Key.RightShift,
                                               Key.LWin, Key.RWin, Key.System };

        // Turns a key press in the shortcut recorder into a shortcut, or null while only modifiers are held.
        public static Shortcut? fromKeyPress(KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (modifierKeys.Contains(key))
                return null;
            var s = new Shortcut { Modifiers = Keyboard.Modifiers, Key = key };
            if ((Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)))
                s.Modifiers |= ModifierKeys.Windows;
            return s;
        }
    }
}
