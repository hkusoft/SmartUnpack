﻿using SmartTaskLib;
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
    public partial class MainWindow : Window 
    {
        MainViewModel viewModel;

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
                List<TaskBase> tasks = null;
                int passwordIndex = Array.IndexOf(PasswordButtons, sender);

                  // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(files.Length>0)
                {
                    //Shift Key pressed: 4, Ctrl key: 8
                    // if (((int) e.KeyStates & 8) == 8)
                    // {
                    //     var dir = System.IO.Path.GetDirectoryName(files[0]);
                    //     tasks = Util.ScanDirectory(dir, passwordIndex);                        
                    // }
                    // else
                    {
                        var filePath = files[0];
                        tasks = Util.CreateTaskForFile(filePath, passwordIndex);
                    }
                    Unpack(tasks);
                }

                
            }
        }

        private void Unpack(List<TaskBase> tasks)
        {
            viewModel.TaskList = tasks;
            viewModel.StartUnpack();
            //RefreshDataSource();
        }

        // private void RefreshDataSource()
        // {
        //     Application.Current.Dispatcher.Invoke(new Action(() =>
        //     {
        //         TaskListView.ItemsSource = AllTasks.Values;
        //         TaskListView.Items.Refresh();
        //     }));            
        // }

        


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
