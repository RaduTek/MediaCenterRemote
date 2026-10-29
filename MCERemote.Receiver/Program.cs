using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MCERemote.Receiver
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // WCF calls are dispatched on this thread, which also owns the remoted WMP player,
            // so rejected COM calls to a busy WMP get retried instead of throwing
            Helpers.OleMessageFilter.Register();

            Application.Run(new MainContext());

            Helpers.OleMessageFilter.Revoke();
        }
    }
}
