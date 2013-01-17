using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.Security.Principal;

namespace WebScreenSaver
{
    public class Entry
    {
        public void Run(string[] args)
        {
            try
            {
                string appFolder = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                string piqpoFolder = System.IO.Path.Combine(appFolder, "Piqpo");
                if (!System.IO.Directory.Exists(piqpoFolder))
                {
                    try
                    {
                        System.IO.Directory.CreateDirectory(piqpoFolder);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        string error = string.Format("Error on start-up, not authorized to write to {0}.", piqpoFolder);
                        throw new Exception(error);
                    }
                }

                string logFolder = System.IO.Path.Combine(piqpoFolder, "Log");
                if (!System.IO.Directory.Exists(logFolder))
                {
                    System.IO.Directory.CreateDirectory(logFolder);
                }

                string logfile = System.IO.Path.Combine(logFolder, string.Format("piqpo_{0}.txt", Process.GetCurrentProcess().Id));

                TextWriterTraceListenerEnhanced listener = new TextWriterTraceListenerEnhanced(logfile);
                listener.TraceOutputOptions = TraceOptions.DateTime;
                Trace.Listeners.Add(listener);
                Trace.AutoFlush = true;

                Trace.TraceInformation("Starting trace");

                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                Trace.TraceInformation("Identity info : " + string.Format("auth type {0}; is anon {1}; is auth {2}; is guest {3}; is system {4}",
                    identity.AuthenticationType, identity.IsAnonymous, identity.IsAuthenticated, identity.IsGuest, identity.IsSystem));

                WindowsPrincipal principal = new WindowsPrincipal(identity);
                Trace.TraceInformation("Pricipal roles: " + string.Format("admin {0}; power user {1}; guest {2}; user {3}",
                    principal.IsInRole(WindowsBuiltInRole.Administrator),
                    principal.IsInRole(WindowsBuiltInRole.PowerUser),
                    principal.IsInRole(WindowsBuiltInRole.Guest),
                    principal.IsInRole(WindowsBuiltInRole.User)
                    ));

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                bool runScreenSaver = true;

                ms_settings = new Settings();

                // Deal with command arguments
                // /c - invoke a settings dialog
                // /s - run screen saver
                // /p <HWND> - preview as a child of hwnd
                // /a <HWND> - change password model to hwnd
                if (args.Length > 0)
                {
                    string option = args[0].ToLower().Trim().Substring(0, 2);
                    switch (option)
                    {
                        case "/c":
                            {
                                // Settings dialog
                                Trace.TraceInformation("Settings mode called.");
                                Application.Run(new SettingsForm(ms_settings));
                                runScreenSaver = false;
                                break;
                            }
                        case "/s":
                            {
                                Trace.TraceInformation("Run screensaver called.");
                                runScreenSaver = true;
                                break;
                            }
                        case "/p":
                            {
                                // Preview, just exit for now
                                Trace.TraceInformation("Preview mode called - not implemented.");
                                runScreenSaver = false;
                                break;
                            }
                        case "/a":
                            {
                                // Change password
                                Trace.TraceInformation("Change password mode called - not implemented.");
                                runScreenSaver = false;
                                break;
                            }
                    }
                }

                if (runScreenSaver)
                {
                    // Run saver, but first check settings
                    bool OK = true;
                    while ((!ms_settings.valid) && (OK))
                    {
                        Trace.TraceInformation("Settings not valid, launching dialog.");
                        SettingsForm form = new SettingsForm(ms_settings);
                        Application.Run(form);
                        OK = form.OK;
                    }

                    if (OK)
                    {
                        MainForm primary = null;
                        List<MainForm> childForms = new List<MainForm>();
                        for (int xx = Screen.AllScreens.GetLowerBound(0); xx <= Screen.AllScreens.GetUpperBound(0); ++xx)
                        {
                            Trace.TraceInformation(string.Format("Creating form for screen {0}", xx));

                            MainForm form = new MainForm(xx, ms_settings, primary);
                            if (xx == 0)
                            {
                                primary = form;
                            }
                        }
                        Application.Run(primary);
                    }
                }
            }
            catch (Exception ee)
            {
                string message = "Exception caught: " + System.Environment.NewLine + ee.Message;

                MessageBox.Show(message, "Piqpo Screensaver Error");
            }
        }

        private static Settings ms_settings;
    }
}
