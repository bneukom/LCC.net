using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;

namespace Selen.Wpf.GenericAppTree
{
    public class AppTreeContainer : AppTreeNode
    {
        private readonly ObservableCollection<AppTreeNode> _children;
        private readonly bool _enablePreview;

        private AppTreeNode _previewChild;

        public AppTreeContainer(string selectionGroup, string header, bool enablePreview = false)
            : base(selectionGroup, header)
        {
            _children = new ObservableCollection<AppTreeNode>();
            _enablePreview = enablePreview;

            if(_enablePreview)
            {
                PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName != "IsSelected") return;
                    
                    if(!IsSelected)
                    {
                        foreach (var item in Children)
                        {
                            item.IsSelected = false;
                        }
                    }

                    UpdatePreview();
                };
            }
        }

        public IEnumerable<AppTreeContainer> ContainerChildren
        {
            get
            {
                return _children.OfType<AppTreeContainer>();
            }
        }

        public IEnumerable<AppTreeItem> ItemChildren
        {
            get
            {
                return _children.OfType<AppTreeItem>();
            }
        }

        public IEnumerable<AppTreeNode> Children
        {
            get
            {
                return _children;
            }
        }

        public void AddChild(AppTreeNode item)
        {
            item.SelectionChanged += OnSelectionChanged;
            item.Parent = this;
            _children.Add(item);

            if (_enablePreview) UpdatePreview();
        }

        public bool RemoveChild(AppTreeNode item)
        {
            item.SelectionChanged -= OnSelectionChanged; 
            item.Parent = null;
            var result = _children.Remove(item);

            if (_enablePreview) UpdatePreview();
            return result;
        }

        private void UpdatePreview()
        {
            if (_previewChild != null) _previewChild.PropertyChanged -= PreviewPropertyChanged;
            
            if(_children.Count == 0)
            {
                Icon = null;
                OnPropertyChanged("Icon");

                return;
            }

            _previewChild = _children.First();
            _previewChild.PropertyChanged += PreviewPropertyChanged;

            if (IsSelected)
            {
                Icon = null;
                OnPropertyChanged("Icon");
            }
            else
            {
                Icon = _previewChild.Icon;
                OnPropertyChanged("Icon");
            }
        }

        private void PreviewPropertyChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Icon") UpdatePreview();
        }
    }
}