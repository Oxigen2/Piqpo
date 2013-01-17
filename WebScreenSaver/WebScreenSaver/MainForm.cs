using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Timers;
using System.Diagnostics;
using Microsoft.Win32;

namespace WebScreenSaver
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)] 
    public partial class MainForm : Form
    {
        public MainForm(int displayNum, Settings settings, MainForm parent)
        {
            m_settings = settings;

            m_displayNumber = displayNum;
            
            m_frameMouseCoords = new Dictionary<int, Point>();

            m_navigationFailures = 0;
            m_targetUrl = "";

            m_childForms = new List<MainForm>();

            m_parent = parent;
            if (parent != null)
            {
                parent.addChild(this);
            }

            InitializeComponent();
        }

        private WebBrowserEnhanced webBrowser;

        private readonly Settings m_settings;

        private readonly int m_displayNumber;

        private string m_targetUrl;

        private Dictionary<int, Point> m_frameMouseCoords;

        private System.Timers.Timer m_timer;
        private int m_navigationFailures;

        private MainForm m_parent;
        private List<MainForm> m_childForms;

        private bool m_launching = false;
        private object ms_launchingLock = new object();

        private static bool ms_shuttingDown = false;
        private static object ms_shutdownLock = new object();

        public void addChild(MainForm form)
        {
            m_childForms.Add(form);
        }

        public void setTargetUrl(string url)
        {
            if (url != null)
            {
                Trace.TraceInformation("Target url set to " + url);
                m_targetUrl = url;

                showApp();
            }
            else
            {
                Trace.TraceInformation("Target url set to null value");
                m_targetUrl = "";
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Trace.TraceInformation(string.Format("Form {0} loaded.", m_displayNumber));
            if (m_childForms != null)
            {
                foreach (MainForm childForm in m_childForms)
                {
                    childForm.Show();
                }
            }

            // Add the web browser component
            createWebBrowser();

            // Fill the screen
            this.Bounds = Screen.AllScreens[m_displayNumber].Bounds;
            //this.Bounds = new Rectangle(this.Bounds.X, this.Bounds.Y, this.Bounds.Width, this.Bounds.Height / 2);
            Trace.TraceInformation(string.Format("Bounds for form {0} set to {1}, {2} - {3},{4}.", m_displayNumber, this.Bounds.X, this.Bounds.Y, this.Bounds.Width, this.Bounds.Height));

            webBrowser.Bounds = Screen.AllScreens[m_displayNumber].Bounds;
            webBrowser.ScriptErrorsSuppressed = true;
            webBrowser.ObjectForScripting = this;

            // Want to hide the app until we have a slide to show.
            hideApp();

            // Mute if set, or if not the first screen
            SoundControl.SetVolume( (m_displayNumber == 0) ? (m_settings.mute ? 0 : 10) : 0); 

            // Turn off the click from navigating pages
            URLSecurityZoneAPI.InternetSetFeatureEnabled(URLSecurityZoneAPI.InternetFeaturelist.DISABLE_NAVIGATION_SOUNDS, URLSecurityZoneAPI.SetFeatureOn.PROCESS, true);

            // Navigate to the script
            navigateToPage();
        }

        private void timerCallback(object sender, ElapsedEventArgs e)
        {
            m_timer.Enabled = false;
            navigateToPage();
        }

        private void navigateToPage()
        {
            Trace.TraceInformation("About to set page to " + m_settings.sourceUrl);

            Uri uri = new Uri(m_settings.sourceUrl);
            webBrowser.Navigate(uri);
        }

        private void hideApp()
        {
            // Can't be set to 0 or it doesn't receive the mouse move event.
            Trace.TraceInformation("App hidden");
            this.Opacity = .01;
        }

        private void showApp()
        {
            Trace.TraceInformation("App displayed");
            bool test = m_settings.testMode;
            Trace.TraceInformation(test ? "Test mode on" : "Test mode off");
            if (test)
            {
                this.TopMost = true;
                this.Opacity = .2;
            }
            else
            {
                this.TopMost = true;
                Cursor.Hide();
                this.Opacity = 1;
            }
        }

        private void webBrowser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            lock (ms_shutdownLock)
            {
                if (!ms_shuttingDown)
                {
                    ms_shuttingDown = true;

                    Trace.TraceInformation("PreviewKeyDown called");

                    if (e.KeyCode == Keys.Space)
                    {
                        Trace.TraceInformation(string.Format("Space bar hit on screen {0}", m_displayNumber));
                        hideAll();
                        launchLinks();
                    }

                    close();
                }
            }
        }

        private void hide()
        {
            this.Bounds = new Rectangle(0, 0, 0, 0);            
        }

        private void hideAll()
        {
            if (m_parent != null)
            {
                m_parent.hideAll();
            }
            else
            {
                this.hide();

                foreach (MainForm form in m_childForms)
                {
                    form.hide();
                }
            }
        }

        private string getDefaultBrowser()
        {
            string browser = string.Empty;
            RegistryKey key = null;
            try
            {
                key = Registry.ClassesRoot.OpenSubKey(@"HTTP\shell\open\command");

                //trim off quotes
                browser = key.GetValue(null).ToString().ToLower().Replace("\"", "");
                browser = browser.Substring(0, browser.LastIndexOf(".exe"));
                browser = browser.Substring(browser.LastIndexOf('\\') + 1);
            }
            finally
            {
                if (key != null) 
                {    
                    key.Close();
                }
            }
            return browser;
        }
        
        private void launchLinks()
        {
            if (m_parent != null)
            {
                m_parent.launchLinks();
            }
            else
            {
                lock (ms_launchingLock)
                {
                    if (!m_launching)
                    {
                        m_launching = true;

                        // See if default browser is running.  Don't launch if it isn't.
                        string browser = getDefaultBrowser();
                        if (browser.Length == 0)
                        {
                            Trace.TraceError("Unable to locate default browser");
                            return;
                        }

                        Process[] processList = Process.GetProcessesByName( browser );

                        if ( processList.Length == 0 )
                        {
                            Trace.TraceWarning("Default browser \"" + browser + "\" not running so not launching page.");
                            return;
                        }

                        List<string> urls = new List<string>();
                        if (m_targetUrl.Length > 0)
                        {
                            urls.Add(m_targetUrl);
                            Trace.TraceInformation(string.Format("About to open page {0}", m_targetUrl));
                        }
                        foreach (MainForm childForm in m_childForms)
                        {
                            string url = childForm.m_targetUrl;
                            if (url.Length > 0)
                            {
                                urls.Add(url);
                                Trace.TraceInformation(string.Format("About to open page {0}", url));
                            }
                        }

                        try
                        {
                            launchUsingDirectCall(urls);
                            //launchUsingBatFile(urls);
                        }
                        catch (Exception ee)
                        {
                            Trace.TraceError(string.Format("Caught exception on opening page(s): {0}", ee.Message));
                        }
                    }
                }
            }
        }

        private void launchUsingDirectCall(List<string> urls)
        {
            foreach (string url in urls)
            {
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = url;
                info.LoadUserProfile = true;
                System.Diagnostics.Process.Start(info);
            }
        }

        private void launchUsingBatFile(List<string> urls)
        {
            string[] lines = new string[urls.Count + 2];
            lines[0] = "@echo off";
            for (int ll = 0; ll < urls.Count; ++ll)
            {
                lines[ll + 1] = string.Format("start {0}", urls[ll].Replace("&", "^&"));
            }
            lines[urls.Count + 1] = "timeout 99999";
            string appFolder = Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string piqpoFolder = System.IO.Path.Combine(appFolder, "Piqpo");
            string batFile = System.IO.Path.Combine(piqpoFolder, "launch.bat");
            System.IO.File.WriteAllLines(batFile, lines);

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = batFile;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            System.Diagnostics.Process.Start(info);
        }

        private void close()
        {
            Application.Exit();
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                Trace.TraceInformation(string.Format("DocumentCompleted called with url {0}", e.Url));

                attachMouseMove(webBrowser.Document, 0);

                for (int ii = 0; ii < webBrowser.Document.Window.Frames.Count; ++ii)
                {
                    attachMouseMove(webBrowser.Document.Window.Frames[ii].Document, ii + 1);
                }
            }
            catch (Exception ee)
            {
                Trace.TraceError("Exception caught in webBrowser_DocumentCompleted: {0}", ee.Message);
            }
        }

        private void attachMouseMove(HtmlDocument document, int id)
        {
            Trace.TraceInformation(string.Format("Attaching mouse move for document with url {0} and id {1}", document.Url, id));

            HtmlElementEventHandler handler = delegate(object sender, HtmlElementEventArgs e) { doc_MouseMove(sender, e, id); };
            document.MouseMove += handler;

            m_frameMouseCoords[id] = new Point();
        }

        void doc_MouseMove(object sender, HtmlElementEventArgs e, int id)
        {
            lock (ms_shutdownLock)
            {
                if (!ms_shuttingDown)
                {
                    // Trace.TraceInformation(string.Format("MouseMove called with X {0} and Y {1} and id {2}, not closing as move is too small", e.MousePosition.X, e.MousePosition.Y, id));
                    if ((m_frameMouseCoords.ContainsKey(id)) && !(m_frameMouseCoords[id].IsEmpty))
                    {
                        // If the mouse actually moved more than 10 pixes in any direction
                        if (Math.Abs(m_frameMouseCoords[id].X - e.MousePosition.X) > 10
                            || Math.Abs(m_frameMouseCoords[id].Y - e.MousePosition.Y) > 10)
                        {
                            // Close
                            if (!m_settings.testMode)
                            {
                                Trace.TraceInformation("Closing due to MouseMove");
                                close();
                            }
                        }
                    }
                    else
                    {
                        Trace.TraceInformation("Mouse coords empty");
                    }

                    // Set the new point where the mouse is
                    m_frameMouseCoords[id] = new Point(e.MousePosition.X, e.MousePosition.Y);
                }
            }
        }

        private void webBrowser_NavigateError(object sender, WebBrowserNavigateErrorEventArgs e)
        {
            try
            {
                Trace.TraceError("Error attempting to navigate to " + e.Url);

                m_navigationFailures++;

                int pauseInSeconds = (int)System.Math.Min(System.Math.Pow(2, m_navigationFailures), 3600);

                if (webBrowser.Document != null)
                {
                    attachMouseMove(webBrowser.Document, 0);
                }

                string error = string.Format("Navigation has failed {0} time{1}, trying again in {2} seconds.", m_navigationFailures, (m_navigationFailures == 1 ? "" : "s"), pauseInSeconds);
                Trace.TraceError(error);
                webBrowser.DocumentText = error;
                showApp();

                m_timer = new System.Timers.Timer(pauseInSeconds * 1000);
                m_timer.Elapsed += new ElapsedEventHandler(this.timerCallback);
                m_timer.Enabled = true;
            }
            catch (Exception ee)
            {
                Trace.TraceError("Exception caught in webBrowser_NavigateError: {0}", ee.Message);
            }
        }

        private void MainForm_MouseMove(object sender, MouseEventArgs e)
        {
            Trace.TraceInformation(string.Format("MainForm MouseMove called with X {0} and Y {1}", e.X, e.Y));
        }

        private void createWebBrowser()
        {
            this.webBrowser = new WebBrowserEnhanced();
            this.SuspendLayout();

            this.webBrowser.AllowWebBrowserDrop = false;
            this.webBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser.IsWebBrowserContextMenuEnabled = false;
            this.webBrowser.Location = new System.Drawing.Point(0, 0);
            this.webBrowser.Margin = new System.Windows.Forms.Padding(0);
            this.webBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.ScrollBarsEnabled = false;
            this.webBrowser.Size = new System.Drawing.Size(595, 357);
            this.webBrowser.TabIndex = 0;
            this.webBrowser.WebBrowserShortcutsEnabled = false;
            this.webBrowser.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowser_DocumentCompleted);
            this.webBrowser.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.webBrowser_PreviewKeyDown);
            this.webBrowser.NavigateError += new WebBrowserNavigateErrorEventHandler(this.webBrowser_NavigateError);

            this.Controls.Add(this.webBrowser);

            this.ResumeLayout();
        }
    }
}
