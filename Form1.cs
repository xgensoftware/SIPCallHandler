using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using Ozeki.VoIP;
using com.Xgensoftware.Core;
using System.Windows.Forms;

namespace SIPCallHandler
{
    public partial class Form1 : Form
    {
        #region Member Variables
        Logging _log = null;
        OzekiService _ozekiService = null;
        Thread _workerThread = null;

        Logging _logFile = null;
        #endregion

        void LogMessage(string message)
        {
            if (_log != null)
                _log.LogMessage(LogMessageType.INFO, message);

            try
            {
                this.Invoke((MethodInvoker)delegate ()
                {
                    message = string.Format($"{DateTime.Now.ToString("yyyyMMdd HH:mm:ss")} {message}");
                    txtLog.Text = message + Environment.NewLine + txtLog.Text;
                });
            }
            catch { }
        }

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.FormClosing += Form1_FormClosing;

            var logFile = string.Format(ConfigurationManager.AppSettings["LogFile"].ToString(), DateTime.Now.ToString("yyyyMMdd"));
            _log = new Logging(logFile);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogMessage("********* SipCallHandler application stopping *********");

            if (_workerThread != null)
            {
                if (_ozekiService != null)
                    _ozekiService.Stop();

                if (_workerThread.ThreadState == ThreadState.Running)
                    _workerThread.Abort();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LogMessage("********* SipCallHandler application starting *********");
            _ozekiService = new OzekiService();
            _ozekiService.OnMessageEvent += _ozekiService_OnMessageEvent;

            _workerThread = new Thread(new ThreadStart(_ozekiService.Start));
            _workerThread.Start();

        }

        private void _ozekiService_OnMessageEvent(string message)
        {
            LogMessage(message);
        }
    }
}
