﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;

namespace SmartUnpack
{

    public class ViewModelBase<ViewType> : INotifyPropertyChanged where ViewType : IView
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly ViewType view;
        public ViewType View
        {
            get
            {
                return this.view;
            }
        }
        public ViewModelBase(ViewType view)
        {
            this.view = view;
            this.View.DataContext = this;
        }
        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
