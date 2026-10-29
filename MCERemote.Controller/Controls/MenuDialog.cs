using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MCERemote.Controller.Controls
{
    /// <summary>
    /// A context menu dialog: a header describing the subject, followed by a vertical list
    /// of options. Choosing an option closes the menu and runs the option's action.
    /// </summary>
    internal class MenuDialog : DialogBase
    {
        public MenuDialog()
        {
            title = new DialogTitle { Style = FindResource("Dialog.Menu.Title") as Style };
            details = new StackPanel();
            options = new ItemsControl { Style = FindResource("Dialog.Menu") as Style };

            var headerContent = new StackPanel();
            headerContent.Children.Add(title);
            headerContent.Children.Add(details);

            var header = new Border
            {
                Style = FindResource("Dialog.Menu.Header") as Style,
                Child = headerContent
            };

            Items.Add(header);
            Items.Add(options);
        }

        public override string BackdropKey => "Dialog.Backdrop.Menu";

        public override bool CloseOnBackdropClick => true;

        #region Fields

        private readonly DialogTitle title;
        private readonly StackPanel details;
        private readonly ItemsControl options;

        private readonly Dictionary<DialogButton, Action> actions = new Dictionary<DialogButton, Action>();

        #endregion

        /// <summary>
        /// Gets or sets the header title, e.g. the song name.
        /// </summary>
        public string Title
        {
            get => title.Text;
            set => title.Text = value;
        }

        /// <summary>
        /// Adds a line of details below the title. Empty lines are skipped.
        /// </summary>
        public void AddDetail(string text)
        {
            if (!string.IsNullOrEmpty(text))
                details.Children.Add(new DialogText { Text = text });
        }

        /// <summary>
        /// Adds an option to the menu.
        /// </summary>
        public void AddOption(string text, Action action)
        {
            var button = new DialogButton { Content = text };
            button.Click += Option_Click;

            actions[button] = action;
            options.Items.Add(button);
        }

        protected override Control GetInitialFocus()
        {
            return options.Items.Count > 0 ? options.Items[0] as Control : null;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.Back)
            {
                Close();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up || e.Key == Key.Down)
            {
                MoveFocus(e.Key == Key.Up ? -1 : 1);
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }

        private void MoveFocus(int step)
        {
            var index = options.Items.IndexOf(Keyboard.FocusedElement);
            var target = index + step;
            if (index >= 0 && target >= 0 && target < options.Items.Count)
                (options.Items[target] as Control)?.Focus();
        }

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            var button = (DialogButton)sender;
            Close(DialogButtonType.None);

            if (actions.TryGetValue(button, out Action action))
                action?.Invoke();
        }
    }
}
