using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WMPUtil;
using WMPLib;

namespace MCERemote.Receiver
{
    public partial class WMPTestForm : Form
    {
        public WMPTestForm()
        {
            InitializeComponent();
        }

        WindowsMediaPlayer wmp;

        private void playButton_Click(object sender, EventArgs e)
        {
            wmp.controls.play();
        }

        private void pauseButton_Click(object sender, EventArgs e)
        {
            wmp.controls.pause();
        }

        private void WMPTestForm_Load(object sender, EventArgs e)
        {
            Connect();
        }

        private void Connect()
        {
            connStatus.Text = "Please wait...";
            connectBtn.Enabled = false;
            if (!WMPRemote.IsWindowMediaPlayerLoaded)
            {
                MessageBox.Show("Windows Media Player is not loaded.");
                connStatus.Text = "Disconnected - WMP is not loaded";
                connectBtn.Enabled = true;
            }
            else
            {
                wmp = WMPRemote.ActivePlayer;
                wmp.StatusChange += new _WMPOCXEvents_StatusChangeEventHandler(Wmp_StatusChange);
                connStatus.Text = "Connected";
            }
        }

        private void Wmp_StatusChange()
        {
            statusLabel.Text = wmp.status;

            string title = wmp.currentMedia.getItemInfo("Title");
            string album = wmp.currentMedia.getItemInfo("Album");
            string artist = wmp.currentMedia.getItemInfo("Artist");

            statusBox.Text = $"Title: {title}\r\nAlbum: {album}\r\nArtist: {artist}";
        }

        private void connectBtn_Click(object sender, EventArgs e)
        {
            Connect();
        }
    }
}
