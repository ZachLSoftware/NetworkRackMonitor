using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RackMonitor.UserControls.IPControls;

namespace RackMonitor.UserControls
{
    /// <summary>
    /// Interaction logic for IPContol.xaml
    /// </summary>
    public partial class IPControl : UserControl
    {
        public event EventHandler TextChanged;

        private List<FieldControl> _fields = new List<FieldControl>();
        private ContextMenu _contextmenu = new ContextMenu();
        private MenuItem _miCopy = new MenuItem();
        private MenuItem _miPaste = new MenuItem();
        private bool _enable = true;

        #region Dependency Properties

        /// <summary>
        /// Identifies the Text dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                "Text",
                typeof(string),
                typeof(IPControl),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        /// <summary>
        /// Gets or sets the IP address text. This is a dependency property.
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        /// <summary>
        /// Handles changes to the Text property.
        /// </summary>
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IPControl control = d as IPControl;
            if (control != null)
            {
                // To prevent recursive updates, only set the IP if the control's current value is different.
                if (control.GetIPAddress() != (string)e.NewValue)
                {
                    control.SetIPAddress((string)e.NewValue, false);
                }
            }
        }

        #endregion

        public new bool IsEnabled
        {
            get { return _enable; }
            set
            {
                _enable = value;
                EnableCtrls(_enable);
            }
        }

        public bool IPValid
        {
            get { return IsValidIP4(Text); }
        }

        public IPControl()
        {
            InitializeComponent();

            //Setup Context Menu
            _miCopy.Header = "Copy";
            _miPaste.Header = "Paste";
            _miCopy.Click += Field_CopyToClipboard;
            _miPaste.Click += Field_CopyFromClipboard;
            _contextmenu.ContextMenuOpening += ContextMenuOpening;
            _contextmenu.Items.Add(_miCopy);
            _contextmenu.Items.Add(_miPaste);
            ContextMenu = _contextmenu;

            _fields.Add(new FieldControl(Octet1, 0));
            _fields.Add(new FieldControl(Octet2, 1));
            _fields.Add(new FieldControl(Octet3, 2));
            _fields.Add(new FieldControl(Octet4, 3));

            foreach (FieldControl field in _fields)
            {
                field.FocusChanged += Field_FocusChanged;
                field.CopyToClipboard += Field_CopyToClipboard;
                field.CopyFromClipboard += Field_CopyFromClipboard;
                field.TextChanged += Field_TextChanged;
                field.ContextMenu = _contextmenu;
            }
        }

        private void Field_TextChanged(object sender, EventArgs e)
        {
            // Update the dependency property to notify any bindings that the value has changed.
            Text = GetIPAddress();

            // Raise the conventional TextChanged event for any consumers who are not using data binding.
            TextChanged?.Invoke(this, new EventArgs());
        }

        private void EnableCtrls(bool enable)
        {
            foreach (FieldControl field in _fields)
            {
                field.IsEnabled = enable;
            }
        }

        private new void ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                if (IsValidIP4(Clipboard.GetText()))
                    _miPaste.IsEnabled = true;
                else
                    _miPaste.IsEnabled = false;
            }
        }

        private void Field_CopyFromClipboard(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                SetIPAddress(Clipboard.GetText(), true);
            }
        }

        private void Field_CopyToClipboard(object sender, EventArgs e)
        {
            Clipboard.SetText(GetIPAddress());
        }

        private void Field_FocusChanged(FocusEventArgs e)
        {
            if (e.Direction == Direction.Reverse)
            {
                if (e.FieldIndex > 0)
                {
                    int idx = e.FieldIndex - 1;
                    _fields[idx].TakeFocus(e.Action, e.Selection);
                }
            }

            if (e.Direction == Direction.Forward)
            {
                if (e.FieldIndex < 3)
                {
                    int idx = e.FieldIndex + 1;
                    _fields[idx].TakeFocus(e.Action, e.Selection);
                }
            }

            if (e.Direction == Direction.None)
            {
                if (e.Action == IPControls.Action.Home)
                {
                    _fields[0].TakeFocus(e.Action, e.Selection);
                }

                if (e.Action == IPControls.Action.End)
                {
                    _fields[3].TakeFocus(e.Action, e.Selection);
                }
            }
        }

        private void SetIPAddress(string text, bool end)
        {
            if (IsValidIP4(text))
            {
                string[] octets = text.Split('.');
                _fields[0].Text = octets[0];
                _fields[1].Text = octets[1];
                _fields[2].Text = octets[2];
                _fields[3].Text = octets[3];

                if (end)
                {
                    _fields[3].TakeFocus(IPControls.Action.End, Selection.None);
                }
            }
            else
            {
                // If the text is not a valid IP, clear the fields.
                _fields[0].Text = string.Empty;
                _fields[1].Text = string.Empty;
                _fields[2].Text = string.Empty;
                _fields[3].Text = string.Empty;
            }
        }

        private string GetIPAddress()
        {
            StringBuilder ip = new StringBuilder();
            ip.Append(_fields[0].Text);
            ip.Append(".");
            ip.Append(_fields[1].Text);
            ip.Append(".");
            ip.Append(_fields[2].Text);
            ip.Append(".");
            ip.Append(_fields[3].Text);

            return ip.ToString();
        }

        public bool IsValidIP4()
        {
            return IsValidIP4(Text);
        }

        public bool IsValidIP4(String strIPAddress)
        {
            if (string.IsNullOrEmpty(strIPAddress))
            {
                return false;
            }
            Regex objIP4Address = new Regex(@"^(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])$");
            return objIP4Address.IsMatch(strIPAddress);
        }
    }
}

