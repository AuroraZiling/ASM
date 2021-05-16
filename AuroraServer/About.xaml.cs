﻿using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
