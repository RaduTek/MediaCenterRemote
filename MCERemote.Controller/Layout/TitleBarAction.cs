using System;

namespace MCERemote.Controller.Layout
{
    public enum TitleBarAction
    {
        None,
        Minimize,
        Close,
        Start,
        Back,
        Settings
    }

    public class TitleBarActionEventArgs : EventArgs
    {
        public TitleBarAction Action { get; private set; }

        public TitleBarActionEventArgs(TitleBarAction action)
        {
            Action = action;
        }
    }

    public class TitleBarActionConverter
    {
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
