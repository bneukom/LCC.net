using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LandscapeClassifier.Annotations;
using LandscapeClassifier.Model;

namespace LandscapeClassifier.ViewModel.Dialogs
{
    public class AddLayerDialogViewModel : INotifyPropertyChanged
    {
        private double _minCutOff = 2;
        private double _maxCutOff = 98;


        /// <summary>
        /// Bands
        /// </summary>
        public ObservableCollection<LayerInfo> Layers { get; set; }

        /// <summary>
        /// Min cut off for band enhancement
        /// </summary>
        public double MinCutOff
        {
            get { return _minCutOff; }
            set { _minCutOff = value; OnPropertyChanged(nameof(MinCutOff)); }
        }

        /// <summary>
        /// Max cut off for band enhancement.
        /// </summary>
        public double MaxCutOff
        {
            get { return _maxCutOff; }
            set { _maxCutOff = value; OnPropertyChanged(nameof(MaxCutOff)); }
        }

        public AddLayerDialogViewModel()
        {
            Layers = new ObservableCollection<LayerInfo>();

        }


        /// <summary>
        /// Resets this viewmodel to its default values.
        /// </summary>
        public void Reset()
        {
            Layers.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LayerInfo : INotifyPropertyChanged
    {
        public string Path { get; set; }

        private bool _contrastEnhancement;
        public bool ContrastEnhancement
        {
            get { return _contrastEnhancement; }
            set { _contrastEnhancement = value; OnPropertyChanged(nameof(ContrastEnhancement)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public LayerInfo(string path, bool contrastEnhancement)
        {
            Path = path;
            ContrastEnhancement = contrastEnhancement;
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


    }
}
