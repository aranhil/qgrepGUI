using System.Text;
using System.Windows.Input;

namespace qgrepControls.Classes
{
    public class Hotkey
    {
        public Key Key { get; }

        public ModifierKeys Modifiers { get; }

        public Hotkey(Key key, ModifierKeys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public override string ToString()
        {
            var str = new StringBuilder();

            if (Modifiers.HasFlag(ModifierKeys.Control))
                str.Append($"{Properties.Resources.Ctrl}+");
            if (Modifiers.HasFlag(ModifierKeys.Shift))
                str.Append($"{Properties.Resources.Shift}+");
            if (Modifiers.HasFlag(ModifierKeys.Alt))
                str.Append($"{Properties.Resources.Alt}+");

            if (Key.ToString().StartsWith("D") && Key.ToString().Length == 2 && char.IsDigit(Key.ToString()[1]))
            {
                str.Append(Key.ToString()[1]);
            }
            else
            {
                str.Append(Key);
            }

            return str.ToString();
        }
    }
}
