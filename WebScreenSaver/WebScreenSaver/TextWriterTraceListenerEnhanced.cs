using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace WebScreenSaver
{
    class TextWriterTraceListenerEnhanced : TextWriterTraceListener
    {
        public TextWriterTraceListenerEnhanced(string filename) :
            base(filename)
        {
        }

    }
}
