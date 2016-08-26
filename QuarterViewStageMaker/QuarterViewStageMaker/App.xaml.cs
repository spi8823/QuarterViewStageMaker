using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace QuarterViewStageMaker
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public static string StartupFolder;
        public static string ProjectsFolder;

        public App()
        {
            StartupFolder = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Environment.GetCommandLineArgs()[0]));
            ProjectsFolder = StartupFolder + "\\Projects";
        }
    }
}
