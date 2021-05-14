using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using CoreRCON;
using CoreRCON.Parsers.Standard;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace AuroraServer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public string IP_address = "127.0.0.1";
        public ushort Port = 25575;
        public string Password = "";

        public static string now_path = Environment.CurrentDirectory.Replace("\\", "/") + "/";
        public MainWindow()
        {
            InitializeComponent();
            MessageExt.Instance.ShowDialog = ShowDialog;
            MessageExt.Instance.ShowYesNo = ShowYesNo;

            CheckFiles();
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
            IP_Textbox.Text = IP_address;
            Port_Textbox.Text = Port.ToString();
            Password_Passwordbox.Password = Password;
        }

        public static void CheckFiles()
        {
            if (Directory.Exists(now_path + "ASM") == false)
            {
                Directory.CreateDirectory(now_path + "ASM");
            }
            if (File.Exists(now_path + "ASM/connect.json") == false)
            {
                File.Create(now_path + "ASM/connect.json").Dispose();
                ExtractResFile("AuroraServer.ExampleFiles.example_connect.json", now_path + "ASM/connect.json");
            }
        }

        public static void SaveConnectFile(string IP_t, ushort Port_t, string Password_t)
        {
            string jsonString = File.ReadAllText(now_path + "ASM/connect.json", Encoding.Default);
            JObject jobject = JObject.Parse(jsonString);
            jobject["IP"] = IP_t;
            jobject["Port"] = Port_t;
            jobject["Password"] = Password_t;
            string convertString = Convert.ToString(jobject);
            File.WriteAllText(now_path + "ASM/connect.json", convertString);
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

        public static bool CheckConnect(string ipString, int port) // IP地址检测
        {
            System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient()
            { SendTimeout = 1000 };
            IPAddress ip = IPAddress.Parse(ipString);
            try
            {
                tcpClient.Connect(ip, port);
            }
            catch (Exception)
            {
                return false;
            }
            bool right = tcpClient.Connected;
            tcpClient.Close();
            tcpClient.Dispose();
            return right;
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string tmp_detail = "";
            if (IP_Textbox.Text != "" && Port_Textbox.Text != "")
            {
                if (!IPAddress.TryParse(IP_Textbox.Text, out IPAddress IPCheck))
                {
                    tmp_detail += "Illegal IP address\n";
                }
                if (int.Parse(Port_Textbox.Text) < 0 || int.Parse(Port_Textbox.Text) > 65536)
                {
                    tmp_detail += "Wrong port";
                }
                if(tmp_detail != "")
                {
                    MessageExt.Instance.ShowDialog(tmp_detail, "Error");
                }
                else
                {
                    if (CheckConnect(IP_Textbox.Text, int.Parse(Port_Textbox.Text)))
                    {
                        IP_address = IP_Textbox.Text;
                        Port = ushort.Parse(Port_Textbox.Text);
                        Password = Password_Passwordbox.Password;

                        var rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
                        try
                        {
                            await rcon.ConnectAsync();

                            SaveConnectFile(IP_address, Port, Password);

                            Manage manage_window = new Manage();
                            manage_window.ShowDialog();
                        }
                        catch (AuthenticationException)
                        {
                            MessageExt.Instance.ShowDialog("Your RCON password could not be verified, please check if your password is entered correctly.", "Error");
                        }
                    }
                    else
                    {
                        MessageExt.Instance.ShowDialog("Unable to connect to the server, please check whether the IP address of the server is correct and whether the port number is open.", "Error");
                    }
                }
            }
        }

        private void IP_Textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(IP_Textbox.Text == "")
            {
                IP_Textbox.BorderBrush = Brushes.Red;
            }
            else
            {
                IP_Textbox.BorderBrush = Brushes.Black;
            }
        }

        private void Port_Textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Port_Textbox.Text == "")
            {
                Port_Textbox.BorderBrush = Brushes.Red;
            }
            else
            {
                Port_Textbox.BorderBrush = Brushes.Black;
            }
        }

        private void Port_Textbox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Delete || e.Key == Key.Back || e.Key == Key.Left))  
            {
                e.Handled = true;
            }
        }
    }
}
