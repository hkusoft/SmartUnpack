using SmartTaskLib;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace SmartUnpack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainView
    {
        MainViewModel viewModel;
        Dictionary<string, UnpackTask> AllTasks = new  Dictionary<string, UnpackTask>();
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel(this);
        }

        private void TaskListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Dictionary<string, UnpackTask> tasks = null;

                  // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(files.Length>0)
                {
                    //Shift Key pressed: 4, Ctrl key: 8
                    if (((int) e.KeyStates & 8) == 8)
                    {
                        var dir = System.IO.Path.GetDirectoryName(files[0]);
                        tasks = SmartTaskUtil.ScanDirectory(dir);                        
                    }
                    else
                    {
                        var filePath = files[0];
                        tasks = SmartTaskUtil.CreateTaskForFile(filePath);                        
                    }

                    AddUnpackTasks(tasks);

                }

                
            }
        }

        private void AddUnpackTasks(Dictionary<string, UnpackTask> tasks)
        {

            foreach (var task in tasks)
            {
                if (!AllTasks.ContainsKey(task.Key))
                {
                    var item = task.Value;
                    AllTasks[task.Key] = item;

                    item.OnTaskFinished += OnTaskFinished;
                    item.Unpack();
                }
            }

            RefreshDataSource();
        }

        private void RefreshDataSource()
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                TaskListView.ItemsSource = AllTasks.Values;
                TaskListView.Items.Refresh();
            }));            
        }

        private void OnTaskFinished(UnpackTask t, bool bSuccessful)
        {
            if (!AllTasks.ContainsKey(t.Hash))
                return;
            
            AllTasks.Remove(t.Hash);

            if (bSuccessful && !string.IsNullOrEmpty(t.SingleChildFolder2UnpackTo))
            {
                var tasks = SmartTaskUtil.ScanDirectory(t.SingleChildFolder2UnpackTo);
                AddUnpackTasks(tasks);
            }

            RefreshDataSource();                
            
        }
        

        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                viewModel.IsSomeTaskSelected = true;
                viewModel.CurrentSelectedTask = e.AddedItems[0] as UnpackTask;
            }
        }
    }
}
