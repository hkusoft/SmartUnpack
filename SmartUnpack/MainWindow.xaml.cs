using SmartTaskLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace SmartUnpack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainView
    {
        MainViewModel viewModel;
        Dictionary<string, TaskBase> AllTasks = new  Dictionary<string, TaskBase>();
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel(this);
        }

        private void TaskListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Dictionary<string, TaskBase> tasks = null;

                  // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(files.Length>0)
                {
                    //Shift Key pressed: 4, Ctrl key: 8
                    if (((int) e.KeyStates & 8) == 8)
                    {
                        var dir = System.IO.Path.GetDirectoryName(files[0]);
                        tasks = Util.ScanDirectory(dir);                        
                    }
                    else
                    {
                        var filePath = files[0];
                        tasks = Util.CreateTaskForFile(filePath);                        
                    }
                    Unpack(tasks);
                }

                
            }
        }

        private void Unpack(Dictionary<string, TaskBase> tasks)
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

        private void OnTaskFinished(TaskBase sharpCompressTask, bool bSuccessful)
        {
            if (!AllTasks.ContainsKey(sharpCompressTask.Hash))
                return;
            
            AllTasks.Remove(sharpCompressTask.Hash);

            if (bSuccessful && sharpCompressTask.HasSoleChildFolder2Unpack)
            {
                //Move the sole child folder to its parent folder
                var folder = new DirectoryInfo(sharpCompressTask.SingleChildFolder2UnpackTo);
                if(sharpCompressTask.HasSoleChildFolder2Unpack)
                {
                    var tasks = Util.ScanDirectory(sharpCompressTask.SingleChildFolder2UnpackTo);
                    Unpack(tasks);
                    RefreshDataSource();
                    return;
                }
            }                        
            RefreshDataSource();
        }


        private void OnListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                viewModel.IsSomeTaskSelected = true;
                viewModel.CurrentSelectedTask = e.AddedItems[0] as SharpCompressTask;
            }
        }
    }
}
