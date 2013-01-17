using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace WebScreenSaver
{
    public class Settings
    {
        public Settings()
        {
        }

        public bool valid
        {
            get
            {
                return (this.guid.Length > 0);
            }
        }

        public bool mute
        {
            get
            {
                bool muteOn = false;
                string val = getRegistryValue("mute");
                if (val.Length > 0)
                {
                    muteOn = (int.Parse(getRegistryValue("mute")) > 0);
                }
                return muteOn;
            }
            set
            {
                setRegistryValue("mute", (value ? "1" : "0"));
            }
        }

        public string sourceUrl
        {
            get 
            {
                string root = getRegistryValue("url");
                if (root.Length == 0)
                {
                    root = m_sourceUrl;
                }
                return string.Format("{0}?guid={1}&mute={2}", root, guid, (mute ? "on" : "off"));
            }
        }

        public bool testMode
        {
            get
            {
                bool testMode = false;
                string testModeStr = getRegistryValue("testmode");
                if (testModeStr.Length > 0)
                {
                    try
                    {
                        testMode = bool.Parse(testModeStr);
                    }
                    catch
                    {
                    }
                }
                return testMode;
            }
        }

        public string guid
        {
            get
            {
                return getRegistryValue("guid");
            }
            set
            {
                setRegistryValue("guid", value);
            }
        }

        private string getRegistryValue(string key)
        {
            string val = "";
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(m_keyValue);
            if (regKey != null)
            {
                val = (string)regKey.GetValue(key, "");
            }
            return val;
        }

        private void setRegistryValue(string key, string value)
        {
            // Create or get existing Registry subkey
            RegistryKey regKey = Registry.CurrentUser.CreateSubKey(m_keyValue);

            regKey.SetValue(key, value);
        }

        private const string m_sourceUrl = "http://www.piqpo.com/view_streams.html";
        private const string m_keyValue = "SOFTWARE\\Piqpo"; 
    }
}
