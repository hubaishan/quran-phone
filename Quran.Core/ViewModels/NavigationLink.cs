﻿using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Quran.Core.ViewModels
{
    public class NavigationLink : BaseViewModel
    {
        public Symbol symbol { get; set; }
        public Symbol Symbol
        {
            get { return symbol; }
            set
            {
                if (value == symbol)
                    return;

                symbol = value;

                base.OnPropertyChanged(() => Symbol);
            }
        }

        private string label;
        public string Label
        {
            get { return label; }
            set
            {
                if (value == label)
                    return;

                label = value;

                base.OnPropertyChanged(() => Label);
            }
        }
        public Action Action { get; set; }

        public override Task Initialize()
        {
            return Refresh();
        }

        public override Task Refresh()
        {
            return Task.FromResult(0);
        }
    }
}
