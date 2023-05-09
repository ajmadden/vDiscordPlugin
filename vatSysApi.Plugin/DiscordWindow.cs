using DiscordPlugin.Common;
using System;
using vatsys;

namespace DiscordPlugin.Plugin
{
    public partial class DiscordWindow : BaseForm
    {
        public DiscordWindow()
        {
            InitializeComponent();
        }

        private void comboBoxDisplay_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (comboBoxDisplay.Text)
            {
                case "None":
                    DiscordPlugin.Details.DisplayType = DisplayType.None;
                    break;
                case "Transmissions Sent":
                    DiscordPlugin.Details.DisplayType = DisplayType.TxSent;
                    break;
                case "Transmissions Received":
                    DiscordPlugin.Details.DisplayType = DisplayType.TxReceived;
                    break;
                case "Transmissions Both":
                    DiscordPlugin.Details.DisplayType = DisplayType.TxBoth;
                    break;
                case "Aircraft Spotted":
                    DiscordPlugin.Details.DisplayType = DisplayType.AcftSpotted;
                    break;
                case "Aircraft Controlled":
                    DiscordPlugin.Details.DisplayType = DisplayType.AcftControlled;
                    break;
                default:
                    break;
            }

            DiscordPlugin.SaveSettings();
        }

        private void DiscordWindow_Load(object sender, EventArgs e)
        {
            switch (DiscordPlugin.Details.DisplayType)
            {
                case DisplayType.None:
                    comboBoxDisplay.Text = "None";
                    return;
                case DisplayType.TxSent:
                    comboBoxDisplay.Text = "Transmissions Sent";
                    return;
                case DisplayType.TxReceived:
                    comboBoxDisplay.Text = "Transmissions Received";
                    return;
                case DisplayType.TxBoth:
                    comboBoxDisplay.Text = "Transmissions Both";
                    return;
                case DisplayType.AcftSpotted:
                    comboBoxDisplay.Text = "Aircraft Spotted";
                    return;
                case DisplayType.AcftControlled:
                    comboBoxDisplay.Text = "Aircraft Controlled";
                    return;
                default:
                    return;
            }
        }
    }
}