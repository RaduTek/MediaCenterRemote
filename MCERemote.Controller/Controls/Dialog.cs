using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

    internal class Dialog : ItemsControl
    {
        public Dialog()
        {
            Style = FindResource("Dialog.Body") as Style;

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

        public DialogHost Host { get; set; }

        public bool IsOpen { get; private set; }

        public event EventHandler<DialogClosedEventArgs> Closed;
        public event EventHandler Opened;

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

        public void Open()
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

            Visibility = Visibility.Visible;
            IsOpen = true;

            if (buttons.Items.Count > 0)
            {
                var control = buttons.Items[0] as Control;
                control?.Focus();
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
    }

    internal class DialogHostEventArgs : EventArgs
    {
        public Dialog Dialog { get; set; }
        public DialogHostEventArgs(Dialog dialog)
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

        public void ShowDialog(Dialog dialog)
        {
            dialog.Host = this;
            Content = dialog;
            dialog.Open();
            Visibility = Visibility.Visible;

            Opened?.Invoke(this, new DialogHostEventArgs(dialog));
        }

        public void CloseDialog()
        {
            Dialog dialog = Content as Dialog;
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
