using System;

namespace WebScreenSaver
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Entry entry = new Entry();
            entry.Run(args);
        }
    }
}
