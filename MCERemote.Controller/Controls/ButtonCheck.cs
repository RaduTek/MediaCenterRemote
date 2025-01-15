using System;
using System.Windows;
using System.Windows.Input;

namespace MCERemote.Controller.Controls
{
    public class ButtonCheck : System.Windows.Controls.CheckBox
    {
        public ButtonCheck()
        {
            Style = FindResource("Page.Button.CheckBox") as System.Windows.Style;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                IsPressed = true;
                IsChecked = !IsChecked;
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
