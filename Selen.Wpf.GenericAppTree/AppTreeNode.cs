using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace Selen.Wpf.GenericAppTree
{
    public abstract class AppTreeNode : INotifyPropertyChanged
    {
        protected string _group;
        private bool _isSelected;

        public AppTreeNode(string selectionGroup, string header)
        {
            _group = selectionGroup;
            Header = header;
        }

        public AppTreeNode(string selectionGroup, string header, Image icon)
        {
            _group = selectionGroup;
            Header = header;

            if (icon == null) return;
            using (var memory = new MemoryStream())
            {
                icon.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                var bmi = new BitmapImage();
                bmi.BeginInit();
                bmi.StreamSource = memory;
                bmi.CacheOption = BitmapCacheOption.OnLoad;
                bmi.EndInit();

                Icon = bmi;
            }
        }

        public ImageSource Icon
        {
            get;
            set;
        } 

        public AppTreeContainer Parent { get; set; }

        public string Header { get; set; }

        public bool IsSelected 
        { 
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                OnPropertyChanged("IsSelected");

                if (value) OnSelectionChanged(this);
            }
        }

        public event Action<AppTreeNode> SelectionChanged;

        public object DataContext { get; set; }
        
        public string Group
        {
            get
            {
                return _group;
            }
        }

        public void Select()
        {
            IsSelected = true;
            OnSelectionChanged(this);

            if (Parent != null) Parent.Select();
        }

        protected void OnSelectionChanged(AppTreeNode sender)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(sender);
            }
        }

        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}