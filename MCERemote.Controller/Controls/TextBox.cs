using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MCERemote.Controller.Controls
{
    public class TextInput : TextBox
    {
        public TextInput()
        {
            Style = FindResource("Page.TextInput") as Style;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Up)
            {
                e.Handled = true;
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
            }

            if (e.Key == Key.Down)
            {
                e.Handled = true;
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
            }

            if (e.Key == Key.Left && CaretIndex == 0)
            {
                e.Handled = true;
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Left));
            }

            if (e.Key == Key.Right && CaretIndex == Text.Length)
            {
                e.Handled = true;
                MoveFocus(new TraversalRequest(FocusNavigationDirection.Right));
            }

            base.OnPreviewKeyDown(e);
        }
    }
}
