using System;
using System.Windows;
using System.Windows.Input;

namespace MCERemote.Controller.Controls
{
    public class ButtonRadio : System.Windows.Controls.RadioButton
    {
        public ButtonRadio()
        {
            Style = FindResource("Page.Button.Radio") as System.Windows.Style;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                IsPressed = true;
                IsChecked = true;
                RaiseEvent(new RoutedEventArgs(ClickEvent));
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if ((e.Key == Key.Enter || e.Key == Key.Space) && IsPressed)
            {
                IsPressed = false;
                e.Handled = true;
            }

            base.OnKeyUp(e);
        }
    }
}
