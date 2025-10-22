using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace RackMonitor.UserControls.IPControls
{
	public enum Direction
	{
		None, Forward, Reverse
	}

	public enum Selection
	{
		None, All
	}

	public enum Action
	{
		None, Trim, Home, End
	}

	public class FocusEventArgs : EventArgs
	{
		public int FieldIndex { get; set; }

		public Action Action { get; set; } = Action.None;

		public Direction Direction { get; set; }

		public Selection Selection { get; set; } = Selection.None;
	}

	public class FieldControl
	{
		public delegate void FieldEventHander(FocusEventArgs e);
		public event FieldEventHander FocusChanged;
		public event EventHandler CopyToClipboard;
		public event EventHandler CopyFromClipboard;
		public event EventHandler TextChanged;

		private TextBox Field = new TextBox();
		private int Index = 0;		
		
		public ContextMenu ContextMenu
		{
			set { Field.ContextMenu = value; }
		}

		public string Text
		{
			get { return Field.Text; }
			set { Field.Text = value; }
		}
		public bool IsEnabled
        {
			get { return Field.IsEnabled;  }
			set { Field.IsEnabled = value; }
        }

        public FieldControl(TextBox field, int index)
		{
			Field = field;
			Index = index;

			Field.TextChanged += Field_TextChanged;
			Field.PreviewKeyDown += Field_PreviewKeyDown;
		}

        public void TakeFocus()
		{
			Field.Focus();
			Field.CaretIndex = Field.Text.Length;
		}

		public void TakeFocus(Action action, Selection selection)
		{
			Field.Focus();

			if (selection == Selection.All)
			{
				Field.Select(0, Field.Text.Length);
			}

			if (action == Action.Home)
			{
				Field.CaretIndex = 0;
			}

			if (action == Action.End)
			{
				Field.CaretIndex = Field.Text.Length;
			}
		}

		private bool IsNumericKey(Key key)
		{
			// Check if the key is a numeric printable character
			return (key >= Key.D0 && key <= Key.D9) || (key >= Key.NumPad0 && key <= Key.NumPad9);
		}

		private char GetNumber(Key key)
		{
			switch (key)
			{
				case Key.D1: return '1';
				case Key.D2: return '2';
				case Key.D3: return '3';
				case Key.D4: return '4';
				case Key.D5: return '5';
				case Key.D6: return '6';
				case Key.D7: return '7';
				case Key.D8: return '8';
				case Key.D9: return '9';
				case Key.D0: return '0';
				case Key.NumPad1: return '1';
				case Key.NumPad2: return '2';
				case Key.NumPad3: return '3';
				case Key.NumPad4: return '4';
				case Key.NumPad5: return '5';
				case Key.NumPad6: return '6';
				case Key.NumPad7: return '7';
				case Key.NumPad8: return '8';
				case Key.NumPad9: return '9';
				case Key.NumPad0: return '0';
			}

			return ' ';
		}

		private void Field_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (sender is TextBox)
			{
				TextBox tb = (TextBox)sender;
				if (tb.IsReadOnly) return;
			}
			bool handled = true;

			//Is Numberic
			//int key = (int)e.Key;
			//if (key >= 34 && key <= 43 || key >= 74 && key <= 83)

			if (IsNumericKey(e.Key))
			{
				if (Field.Text.Length == 3)
				{
					FocusEventArgs args = new FocusEventArgs();
					args.Direction = Direction.Forward;
					args.Selection = Selection.All;
					args.FieldIndex = Index;
					FocusChanged?.Invoke(args);
				}

				var pos = Field.SelectionStart;
				Field.Text = Field.Text.Insert(Field.CaretIndex, GetNumber(e.Key).ToString());
				Field.CaretIndex = Field.CaretIndex + 1;
				Field.SelectionStart = pos + 1;
				
				handled = true;
			}

			if (e.Key == Key.Back || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Delete) handled = false;

			//Back Key
			if (e.Key == Key.Back && Field.CaretIndex == 0 && Field.SelectedText.Length == 0)
			{
				FocusEventArgs args = new FocusEventArgs();
				args.Direction = Direction.Reverse;
				args.Action = Action.End;
				args.FieldIndex = Index;
				FocusChanged?.Invoke(args);
			}

			//Period (dot)
			if ((e.Key == Key.OemPeriod || e.Key == Key.Decimal) && Field.Text.Length > 0)
			{
				FocusEventArgs args = new FocusEventArgs();
				args.Direction = Direction.Forward;
				args.Selection = Selection.All;
				args.FieldIndex = Index;
				FocusChanged?.Invoke(args);
			}

			//Left Key
			if (e.Key == Key.Left && Field.CaretIndex == 0)
			{
				FocusEventArgs args = new FocusEventArgs();
				args.Direction = Direction.Reverse;
				args.Action = Action.End;
				args.FieldIndex = Index;
				FocusChanged?.Invoke(args);
				handled = true;
			}

			//Right Key
			if (e.Key == Key.Right && Field.CaretIndex == Field.Text.Length)
			{
				FocusEventArgs args = new FocusEventArgs();
				args.Direction = Direction.Forward;
				args.Action = Action.Home;
				args.FieldIndex = Index;
				FocusChanged?.Invoke(args);
				handled = true;
			}

			//Home Key
			if (e.Key == Key.Home)
			{
				FocusEventArgs args = new FocusEventArgs();
				args.Direction = Direction.None;
				args.Action = Action.Home;
				args.FieldIndex = Index;
				FocusChanged?.Invoke(args);
				handled = true;
			}

			//End Key
			if (e.Key == Key.End)
			{
				FocusEventArgs args = new FocusEventArgs();
				args.Direction = Direction.None;
				args.Action = Action.End;
				args.FieldIndex = Index;
				FocusChanged?.Invoke(args);
				handled = true;
			}

			//Copy to Clipboard
			if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
			{
				CopyToClipboard?.Invoke(this, new EventArgs());
			}

			//Copy From Clipboard
			if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V)
			{
				CopyFromClipboard?.Invoke(this, new EventArgs());
			}

			e.Handled = handled;
		}

		private void Field_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (Field.Text != string.Empty)
			{
				if (Field.Text.Length == 3)
				{
                    FocusEventArgs args = new FocusEventArgs();
                    args.Direction = Direction.Forward;
                    args.Action = Action.Home;
                    args.FieldIndex = Index;
                    FocusChanged?.Invoke(args);
                }
				int value = 0;
				int.TryParse(Field.Text,out value);
				if (value > 255)
				{
					Field.Text = "255";
					Field.CaretIndex = Field.Text.Length;
				}	
			}

			TextChanged?.Invoke(this, new EventArgs());
		}
	}
}
