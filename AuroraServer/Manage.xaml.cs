using CoreRCON.Parsers.Standard;
using CoreRCON;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

namespace AuroraServer
{
    public partial class Manage : MetroWindow
    {
        public string IP_address = "127.0.0.1";
        public ushort Port = 25575;
        public string Password = "";
        public string Log = "Aurora Server Manager Starting...";
        public string[] PlayerInfo = { };
        public static string now_path = Environment.CurrentDirectory.Replace("\\", "/") + "/";
        public Manage()
        {
            MessageExt.Instance.ShowDialog = ShowDialog;
            MessageExt.Instance.ShowYesNo = ShowYesNo;
            InitializeComponent();

            this.Closing += Window_Closing;

            using(StreamReader file = File.OpenText(now_path + "ASM/connect.json")) //读取ASM/connect.json文件
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
            ConsoleControl(); //开启控制台
        }

        public async void PlayerNumUpdate() //玩家人数刷新
        {
            RCON rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
            await rcon.ConnectAsync();
            while (true)
            {
                var ret = await rcon.SendCommandAsync("list");
                ret = ret.Replace("There are ", "").Replace(" of a max of 10 players online", "");
                string[] PlayerInfo = Regex.Split(ret, ": ", RegexOptions.IgnoreCase);
                string[] PlayerList = new string[] { };
                try
                {
                    PlayerList = Regex.Split(PlayerInfo[1], ", ", RegexOptions.IgnoreCase);
                }
                catch
                {
                    PlayerList = new string[] { }; 
                }
                PlayerInfo[0] = Regex.Split(ret, ":", RegexOptions.IgnoreCase)[0];
                ushort PlayerNum = ushort.Parse(PlayerInfo[0]);

                if (PlayerList.Length == 0)
                {
                    PlayerListBox.Items.Clear();
                }
                string[] PlayerListShow = { };
                List<string> PlayerListShowTemp = new List<string> { };
                for (int i = 0; i < PlayerList.Length; ++i) //PlayerListBox.Items与PlayerList差异添加
                {
                    if (PlayerListBox.Items.IndexOf(PlayerList[i]) == -1)
                    {
                        PlayerListBox.Items.Add(PlayerList[i]);
                    }
                }
                foreach (string i in PlayerListBox.Items) //避免foreach不可中途删除导致的错误
                {
                    PlayerListShowTemp.Add(i);
                }
                PlayerListShow = PlayerListShowTemp.ToArray();
                for (int i = 0; i < PlayerListShow.Length; ++i) //PlayerListBox.Items与PlayerList差异删除
                {
                    try
                    {
                        if (PlayerListBox.Items.IndexOf(PlayerList[i]) == -1)
                        {
                            PlayerListBox.Items.Remove(PlayerList[i]);
                        }
                    }
                    catch
                    {
                        PlayerListBox.Items.Remove(PlayerListShow[i]); ;
                    }
                }

                if (PlayerNum == 0 || PlayerNum == 1)
                {
                    PlayerNumLabel.Content = PlayerInfo[0] + " Player Online";
                }
                else
                {
                    PlayerNumLabel.Content = PlayerInfo[0] + " Players Online";
                }
                await Task.Delay(1000);
            }
        }

        public async void ConsoleControl() //控制台
        {
            LogUpdate("\nTrying to connect to the target server...", null);
            try
            {
                RCON rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
                await rcon.ConnectAsync();
                Title = "ASM - Manage for " + IP_address + ':' + Port.ToString();
                LogUpdate("\nRCON server has been successfully connected. IP:"+IP_address+" Port:"+Port.ToString(), null);
                /*
                LogUpdate("\nTrying to open the listening interface...", null);
                Task PlayerListener = Task.Run(() => // 监听玩家聊天(未完成)
                {
                    while (true)
                    {
                        var log = new LogReceiver(50000, new IPEndPoint(IPAddress.Parse(IP_address), Port));
                        log.Listen<ChatMessage>(chat =>
                        {
                            LogUpdate($"Chat message: {chat.Player.Name} said {chat.Message} on channel {chat.Channel}", null);
                        });
                        log.Dispose();
                    }
                });
                LogUpdate("\nListening interface has been successfully opened.", null);
                */
                LogUpdate("\nTrying to open the player detection system...", null);
                //await PlayerNumUpdate();
                Task PlayerUpdate = Task.Run(() => // 监听玩家聊天(未完成)
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        PlayerNumUpdate();
                    }));
                });
                LogUpdate("\nPlayer detection system has been successfully opened.", null);
            }
            catch (AuthenticationException)
            {
                MessageExt.Instance.ShowDialog("Your RCON password could not be verified, please check if your password is entered correctly.", "Error");
            }
        }

        public void LogUpdate(string UpdateStr, string commandipt) //控制台信息更新
        {
            if (commandipt == null)
            {
                Log += UpdateStr;
            }
            else
            {
                Log += commandipt + "\n" + UpdateStr + "\n";
            }
            
            Run r = new Run(Log);
            Paragraph para = new Paragraph();
            para.Inlines.Add(r);
            ConsoleTextBox.Document.Blocks.Clear();
            ConsoleTextBox.Document.Blocks.Add(para);
        }

        public static void ExtractResFile(string resFileName, string outputFile) //将嵌入资源复制至外部文件夹
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

        public async void ShowDialog(string message, string title) // CustomMsgBox的对话框
        {
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "Close",
                ColorScheme = MetroDialogColorScheme.Theme
            };
            _ = await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, mySettings);
        }
        public async void ShowYesNo(string message, string title, Action action) // CustomMsgBox的Ok/Cancel对话框
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

        private void SaveLogBtn_Click(object sender, RoutedEventArgs e)
        {
            using (StreamWriter sw = new StreamWriter("ASM/Log.txt"))
            {
                    sw.WriteLine(Log);
            }
            string line = "";
            using (StreamReader sr = new StreamReader("ASM/Log.txt"))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                }
            }
            MessageExt.Instance.ShowYesNo("Successfully saved the log file to \n"+now_path+"ASM/Log.txt"+ "\n\nWhether to open the log file?", "Notice", new Action(() => {
                    Process.Start(now_path + "ASM/Log.txt");
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageExt.Instance.ShowYesNo("Confirm to close this program?", "Warning", new Action(() => { Process.GetCurrentProcess().Kill(); }));
            e.Cancel = true;
        }

        
    }
}
