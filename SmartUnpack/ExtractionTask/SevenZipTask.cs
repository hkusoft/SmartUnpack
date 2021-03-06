﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using SevenZip;
using SmartTaskLib;

namespace SmartUnpack.ExtractionTask
{
    class SevenZipTask : TaskBase {
        
        public SevenZipTask(List<string> paths, int passwordIndex) : base(paths, passwordIndex)
        {
        }

        protected override void UnpackImpl()
        {
            var firstFile = InputFilePaths.FirstOrDefault(); //*.rar or *.r01
            if (firstFile == null)
                return;

            try
            {

                var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    Environment.Is64BitProcess ? "x64" : "x86", "7z.dll");
                SevenZip.SevenZipBase.SetLibraryPath(path);

                int index = PasswordIndex;
                SevenZipExtractor extractor;
                if (index == -1)
                {
                    extractor = new SevenZipExtractor(firstFile);
                }
                else
                {
                    var password = Properties.Settings.Default.ArchivePasswords[index];
                    extractor = new SevenZipExtractor(firstFile, password);
                }
                
                extractor.FileExtractionStarted += OnFileExtractionStarted;
                extractor.Extracting += OnExtracting;
                extractor.FileExtractionFinished += OnFileExtractionFinished;
                extractor.ExtractionFinished += OnExtractionFinished;
                extractor.BeginExtractArchive(TargetExtractionFolder);
            }
            catch (Exception ex)
            {
                CurrentProgressDescription = ex.Message;
            }

        }

        private void OnExtractionFinished(object sender, EventArgs e)
        {
            SevenZipExtractor extractor = sender as SevenZipExtractor;
            InputFilePaths.AddRange(extractor.VolumeFileNames); // *.r01, *.r02, which will be deleted in clean up

            if (OverallProgress == 100)
            {
                SingleFileUnpackProgress = 0;
                OverallProgress = 0;
                CurrentProgressDescription = "Success!";
                OnUnpackFinished(true);
                CleanUp();
            }
                
        }


        private void OnFileExtractionStarted(object sender, FileInfoEventArgs e)
        {
            var extractedFile = Path.GetFileName(e.FileInfo.FileName);
            CurrentProgressDescription = $"--> Extracting {extractedFile}";
            Console.WriteLine(CurrentProgressDescription);
            SingleFileUnpackProgress = 0;
        }

        private void OnExtracting(object sender, ProgressEventArgs e)
        {
            SingleFileUnpackProgress = e.PercentDone;
        }

        private void OnFileExtractionFinished(object sender, FileInfoEventArgs e)
        {
            SingleFileUnpackProgress = Convert.ToInt32(100);
            var extractor = sender as SevenZipExtractor;
            if (extractor != null)
            {
                long totalSize = extractor.UnpackedSize;
                bytesUnpacked += e.FileInfo.Size;
                OverallProgress = Convert.ToInt32(bytesUnpacked * 100 / (ulong) totalSize);
            }
        }

    }
}
