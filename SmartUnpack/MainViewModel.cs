using SmartTaskLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartUnpack
{
    public class MainViewModel : ViewModelBase<IMainView>
    {
        public RelayCommand UnpackSelectedCommand
        {
            get;
            private set;
        }

        private bool isSomeTaskSelected;
        public bool IsSomeTaskSelected
        {
            get { return isSomeTaskSelected; }
            set
            {
                isSomeTaskSelected = value;
                RaisePropertyChanged("IsSomeTaskSelected");
            }
        }

        public SharpCompressTask CurrentSelectedTask { get; set; }

        public MainViewModel(IMainView view) : base(view)
        {
            UnpackSelectedCommand = new RelayCommand(OnUnpackSelected, () => IsSomeTaskSelected);
            IsSomeTaskSelected = false;
        }



        private void OnUnpackSelected()
        {
            CurrentSelectedTask?.Unpack();
        }
    }
}
