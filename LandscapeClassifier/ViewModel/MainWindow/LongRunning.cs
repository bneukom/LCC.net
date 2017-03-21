using System;
using GalaSoft.MvvmLight;

namespace LandscapeClassifier.ViewModel.MainWindow
{
    public abstract class LongRunning : ViewModelBase
    {
        protected readonly Progress<double> ProgressReporter = new Progress<double>();
        private double _progress;
        private string _taskName;

        public double Progress
        {
            get { return _progress; }
            set { _progress = value; RaisePropertyChanged(); }
        }

        public string TaskName
        {
            get { return _taskName; }
            set { _taskName = value; RaisePropertyChanged(); }
        }

        public abstract int MaximumProgress { get; }

        protected LongRunning(string taskName)
        {
            ProgressReporter.ProgressChanged += (sender, i) =>
            {
                Progress = i;
            };
            TaskName = taskName;
        }

        public abstract void Execute();
    }
}
