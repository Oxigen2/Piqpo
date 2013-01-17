using System;
using System.Collections.Generic;
using System.Text;

namespace WebScreenSaver
{
    class DisplayItem
    {
        public DisplayItem(string sourceUrl, string targetLink, int pause)
        {
            m_sourceUrl = sourceUrl;
            m_targetLink = targetLink;
            m_pause = pause;
        }

        public string sourceUrl { get { return m_sourceUrl; } }
        public string targetLink { get { return m_targetLink; } }
        public int pause { get { return m_pause; } }

        private string m_sourceUrl;
        private string m_targetLink;
        private int m_pause;
    }
}
