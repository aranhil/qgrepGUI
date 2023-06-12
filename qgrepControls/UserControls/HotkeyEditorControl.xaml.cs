using qgrepControls.Classes;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using UserControl = System.Windows.Controls.UserControl;

namespace qgrepControls.UserControls
{
    public static class KeyExtensions
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int MapVirtualKey(int uCode, int uMapType);

        public static bool RepresentsPrintableChar(this Keys key)
        {
            return !char.IsControl((char)MapVirtualKey((int)key, 2));
        }
    }

    public partial class HotkeyEditorControl : UserControl
    {
        public event EventHandler<EventArgs> HotkeyChanged;
        private Hotkey PrevHotkey { get; set; } = new Hotkey();

        public static readonly DependencyProperty HotkeyProperty =
            DependencyProperty.Register(
                nameof(Hotkey),
                typeof(Hotkey),
                typeof(HotkeyEditorControl),
                new FrameworkPropertyMetadata(
                    default(Hotkey),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault
                )
            );

        public Hotkey Hotkey
        {
            get => (Hotkey)GetValue(HotkeyProperty);
            set
            {
                SetValue(HotkeyProperty, value);
                IsEnabled = value?.ReadOnlyKeys == null;
            }
        }

        public HotkeyEditorControl()
        {
            InitializeComponent();
            Hotkey = new Hotkey();
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Don't let the event pass further
            // because we don't want standard textbox shortcuts working
            e.Handled = true;

            // Get modifiers and key data
            var modifiers = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
            {
                key = e.SystemKey;
            }

            // Pressing delete, backspace or escape without modifiers clears the current value
            if (modifiers == ModifierKeys.None &&
                (key == Key.Delete || key == Key.Back || key == Key.Escape))
            {
                PrevHotkey = Hotkey;
                Hotkey = new Hotkey();
                return;
            }

            // If no actual key was pressed - return
            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin ||
                key == Key.Clear ||
                key == Key.OemClear ||
                key == Key.Apps)
            {
                PrevHotkey = Hotkey;
                return;
            }

            if(key == Key.Up ||
                key == Key.Down ||
                key == Key.Left ||
                key == Key.Right ||
                key == Key.Home ||
                key == Key.End ||
                key == Key.Insert ||
                key == Key.PageUp ||
                key == Key.PageDown ||
                key == Key.Enter)
            {
                if ((modifiers & ModifierKeys.Control) != ModifierKeys.Control &&
                    (modifiers & ModifierKeys.Shift) != ModifierKeys.Shift &&
                    (modifiers & ModifierKeys.Alt) != ModifierKeys.Alt)
                {
                    PrevHotkey = Hotkey;
                    return;
                }
            }

            Keys keys = new Keys();

            //if ((modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            //    keys |= Keys.Control;

            //if ((modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            //    keys |= Keys.Shift;

            //if ((modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            //    keys |= Keys.Alt;

            keys |= (Keys)KeyInterop.VirtualKeyFromKey(key);

            if (KeyExtensions.RepresentsPrintableChar(keys))
            {
                if((modifiers & ModifierKeys.Control) != ModifierKeys.Control &&
                    (modifiers & ModifierKeys.Alt) != ModifierKeys.Alt)
                {
                    PrevHotkey = Hotkey;
                    return;
                }
            }

            // Update the value
            PrevHotkey = Hotkey;
            Hotkey = new Hotkey(key, modifiers);
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
