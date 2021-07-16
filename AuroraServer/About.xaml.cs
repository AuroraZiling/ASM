using MahApps.Metro.Controls;
using System.Diagnostics;
using System.Windows;

namespace AuroraServer
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : MetroWindow
    {
        public About()
        {
            InitializeComponent();
        }

        private void AuthorVisitGithubBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/AuroraZiling");
        }

        private void AuthorVisitAfdianBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://afdian.net/@ASMdonate");
        }

        private void AuthorVisitQQBtn_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://wpa.qq.com/msgrd?v=3&uin=2935876049&site=qq&menu=yes");
        }
    }
}
