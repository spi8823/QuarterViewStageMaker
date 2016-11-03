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
        public static readonly RoutedEvent MapObjectSettingChangedEvent = EventManager.RegisterRoutedEvent("MapObjectSettingChanged", RoutingStrategy.Bubble, typeof(EventHandler<MapObjectSettingChangedEventArgs>), typeof(MapObjectSettingPanel));
        public event RoutedEventHandler MapObjectSettingChanged
        {
            add { AddHandler(MapObjectSettingChangedEvent, value); }
            remove { RemoveHandler(MapObjectSettingChangedEvent, value); }
        }

        public class MapObjectSettingChangedEventArgs : RoutedEventArgs
        {
            public MapObject MapObject;

            public MapObjectSettingChangedEventArgs(RoutedEvent routedEvent, object source, MapObject mapObject)
                : base(routedEvent, source)
            {
                MapObject = mapObject;
            }
        }

        public MapObject MapObject;

        public MapObjectSettingPanel()
        {
            InitializeComponent();
        }

        private bool _IsDataShowing = true;
        public void ShowDatas(MapObject mapObject)
        {
            MapObject = mapObject;

            _IsDataShowing = true;
            XUpDown.Value = MapObject?.Position.X;
            YUpDown.Value = MapObject?.Position.Y;
            ZUpDown.Value = MapObject?.Position.Z;

            TagTextBox.Text = MapObject?.Tag ?? "";
            DiscriptionTextBox.Text = MapObject?.Discription ?? "";
            _IsDataShowing = false;

            SetDataTable();
        }

        private void SetDataTable()
        {
            ParameterListPanel.Children.Clear();
            if (MapObject?.Parameters == null)
                return;
            var index = 0;
            foreach(var item in MapObject.Parameters)
            {
                AddKeyValuePanel(item.Key, item.Value, index);
                index++;
            }
            AddKeyValuePanel("", "", index);

            ParameterListPanel.UpdateLayout();
        }

        private void AddKeyValuePanel(string key, string value, int index)
        {
            var panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            var keyBox = new TextBox();
            keyBox.Text = key;
            keyBox.Width = 90;
            keyBox.Tag = index;
            keyBox.TextChanged += KeyBox_TextChanged;
            panel.Children.Add(keyBox);
            var valueBox = new TextBox();
            valueBox.Text = value;
            valueBox.Width = 87;
            valueBox.Tag = index;
            valueBox.TextChanged += ValueBox_TextChanged;
            panel.Children.Add(valueBox);
            ParameterListPanel.Children.Add(panel);
        }

        private void KeyBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var keyBox = sender as TextBox;
            var index = (int)keyBox.Tag;
            if(index == MapObject.Parameters.Keys.Count && !string.IsNullOrWhiteSpace(keyBox.Text))
            {
                AddKeyValuePanel("", "", index + 1);
            }
            UpdateParameters();
        }

        private void ValueBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateParameters();
        }

        private void UpdateParameters()
        {
            var parameters = new Dictionary<string, string>();
            foreach(StackPanel panel in ParameterListPanel.Children)
            {
                var keyBox = panel.Children[0] as TextBox;
                var valueBox = panel.Children[1] as TextBox;
                if (keyBox == null || valueBox == null)
                    continue;
                if (string.IsNullOrWhiteSpace(keyBox.Text))
                    continue;
                if (valueBox.Text == null)
                    valueBox.Text = "";
                if (!parameters.ContainsKey(keyBox.Text))
                    parameters.Add(keyBox.Text, valueBox.Text);
            }
            MapObject.Parameters = parameters;
        }

        protected class Parameter
        {
            public string Key;
            public string Value;

            public Parameter()
            {
                Key = "";
                Value = "";
            }

            public Parameter(KeyValuePair<string, string> pair)
            {
                Key = pair.Key;
                Value = pair.Value;
            }
        }

        private void PositionUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_IsDataShowing)
                return;
            var position = new Point(MapObject.Position);
            MapObject.Position = new Point(XUpDown.Value ?? MapObject.Position.X, YUpDown.Value ?? MapObject.Position.Y, ZUpDown.Value ?? MapObject.Position.Z);
            RaiseEvent(new MapObjectSettingChangedEventArgs(MapObjectSettingChangedEvent, this, MapObject));
        }

        private void Parameter_Changed(object sender, TextChangedEventArgs e)
        {
            if (_IsDataShowing)
                return;
            MapObject.Tag = TagTextBox.Text;
            MapObject.Discription = DiscriptionTextBox.Text;
            RaiseEvent(new MapObjectSettingChangedEventArgs(MapObjectSettingChangedEvent, this, MapObject));
        }
    }
}
