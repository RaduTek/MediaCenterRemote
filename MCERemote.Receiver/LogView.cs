using System;
using System.Windows.Forms;

namespace MCERemote.Receiver
{
    internal class LogView : ListView
    {
        public LogView()
        {
            View = View.Details;
            FullRowSelect = true;
            Columns.Add("Time", 100);
            Columns.Add("Message", 400);
        }

        public void Log(string message)
        {
            var item = new ListViewItem(DateTime.Now.ToString("HH:mm:ss"));
            item.SubItems.Add(message);
            Items.Add(item);
            EnsureVisible(Items.Count - 1);
        }

        protected override void OnResize(EventArgs e)
        {
            Columns[1].Width = ClientSize.Width - Columns[0].Width - 20;
            base.OnResize(e);
        }
    }
}
