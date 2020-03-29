using SmartTaskLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SmartUnpack
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IMainView
    {
        MainViewModel viewModel;
        Dictionary<string, TaskBase> AllTasks = new  Dictionary<string, TaskBase>();

        Button[] PasswordButtons;
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel(this);

            
            PasswordButtons = new[] {B1, B2, B3, B4, B5, B6};
            var passwords = Properties.Settings.Default.ArchivePasswords;
            for (int i = 0; i < passwords.Count; i++)
            {
                if (i < 6)
                    PasswordButtons[i].Visibility = Visibility.Visible;
            }
        }

        private void OnFileDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                Dictionary<string, TaskBase> tasks = null;
                int passwordIndex = Array.IndexOf(PasswordButtons, sender);

                  // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(files.Length>0)
                {
                    //Shift Key pressed: 4, Ctrl key: 8
                    if (((int) e.KeyStates & 8) == 8)
                    {
                        var dir = System.IO.Path.GetDirectoryName(files[0]);
                        tasks = Util.ScanDirectory(dir, passwordIndex);                        
                    }
                    else
                    {
                        var filePath = files[0];
                        tasks = Util.CreateTaskForFile(filePath, passwordIndex);                        
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

        private void OnTaskFinished(TaskBase task, bool bSuccessful)
        {
            if (!AllTasks.ContainsKey(task.Hash))
                return;
            
            AllTasks.Remove(task.Hash);

            if (bSuccessful && task.HasSoleChildFolder2Unpack)
            {
                //Move the sole child folder to its parent folder
                var folder = new DirectoryInfo(task.SingleChildFolder2UnpackTo);
                if(task.HasSoleChildFolder2Unpack)
                {
                    var tasks = Util.ScanDirectory(task.SingleChildFolder2UnpackTo, task.PasswordIndex);
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
