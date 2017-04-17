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
        public MainWindow()
        {
            InitializeComponent();
            viewModel = new MainViewModel(this);
        }

        private void TaskListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                List<UnpackTask> tasks = null;                    

                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(files.Length>0)
                {
                    //Shift Key pressed
                    if (((int) e.KeyStates & 4) == 4)
                    {
                        var dir = System.IO.Path.GetDirectoryName(files[0]);
                        tasks = SmartTaskUtil.ScanDirectory(dir);
                    }
                    else
                    {
                        var filePath = files[0];
                        tasks = SmartTaskUtil.CreateTaskForFile(filePath);                        
                    }

                    TaskListView.ItemsSource = tasks;
                    foreach (var task in tasks)
                    {
                        task.Unpack();
                    }
                }

                
            }
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
