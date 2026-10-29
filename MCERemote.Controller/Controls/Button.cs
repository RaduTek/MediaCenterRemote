using System;
using System.Windows;
using System.Windows.Input;

namespace MCERemote.Controller.Controls
{
    public static class ButtonHelper
    {
        public static readonly DependencyProperty IsEnterPressedProperty =
            DependencyProperty.RegisterAttached(
                "IsEnterPressed",
                typeof(bool),
                typeof(ButtonHelper),
                new PropertyMetadata(false));

        public static void SetIsEnterPressed(UIElement element, bool value)
        {
            element.SetValue(IsEnterPressedProperty, value);
        }

        public static bool GetIsEnterPressed(UIElement element)
        {
            return (bool)element.GetValue(IsEnterPressedProperty);
        }
    }

    public class Button : System.Windows.Controls.Button
    {
        public Button()
        {
            Style = FindResource("Page.Button") as System.Windows.Style;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ButtonHelper.SetIsEnterPressed(this, true);
                RaiseEvent(new RoutedEventArgs(ClickEvent));
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            ButtonHelper.SetIsEnterPressed(this, false);

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
            }

            base.OnKeyUp(e);
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            // Enter may move focus away (e.g. to another page), so the key release won't reach
            // this button to end the press
            ButtonHelper.SetIsEnterPressed(this, false);

            base.OnLostKeyboardFocus(e);
        }
    }

    public class ButtonWide : Button
    {
        public ButtonWide()
        {
            Style = FindResource("Page.Button.Wide") as System.Windows.Style;
        }
    }
}
