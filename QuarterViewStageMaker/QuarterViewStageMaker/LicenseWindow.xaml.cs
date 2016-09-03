using System;
using System.Collections.Generic;
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
using System.Net;
using System.IO;

namespace QuarterViewStageMaker
{
    /// <summary>
    /// LicenseWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class LicenseWindow : Window
    {
        public string[] LicenseAddresses = new[]
        {
            "https://raw.githubusercontent.com/runceel/ReactiveProperty/master/LICENSE.txt",
            "https://raw.githubusercontent.com/JamesNK/Newtonsoft.Json/master/LICENSE.md",
        };

        public LicenseWindow()
        {
            InitializeComponent();

            ShowLicenses();
        }

        public void ShowLicenses()
        {
            foreach(var address in LicenseAddresses)
            {
                var request = WebRequest.Create(address) as HttpWebRequest;
                request.Method = "GET";

                var response = request.GetResponse();
                var license = "";
                using (var reader = new StreamReader(response.GetResponseStream()))
                {
                    license = reader.ReadToEnd();
                }

                if (license == "")
                    continue;

                var label = new Label();
                label.Content = license;

                Panel.Children.Add(label);
            }
        }
    }
}
