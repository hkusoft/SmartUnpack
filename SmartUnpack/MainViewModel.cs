using SmartTaskLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SmartUnpack
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly MainWindow view;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        

        private List<TaskBase> _taskList = new List<TaskBase>();
        public List<TaskBase> TaskList
        {
            get => _taskList;
            set
            {
                _taskList = value;
                OnPropertyChanged("TaskList");
            }
        }

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
                 OnPropertyChanged("IsSomeTaskSelected");
             }
         }

        public TaskBase CurrentSelectedTask { get; set; }

        public MainViewModel(MainWindow view)
        {
            this.view = view;
            this.view.DataContext = this;
        }
        
        private void OnUnpackSelected()
        {
            CurrentSelectedTask?.Unpack();
        }

        public void StartUnpack()
        {
            foreach (var task in TaskList)
            {
                task.OnTaskFinished += OnTaskFinished;
                task.Unpack();
            }
        }

        
        private void OnTaskFinished(TaskBase task, bool bSuccessful)
        {
            TaskList.Remove(task);
            OnPropertyChanged("TaskList");
        }

        
    }
}
