using System;

namespace MCERemote.Controller.Layout
{
    /// <summary>
    /// Represents an action that can be performed by the title bar.
    /// </summary>
    public enum TitleBarAction
    {
        None,
        Minimize,
        Close,
        Start,
        Back,
        Settings
    }

    /// <summary>
    /// Provides data for the TitleBarActionOccurred event.
    /// </summary>
    public class TitleBarActionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the action that occurred.
        /// </summary>
        public TitleBarAction Action { get; private set; }

        public TitleBarActionEventArgs(TitleBarAction action)
        {
            Action = action;
        }
    }

    /// <summary>
    /// Provides a way to convert a string to a TitleBarAction.
    /// </summary>
    public class TitleBarActionConverter
    {
        /// <summary>
        /// Converts a string to a TitleBarAction.
        /// </summary>
        /// <param name="key">The string to convert.</param>
        /// <returns>The converted TitleBarAction.</returns>
        public static TitleBarAction Parse(string key)
        {
            try
            {
                return (TitleBarAction)Enum.Parse(typeof(TitleBarAction), key, true);
            }
            catch
            {
                return TitleBarAction.None;
            }
        }
    }
}
