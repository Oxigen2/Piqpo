using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WebScreenSaver
{
    public partial class SettingsForm : Form
    {
        public SettingsForm(Settings settings)
        {
            InitializeComponent();

            m_settings = settings;
            m_OK = false;

            populateForm();
        }

        public bool OK
        {
            get
            {
                return m_OK;
            }
        }

        private void populateForm()
        {
            this.textBoxGuid.Text = m_settings.guid;
            this.checkBoxMute.Checked = m_settings.mute;
        }

        private void saveSettings()
        {
            m_settings.guid = this.textBoxGuid.Text;
            m_settings.mute = this.checkBoxMute.Checked;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            saveSettings();
            m_OK = true;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
        
        private Settings m_settings;
        private bool m_OK;
    }
}
