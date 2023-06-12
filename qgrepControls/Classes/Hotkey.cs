using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Input;

namespace qgrepControls.Classes
{
    public class Hotkey
    {
        public Key Key { get; set; } = Key.None;
        public bool IsGlobal { get; set; } = false;
        public string Scope { get; set; } = "";
        public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;
        public string ReadOnlyKeys { get; private set; }

        private static KeysConverter KeysConverter = new KeysConverter();

        public Hotkey()
        {
        }

        public Hotkey(Key key, ModifierKeys modifiers, bool isGlobal = false, string scope = "")
        {
            Key = key;
            Modifiers = modifiers;
            IsGlobal = isGlobal;
            Scope = scope;
        }

        public Hotkey(string readOnlyKeys)
        {
            ReadOnlyKeys = readOnlyKeys;
        }

        private static List<Tuple<string, string>> conversions = new List<Tuple<string, string>>();
        private static List<Tuple<string, string>> Conversions
        {
            get
            {
                if(conversions.Count == 0)
                {
                    conversions.Add(new Tuple<string, string>("Bkspce", "Back"));
                    conversions.Add(new Tuple<string, string>("Break", "Pause"));
                    conversions.Add(new Tuple<string, string>("\\\\", "OemBackslash"));
                    conversions.Add(new Tuple<string, string>("Num \\*", "Multiply"));
                    conversions.Add(new Tuple<string, string>("Esc", "Escape"));
                    conversions.Add(new Tuple<string, string>("\\/", "OemQuestion"));
                    conversions.Add(new Tuple<string, string>("\\-", "OemMinus"));
                    conversions.Add(new Tuple<string, string>("\\.", "OemPeriod"));
                    conversions.Add(new Tuple<string, string>("Left Arrow", "Left"));
                    conversions.Add(new Tuple<string, string>("Right Arrow", "Right"));
                    conversions.Add(new Tuple<string, string>("\\bDown Arrow\\b", "\\bDown\\b"));
                    conversions.Add(new Tuple<string, string>("\\bUp Arrow\\b", "\\bUp\\b"));
                    conversions.Add(new Tuple<string, string>("`", "Oemtilde"));
                    conversions.Add(new Tuple<string, string>(";", "OemSemicolon"));
                    conversions.Add(new Tuple<string, string>(",", "Oemcomma"));
                    conversions.Add(new Tuple<string, string>("'", "OemQuotes"));
                    conversions.Add(new Tuple<string, string>("\\[", "OemOpenBrackets"));
                    conversions.Add(new Tuple<string, string>("\\]", "Oem6"));
                    conversions.Add(new Tuple<string, string>("=", "Oemplus"));
                    conversions.Add(new Tuple<string, string>("Num \\.", "Decimal"));
                    conversions.Add(new Tuple<string, string>("Num \\/", "Divide"));
                    conversions.Add(new Tuple<string, string>("Num \\-", "Subtract"));
                    conversions.Add(new Tuple<string, string>("Num \\+", "Add"));
                    conversions.Add(new Tuple<string, string>("\\bNum ", "\\bNumPad\\b"));
                }

                return conversions;
            }
        }

        public Keys FormsKey
        {
            get
            {
                Keys keys = (Keys)KeyInterop.VirtualKeyFromKey(Key);

                if ((Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                    keys |= Keys.Control;

                if ((Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                    keys |= Keys.Shift;

                if ((Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                    keys |= Keys.Alt;

                return keys;
            }
        }

        public override string ToString()
        {
            if(ReadOnlyKeys != null)
            {
                return ReadOnlyKeys;
            }

            string keyString = "";

            if ((Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                keyString += KeysConverter.ConvertToString(null, CultureInfo.CurrentUICulture, Keys.Control).Replace("+None", "") + "+";

            if ((Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                keyString += KeysConverter.ConvertToString(null, CultureInfo.CurrentUICulture, Keys.Shift).Replace("+None", "") + "+";

            if ((Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
                keyString += KeysConverter.ConvertToString(null, CultureInfo.CurrentUICulture, Keys.Alt).Replace("+None", "") + "+";

            keyString += KeysConverter.ConvertToString(null, CultureInfo.CurrentUICulture, (Keys)KeyInterop.VirtualKeyFromKey(Key));

            foreach (Tuple<string, string> conversion in Conversions)
            {
                keyString = Regex.Replace(keyString, conversion.Item2, conversion.Item1);
            }

            keyString = keyString.Replace("\\b", "");
            keyString = Regex.Replace(keyString, "\\\\(.){1}", "$1");
            keyString = keyString.Replace("None", Properties.Resources.NotSet);

            return keyString;
        }

        public string ToUnlocalizedString()
        {
            KeyConverter keyConverter = new KeyConverter();
            ModifierKeysConverter modifierKeysConverter = new ModifierKeysConverter();

            string keyString = keyConverter.ConvertToString(Key);
            string modifierKeysString = modifierKeysConverter.ConvertToString(Modifiers);

            return modifierKeysString + (modifierKeysString.Length > 0 ? "+" : "") + keyString;
        }

        public static Hotkey FromString(string keysString)
        {
            foreach(Tuple<string, string> conversion in Conversions)
            {
                keysString = Regex.Replace(keysString, conversion.Item1, conversion.Item2);
            }

            keysString = keysString.Replace("\\b", "");
            keysString = Regex.Replace(keysString, "\\\\(.){1}", "$1");

            Keys keys = (Keys)KeysConverter.ConvertFromString(null, CultureInfo.CurrentUICulture, keysString);

            ModifierKeys modifierKeys = new ModifierKeys();

            if ((keys & Keys.Control) == Keys.Control)
            {
                modifierKeys |= ModifierKeys.Control;
                keys &= ~Keys.Control;
            }
            if ((keys & Keys.Shift) == Keys.Shift)
            {
                modifierKeys |= ModifierKeys.Shift;
                keys &= ~Keys.Shift;
            }
            if ((keys & Keys.Alt) == Keys.Alt)
            {
                modifierKeys |= ModifierKeys.Alt;
                keys &= ~Keys.Alt;
            }

            return new Hotkey(KeyInterop.KeyFromVirtualKey((int)keys), modifierKeys);
        }
    }
}
