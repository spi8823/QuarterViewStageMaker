using System;
using System.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Reactive.Bindings;

namespace QuarterViewStageMaker
{
    /// <summary>
    /// MapObjectSettingPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class MapObjectSettingPanel : UserControl
    {
        public MapObject MapObject;
        public DataTable Parameters;

        public MapObjectSettingPanel()
        {
            InitializeComponent();
        }

        public void ShowDatas(MapObject mapObject)
        {
            MapObject = mapObject;

            XUpDown.Value = MapObject?.Position.X;
            YUpDown.Value = MapObject?.Position.Y;
            ZUpDown.Value = MapObject?.Position.Z;

            TagTextBox.Text = MapObject?.Tag;
            DiscriptionTextBox.Text = MapObject?.Tag;

            SetDataTable();
        }

        private void SetDataTable()
        {
            Parameters = new DataTable();
            Parameters.Columns.Add("Key");
            Parameters.Columns.Add("Value");
            if (MapObject != null)
            {
                foreach (var item in MapObject.Parameters)
                {
                    var row = Parameters.NewRow();
                    row[0] = item.Key;
                    row[1] = item.Value.ToString();
                }
            }

            ParametersDataGrid.ItemsSource = Parameters.DefaultView;
        }

        protected class Parameter
        {
            public string Key;
            public double Value;

            public Parameter()
            {
                Key = "";
                Value = 0;
            }

            public Parameter(KeyValuePair<string, double> pair)
            {
                Key = pair.Key;
                Value = pair.Value;
            }
        }

        private void ParametersDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (MapObject == null)
            {
                e.Cancel = true;
                return;
            }

            var success = true;
            var parameters = new Dictionary<string, double>();
            foreach(var item in ParametersDataGrid.Items)
            {

                var view = item as DataRowView;
                if (view == null)
                    continue;
                var row = view.Row;
                var key = row.ItemArray[0] as string;
                if (key == null)
                    continue;

                if (parameters.ContainsKey(key as string))
                {
                    success = false;
                    break;
                }
                double value;
                if(double.TryParse(row.ItemArray[1] as string, out value))
                {
                    parameters.Add(key, value);
                }
                else
                {
                    success = false;
                    break;
                }
            }

            if(success)
            {
                MapObject.Parameters = parameters;
            }
            else
            {
                e.Cancel = true;
            }
        }
    }
}
