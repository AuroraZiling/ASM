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
using System.Diagnostics;
using System.Collections.Generic;

namespace AuroraServer
{
    public partial class Manage : MetroWindow
    {
        public string IP_address = "127.0.0.1";
        public ushort Port = 25575;
        public string Password = "";
        public string Log = "系统启动中...";
        public string[] PlayerInfo = { };
        public static string now_path = Environment.CurrentDirectory.Replace("\\", "/") + "/";

        public Paragraph commandTemp = new Paragraph();
        public Manage()
        {
            MessageExt.Instance.ShowDialog = ShowDialog;
            MessageExt.Instance.ShowYesNo = ShowYesNo;
            InitializeComponent();

            this.Closing += Window_Closing;
            PlayerControlGrid.Visibility = Visibility.Hidden;

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
                ret = ret.Replace("There are ", "").Replace("a max of ", "").Replace(" players online", "");
                string[] PlayerInfo = Regex.Split(ret, " of ", RegexOptions.IgnoreCase);
                string[] PlayerList;
                try
                {
                    PlayerList = Regex.Split(Regex.Split(PlayerInfo[1], ": ", RegexOptions.IgnoreCase)[1], ", ", RegexOptions.IgnoreCase);
                }
                catch
                {
                    PlayerList = new string[] { };
                }
                ushort PlayerNum = ushort.Parse(PlayerInfo[0]);

                if (PlayerList.Length == 0)
                {
                    PlayerListBox.Items.Clear();
                }
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
                string[] PlayerListShow = PlayerListShowTemp.ToArray();
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
                PlayerNumLabel.Content = PlayerInfo[0] + " 个玩家在线";
                await Task.Delay(1000);
            }
        }

        public async void ConsoleControl() //控制台
        {
            LogUpdate("\n正在尝试连接至RCON服务器...", null, null);
            try
            {
                RCON rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
                await rcon.ConnectAsync();
                Title = "ASM - 管理: " + IP_address + ':' + Port.ToString();
                LogUpdate("\nRCON服务器已成功连接 IP:"+IP_address+" Port:"+Port.ToString(), null, null);
                LogUpdate("\n正在尝试打开玩家检测器...", null, null);
                Task PlayerUpdate = Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        PlayerNumUpdate();
                    }));
                });
                LogUpdate("\n玩家检测器已开启", null, null);
                LogUpdate("\n", null, null);
            }
            catch (AuthenticationException)
            {
                MessageExt.Instance.ShowDialog("您的RCON连接密码无法通过验证, 请检查输入的密码是否正确", "错误");
            }
        }

        public void LogUpdate(string UpdateStr, string commandipt, string originalColor) //控制台信息更新
        {
            if (commandipt == null && originalColor == null)
            {
                Log += UpdateStr;
            }
            else if(originalColor == null)
            {
                Log += ">>>" + commandipt;
                if (UpdateStr != null)
                {
                    Log += "\n" + UpdateStr + "\n";
                }
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
                AffirmativeButtonText = "关闭",
                ColorScheme = MetroDialogColorScheme.Theme
            };
            _ = await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, mySettings);
        }
        public async void ShowYesNo(string message, string title, Action action) // CustomMsgBox的Ok/Cancel对话框
        {
            var mySettings = new MetroDialogSettings()
            {
                AffirmativeButtonText = "好",
                NegativeButtonText = "取消",
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
                LogUpdate("命令成功执行", command, null);
            }
            else if (ret.IndexOf('§') == -1)
            {
                LogUpdate(ret, command, null);
            }
            else
            {
                ret = Regex.Replace(ret, @"§[a-g0-9]", "");
                LogUpdate(ret, command, null);
                /* 彩色输出(失败)
                LogUpdate(null, command, null);
                ret += "§0";
                string tempStr = "";
                string[] process = Regex.Split(ret, "\n", RegexOptions.IgnoreCase);
                string color = "§0";
                var realColor = Brushes.Black;
                foreach (string i in process)
                {
                    for (int j = 0; j < i.Length; ++j)
                    {
                        if (i[j] == '§')
                        {
                            if (tempStr != "")
                            {
                                if (color == "§0")
                                {
                                    realColor = Brushes.Black;
                                }
                                else if (color == "§1")
                                {
                                    realColor = Brushes.DarkBlue;
                                }
                                else if (color == "§2")
                                {
                                    realColor = Brushes.DarkGreen;
                                }
                                else if (color == "§3")
                                {
                                    realColor = Brushes.Aqua; //Wrong
                                }
                                else if (color == "§4")
                                {
                                    realColor = Brushes.DarkRed;
                                }
                                else if (color == "§5")
                                {
                                    realColor = Brushes.DarkViolet;
                                }
                                else if (color == "§6")
                                {
                                    realColor = Brushes.Gold;
                                }
                                else if (color == "§7")
                                {
                                    realColor = Brushes.Gray;
                                }
                                else if (color == "§8")
                                {
                                    realColor = Brushes.DarkGray;
                                }
                                else if (color == "§9")
                                {
                                    realColor = Brushes.Blue;
                                }
                                else if (color == "§a")
                                {
                                    realColor = Brushes.Green;
                                }
                                else if (color == "§b")
                                {
                                    realColor = Brushes.Aqua;
                                }
                                else if (color == "§c")
                                {
                                    realColor = Brushes.Red;
                                }
                                else if (color == "§d")
                                {
                                    realColor = Brushes.Pink;
                                }
                                else if (color == "§e")
                                {
                                    realColor = Brushes.Yellow;
                                }
                                else if (color == "§f")
                                {
                                    realColor = Brushes.White;
                                }
                                else if (color == "§g")
                                {
                                    realColor = Brushes.DarkGoldenrod;
                                }
                                Run commandLineTemp2 = new Run(tempStr) { Foreground = realColor };
                                commandTemp.Inlines.Add(commandLineTemp2);
                                tempStr = "";
                            }
                            color = i[j].ToString() + i[j + 1].ToString();
                            ++j;
                            continue;
                        }
                        else
                        {
                            tempStr += i[j];
                        }
                    }
                }
                Run commandLineTemp = new Run(tempStr) { Foreground = realColor };
                commandTemp.Inlines.Add(commandLineTemp);
                ConsoleTextBox.Document.Blocks.Add(commandTemp);
                */

            }
        }

        private void SaveLogBtn_Click(object sender, RoutedEventArgs e)
        {
            string prepare = "-----Aurora Server Manager Log-----\n时间:" + DateTime.Now + "\n配置:\n    ";
            prepare += "CPU:" + Tools.GetComputerInfo.GetCpuInfo() + "\n    ";
            prepare += "内存:" + Tools.GetComputerInfo.GetMemoryInfo() + "\n";
            prepare += "-----控制台-----" + "\n";
            Log = prepare + Log;
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
            MessageExt.Instance.ShowYesNo("已成功将Log文件保存至\n"+now_path+"ASM/Log.txt"+ "\n\n需要打开日志文件吗?", "提示", new Action(() => {
                    Process.Start(now_path + "ASM/Log.txt");
            }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageExt.Instance.ShowYesNo("确定关闭该系统?", "警告", new Action(() => { Process.GetCurrentProcess().Kill(); }));
            e.Cancel = true;
        }

        private void PlayerListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(PlayerListBox.Items.Count > 0 && PlayerListBox.SelectedItems.Count != 0)
            {
                PlayerControlGrid.Visibility = Visibility.Visible;
                PlayerControlNameLabel.Content = PlayerListBox.SelectedItem;
            }
        }

        private void PlayerControlClose_Click(object sender, RoutedEventArgs e)
        {
            PlayerControlGrid.Visibility = Visibility.Hidden;
        }

        private void PlayerControlBan_Click(object sender, RoutedEventArgs e)
        {
            MessageExt.Instance.ShowYesNo("确认要封禁玩家" + PlayerControlNameLabel.Content + "吗?", "提示", new Action(() =>
            {
                this.Dispatcher.Invoke((Action)async delegate ()
                {
                    RCON rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
                    await rcon.ConnectAsync();
                    await rcon.SendCommandAsync("ban " + PlayerControlNameLabel.Content);
                    await rcon.SendCommandAsync("tellraw @a [{\"text\":\"ASM\",\"color\":\"aqua\",\"bold\":true},{\"text\":\" >>> \",\"color\":\"green\",\"bold\":false},{\"text\":\"" + PlayerControlNameLabel.Content + "已被封禁\",\"color\":\"gold\"}]");
                });
            }));
            PlayerControlGrid.Visibility = Visibility.Hidden;
        }

        private void PlayerControlKick_Click(object sender, RoutedEventArgs e)
        {
            MessageExt.Instance.ShowYesNo("确认要踢出玩家" + PlayerControlNameLabel.Content + "吗?", "提示", new Action(() =>
            {
                this.Dispatcher.Invoke((Action)async delegate ()
                {
                    RCON rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
                    await rcon.ConnectAsync();
                    await rcon.SendCommandAsync("kick " + PlayerControlNameLabel.Content);
                    await rcon.SendCommandAsync("tellraw @a [{\"text\":\"ASM\",\"color\":\"aqua\",\"bold\":true},{\"text\":\" >>> \",\"color\":\"green\",\"bold\":false},{\"text\":\"" + PlayerControlNameLabel.Content + "已被踢出\",\"color\":\"gold\"}]");
                });
            }));
            PlayerControlGrid.Visibility = Visibility.Hidden;
        }

        private void PlayerControlOP_Click(object sender, RoutedEventArgs e)
        {
            MessageExt.Instance.ShowYesNo("确认要将玩家" + PlayerControlNameLabel.Content + "的权限变更为管理员吗?", "提示", new Action(() =>
            {
                this.Dispatcher.Invoke((Action)async delegate ()
                {
                    RCON rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
                    await rcon.ConnectAsync();
                    await rcon.SendCommandAsync("op " + PlayerControlNameLabel.Content);
                    await rcon.SendCommandAsync("tellraw @a [{\"text\":\"ASM\",\"color\":\"aqua\",\"bold\":true},{\"text\":\" >>> \",\"color\":\"green\",\"bold\":false},{\"text\":\"" + PlayerControlNameLabel.Content + "的权限等级已调整为管理员\",\"color\":\"gold\"}]");
                });
            }));
            PlayerControlGrid.Visibility = Visibility.Hidden;
        }

        private void PlayerControlDEOP_Copy_Click(object sender, RoutedEventArgs e)
        {
            MessageExt.Instance.ShowYesNo("确认要将玩家" + PlayerControlNameLabel.Content + "的权限变更为成员吗?", "提示", new Action(() =>
            {
                this.Dispatcher.Invoke((Action)async delegate ()
                {
                    RCON rcon = new RCON(IPAddress.Parse(IP_address), Port, Password);
                    await rcon.ConnectAsync();
                    await rcon.SendCommandAsync("deop " + PlayerControlNameLabel.Content);
                    await rcon.SendCommandAsync("tellraw @a [{\"text\":\"ASM\",\"color\":\"aqua\",\"bold\":true},{\"text\":\" >>> \",\"color\":\"green\",\"bold\":false},{\"text\":\"" + PlayerControlNameLabel.Content + "的权限等级已调整为成员\",\"color\":\"gold\"}]");
                });
            }));
            PlayerControlGrid.Visibility = Visibility.Hidden;
        }
    }
}
