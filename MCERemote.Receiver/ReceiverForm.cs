using System;
using System.ServiceModel;
using System.Windows.Forms;

namespace MCERemote.Receiver
{
    public partial class ReceiverForm : Form
    {
        public ReceiverForm()
        {
            InitializeComponent();
        }

        ControllerHost controller;

        private void UpdateStatus()
        {
            statusLabel.Text = controller.State.ToString();

            bool started = controller.State == CommunicationState.Opening ||
                controller.State == CommunicationState.Opened ||
                controller.State == CommunicationState.Faulted;

            startToolStripMenuItem.Enabled = !started;
            stopToolStripMenuItem.Enabled = started;
        }

        private void ReceiverForm_Load(object sender, EventArgs e)
        {
            controller = new ControllerHost();

            controller.Host.Opened += Host_Status_Change;
            controller.Host.Faulted += Host_Status_Change;
            controller.Host.Closed += Host_Status_Change;

            controller.Service.MessageReceived += Service_MessageReceived;

            controller?.Open();

            UpdateStatus();
        }

        private void Service_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            logView.Log(e.Message);
        }

        private void Host_Status_Change(object sender, EventArgs e)
        {
            UpdateStatus();
        }

        private void ReceiverForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            controller?.Close();
        }

        private void Start_Click(object sender, EventArgs e)
        {
            controller?.Open();
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            controller?.Close();
        }

        private void Settings_Click(object sender, EventArgs e)
        {

        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
