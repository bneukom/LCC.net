using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using LandscapeClassifier.ViewModel.MainWindow;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LandscapeClassifier.Model
{
    public class LandcoverTypeViewModel : ViewModelBase
    {
        private string _name;
        private Color _color;

        public int Id { get; }

        public string Name
        {
            get { return _name; }
            set { _name = value; RaisePropertyChanged(); }
        }

        public Color Color
        {
            get { return _color; }
            set { _color = value; RaisePropertyChanged(); }
        }

        public static LandcoverTypeViewModel None { get; } = new LandcoverTypeViewModel(-1,"None", Colors.Black);

        public static LandcoverTypeViewModel[] DefaultLandcoverTypesViewModel =
        {
            new LandcoverTypeViewModel(0,"Grass", Colors.Green),
            new LandcoverTypeViewModel(1,"Gravel", Colors.LightGray),
            new LandcoverTypeViewModel(2,"Rock", Colors.DarkGray),
            new LandcoverTypeViewModel(3,"Snow", Colors.White),
            new LandcoverTypeViewModel(4,"Tree", Colors.DarkGreen),
            new LandcoverTypeViewModel(5,"Water", Colors.Blue),
            new LandcoverTypeViewModel(6,"Agriculture", Colors.AntiqueWhite),
            new LandcoverTypeViewModel(7,"Settlement", Colors.Black),
            new LandcoverTypeViewModel(8,"Soil", Colors.SaddleBrown),
        };

        public Brush GetBrush()
        {
            return new SolidColorBrush(Color);
        }


        public LandcoverTypeViewModel(int id, string name, Color color)
        {
            Id = id;
            Name = name;
            Color = color;
        }

        public LandcoverTypeViewModel(LandcoverTypeViewModel other)
        {
            Id = other.Id;
            Name = other.Name;
            Color = other.Color;
        }


        public override string ToString()
        {
            return _name;
        }

        protected bool Equals(LandcoverTypeViewModel other)
        {
            return Id == other.Id;
        }


    }
}
