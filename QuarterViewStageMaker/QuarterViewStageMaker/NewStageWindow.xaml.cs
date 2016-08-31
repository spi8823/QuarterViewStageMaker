﻿using System;
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
using Reactive.Bindings;

/*
The MIT License (MIT)

Copyright (c) 2016 neuecc, xin9le, okazuki

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

namespace QuarterViewStageMaker
{
    /// <summary>
    /// NewStageWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NewStageWindow : Window
    {
        public ReactiveProperty<string> StageName { get; private set; } = new ReactiveProperty<string>("NewStage");
        public ReactiveProperty<int> StageWidth { get; private set; } = new ReactiveProperty<int>(16);
        public ReactiveProperty<int> StageHeight { get; private set; } = new ReactiveProperty<int>(16);

        public NewStageWindow()
        {
            InitializeComponent();

            StageNameBox.DataContext = this;
            StageWidthBox.DataContext = this;
            StageHeightBox.DataContext = this;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
