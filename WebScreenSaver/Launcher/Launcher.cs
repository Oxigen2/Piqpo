using System;
using System.IO;
using System.Reflection;

namespace Launcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string appFolder = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string launcherFolder = Path.Combine(appFolder, "Launcher");
            string filename = Path.Combine(launcherFolder, "Piqpo.exe");

            if (File.Exists(filename))
            {
                Assembly aa = Assembly.LoadFile(filename);

                Type type = aa.GetType("WebScreenSaver.Entry");

                object oo = Activator.CreateInstance(type);

                MethodInfo methodInfo = type.GetMethod("Run");
                object[] methodArgs = { args };
                methodInfo.Invoke(oo, methodArgs);
            }
        }
    }
}
