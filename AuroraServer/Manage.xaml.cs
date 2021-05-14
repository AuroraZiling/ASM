using CoreRCON.Parsers.Standard;
using CoreRCON;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AuroraServer
{
    public partial class Manage : MetroWindow
    {
        public string IP_address = "127.0.0.1";
        public ushort Port = 25575;
        public string Password = "";
        public string Log = "Aurora Server Manager Starting...";

        public static string now_path = Environment.CurrentDirectory.Replace("\\", "/") + "/";
        public Manage()
        {
            MessageExt.Instance.ShowDialog = ShowDialog;
            MessageExt.Instance.ShowYesNo = ShowYesNo;
            InitializeComponent();

            using (StreamReader file = File.OpenText(now_path + "ASM/connect.json"))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject o = (JObject)JToken.ReadFrom(reader);
                    if (IPAddress.TryParse(o["IP"].ToString(), out IPAddress IPCheck))
                    {
                        IP_address = o["IP"].ToString();
                    }
                    if (ushort.Parse(o["Port"].ToString()) >= 0 && ushort.Parse(o["Port"].ToString()) <= 65535)
                    {
                        Port = ushort.Parse(o["Port"].ToString());
                    }
                    Password = o["Password"].ToString();
                }
            }

            ConsoleControl();
        }

        public async void ConsoleControl()
        {
            var rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
            LogUpdate("\nTrying to connect to the target server...", null);
            try
            {
                await rcon.ConnectAsync();
                LogUpdate("\nRCON server has been successfully connected. IP:"+IP_address+" Port:"+Port.ToString(), null);
                Title = "ASM - Manage for " + IP_address + ':' + Port.ToString();
                LogUpdate("\n>>>", null);
            }
            catch (AuthenticationException)
            {
                MessageExt.Instance.ShowDialog("Your RCON password could not be verified, please check if your password is entered correctly.", "Error");
            }
        }

        public void LogUpdate(string UpdateStr, string commandipt)
        {
            if (commandipt == null)
            {
                Log += UpdateStr;
            }
            else
            {
                Log += commandipt + "\n" + UpdateStr + "\n\n>>>";
            }
            
            Run r = new Run(Log);
            Paragraph para = new Paragraph();
            para.Inlines.Add(r);
            ConsoleTextBox.Document.Blocks.Clear();
            ConsoleTextBox.Document.Blocks.Add(para);
        }

        public static void ExtractResFile(string resFileName, string outputFile)
        {
            BufferedStream inStream = null;
            FileStream outStream = null;
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                inStream = new BufferedStream(asm.GetManifestResourceStream(resFileName));
                outStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

                byte[] buffer = new byte[1024];
                int length;

                while ((length = inStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outStream.Write(buffer, 0, length);
                }
                outStream.Flush();
            }
            finally
            {
                if (outStream != null)
                {
                    outStream.Close();
                }
                if (inStream != null)
                {
                    inStream.Close();
                }
            }
        }

        public sealed class MessageExt // // CustomMsgBox
        {
            /// CustomMsgBox使用方法:
            /// MessageExt.Instance.ShowDialog("查询", "提示");
            /// MessageExt.Instance.ShowYesNo("查询", "提示", new Action(() => {
            ///    MessageBox.Show("我来了");
            /// }));
            private static readonly MessageExt instance = new MessageExt();
            private MessageExt()
            {
            }

            public static MessageExt Instance
            {
                get
                {
                    return instance;
                }
            }
            public Action<string, string> ShowDialog { get; set; }
            public Action<string, string, Action> ShowYesNo { get; set; }
        }

        public async void ShowDialog(string message, string title) // CustomMsgBox的Box
        {
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Close",
                ColorScheme = MetroDialogColorScheme.Theme
            };
            _ = await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, mySettings);
        }
        public async void ShowYesNo(string message, string title, Action action) // CustomMsgBox的YNBox
        {
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Ok",
                NegativeButtonText = "Cancel",
                ColorScheme = MetroDialogColorScheme.Theme
            };
            MessageDialogResult result = await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, mySettings);
            if (result == MessageDialogResult.Affirmative)
                await Task.Factory.StartNew(action);
        }

        private async void CommandRunBtn_Click(object sender, RoutedEventArgs e)
        {
            string command = CommandText.Text;
            var rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
            await rcon.ConnectAsync();
            var ret = await rcon.SendCommandAsync(command);
            if (ret == "")
            {
                LogUpdate("Command execution completed.", command);
            }
            else
            {
                LogUpdate(ret, command);
            }
        }
    }
}
