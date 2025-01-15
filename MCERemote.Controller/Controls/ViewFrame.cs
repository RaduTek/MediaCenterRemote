using System;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace MCERemote.Controller.Controls
{
    public class ViewFrame : Frame
    {
        public ViewFrame()
        {
            NavigationUIVisibility = System.Windows.Navigation.NavigationUIVisibility.Hidden;
            Focusable = false;

            Navigated += ViewFrame_Navigated;
        }

        private bool clearHistory = false;

        private void ViewFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            if (clearHistory)
            {
                clearHistory = false;
                ClearBackEntry();
                return;
            }

            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Gets or sets the connection to the receiver.
        /// </summary>
        public Connection Connection { get; set; }

        /// <summary>
        /// Event raised when the navigation history changes.
        /// </summary>
        public event EventHandler HistoryChanged;

        /// <summary>
        /// Navigates to the specified page.
        /// </summary>
        /// <param name="page">The page to navigate to.</param>
        public void OpenPage(PageView page, bool clearHistory = false)
        {
            page.Connection = Connection;
            page.ParentFrame = this;
            Content = page;

            this.clearHistory = clearHistory;
        }

        /// <summary>
        /// Clears the navigation history.
        /// </summary>
        public void ClearBackEntry()
        {
            try
            {
                var entry = RemoveBackEntry();
                while (entry != null)
                {
                    entry = RemoveBackEntry();
                }
            }
            catch { }

            HistoryChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Navigates back to the previous page.
        /// </summary>
        public void GoBackNoException()
        {
            try
            {
                GoBack();
            }
            catch { }
        }
    }
}
