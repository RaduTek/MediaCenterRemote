using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MCERemote.Controller.Controls
{
    internal enum DialogButtonType
    {
        None,
        Button1,
        Button2
    }

    internal class DialogClosedEventArgs : EventArgs
    {
        public DialogButtonType Button { get; set; }

        public DialogClosedEventArgs(DialogButtonType button)
        {
            Button = button;
        }
    }

    /// <summary>
    /// Base of dialogs shown by a <see cref="DialogHost"/>.
    /// </summary>
    internal abstract class DialogBase : ItemsControl
    {
        protected DialogBase()
        {
            Style = FindResource("Dialog.Body") as Style;
        }

        public DialogHost Host { get; set; }

        public bool IsOpen { get; private set; }

        public event EventHandler<DialogClosedEventArgs> Closed;
        public event EventHandler Opened;

        /// <summary>
        /// Gets the resource key of the backdrop brush drawn behind the dialog.
        /// </summary>
        public virtual string BackdropKey => "Dialog.Backdrop";

        /// <summary>
        /// Gets whether clicking the backdrop outside the dialog closes it.
        /// </summary>
        public virtual bool CloseOnBackdropClick => false;

        public void Open()
        {
            OnOpening();

            Visibility = Visibility.Visible;
            IsOpen = true;

            var control = GetInitialFocus();
            if (control != null)
            {
                control.Focus();
                Keyboard.Focus(control);
            }

            Opened?.Invoke(this, EventArgs.Empty);
        }

        public void Close(DialogButtonType button = DialogButtonType.None)
        {
            Visibility = Visibility.Collapsed;
            IsOpen = false;
            Host.CloseDialog();
            Closed?.Invoke(this, new DialogClosedEventArgs(button));
        }

        /// <summary>
        /// Called before the dialog is shown, to prepare its contents.
        /// </summary>
        protected virtual void OnOpening()
        {
        }

        /// <summary>
        /// Gets the control focused when the dialog opens, if any.
        /// </summary>
        protected virtual Control GetInitialFocus()
        {
            return null;
        }
    }

    /// <summary>
    /// A message dialog with a title, text and up to two buttons.
    /// </summary>
    internal class Dialog : DialogBase
    {
        public Dialog()
        {
            InitializeComponents();
        }

        private DialogTitle title;
        private DialogText text;
        private DialogButtons buttons;
        private DialogButton button1, button2;

        private void InitializeComponents()
        {
            title = new DialogTitle();
            text = new DialogText();
            buttons = new DialogButtons();

            Items.Add(title);
            Items.Add(text);
            Items.Add(buttons);

            button1 = new DialogButton();
            button1.Click += DialogButton_Click;

            button2 = new DialogButton();
            button2.Click += DialogButton_Click;
        }

        private void DialogButton_Click(object sender, RoutedEventArgs e)
        {
            DialogButtonType button = DialogButtonType.None;
            if (sender == button1)
                button = DialogButtonType.Button1;
            else if (sender == button2)
                button = DialogButtonType.Button2;

            Close(button);
        }

        public string Title
        {
            get => title.Text;
            set => title.Text = value;
        }
        
        public string Text
        {
            get => text.Text;
            set => text.Text = value;
        }

        private string _Button1Text = "";
        public string Button1Text
        {
            get => _Button1Text;
            set => button1.Content = _Button1Text = value;
        }

        private string _Button2Text = "";
        public string Button2Text
        {
            get => _Button2Text;
            set => button2.Content = _Button2Text = value;
        }

        protected override void OnOpening()
        {
            buttons.Items.Clear();

            if (Button1Text?.Length > 0)
                buttons.Items.Add(button1);
            if (Button2Text?.Length > 0)
                buttons.Items.Add(button2);

            if (buttons.Items.Count == 0)
                buttons.Visibility = Visibility.Collapsed;
            else if (buttons.Items.Count == 1)
                buttons.Items.Insert(0, new Grid());
            else
                buttons.Visibility = Visibility.Visible;
        }

        protected override Control GetInitialFocus()
        {
            // With one button, a spacer is inserted before it
            foreach (var item in buttons.Items)
            {
                if (item is Control control)
                    return control;
            }
            return null;
        }
    }

    internal class DialogHostEventArgs : EventArgs
    {
        public DialogBase Dialog { get; set; }
        public DialogHostEventArgs(DialogBase dialog)
        {
            Dialog = dialog;
        }
    }

    internal class DialogHost : ContentControl
    {
        public DialogHost()
        {
            Style = FindResource("Dialog.Host") as Style;
        }

        public event EventHandler<DialogHostEventArgs> Closed;
        public event EventHandler<DialogHostEventArgs> Opened;

        public void ShowDialog(DialogBase dialog)
        {
            dialog.Host = this;
            Content = dialog;
            Background = FindResource(dialog.BackdropKey) as Brush;

            // The host must be visible and laid out before the dialog opens, or focusing its
            // first control fails and the dialog doesn't receive key presses
            Visibility = Visibility.Visible;
            UpdateLayout();

            dialog.Open();

            Opened?.Invoke(this, new DialogHostEventArgs(dialog));
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            var dialog = Content as DialogBase;
            var source = e.OriginalSource as Visual;
            if (dialog == null || !dialog.CloseOnBackdropClick || source == null)
                return;

            // Clicked the backdrop rather than the dialog itself
            if (source != dialog && !dialog.IsAncestorOf(source))
            {
                dialog.Close();
                e.Handled = true;
            }
        }

        public void CloseDialog()
        {
            var dialog = Content as DialogBase;
            if (dialog != null)
            {
                if (dialog.IsOpen)
                    dialog.Close();

            }

            Content = null;
            Visibility = Visibility.Collapsed;

            Closed?.Invoke(this, new DialogHostEventArgs(dialog));
        }
    }

    internal class DialogButtons : ItemsControl
    {
        public DialogButtons()
        {
            Style = FindResource("Dialog.Buttons") as Style;
        }
    }

    internal class DialogButton : Button
    {
        public DialogButton() : base()
        {
            Margin = new Thickness(4);
            TextBlock.SetTextAlignment(this, TextAlignment.Center);
        }
    }

    internal class DialogTitle : TextBlock
    {
        public DialogTitle()
        {
            Style = FindResource("Dialog.Title") as Style;
        }
    }

    internal class DialogText : TextBlock
    {
        public DialogText()
        {
            Style = FindResource("Dialog.Text") as Style;
        }
    }
}
