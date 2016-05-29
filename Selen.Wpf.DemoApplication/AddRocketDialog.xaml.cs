using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Selen.Wpf.GenericAppTree;
using Selen.Wpf.GenericAppTree.Contracts;
using Image = System.Drawing.Image;

namespace Selen.Wpf.DemoApplication
{
    /// <summary>
    /// Interaction logic for AddRocketDialog.xaml
    /// </summary>
    public partial class AddRocketDialog
    {
        public AddRocketDialog()
        {
            InitializeComponent();

            var bmp = (Bitmap)Image.FromFile(Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).FullName, @"Images\standard.png"));
            DataContext = new AppTreeGenerator().Generate(new List<IAppTreeDataProvider>()
            {
                new Test("Rockets", "ADR 13 (antiDest)", bmp),
                new Test("Rockets", "Cybercontrolled S30", bmp),
                new Test("Rockets/LongRange", "<< 33Km (antiBorg integrated)", bmp),
                new Test("Rockets/LongRange", "<< 50Km", bmp),
                new Test("Rockets/LongRange", "<< 200Km", bmp),
                new Test("Rockets/ShortRange", "Rapid missile (2.7 sps)", bmp),
                new Test("Rockets/ShortRange", "Tank breaking missile (1.5 sps)", bmp),
                new Test("SpaceCannons/Laser", "5600V standard", bmp),
                new Test("SpaceCannons/Laser", "AK74 - L hand automatic", bmp),
                new Test("SpaceCannons/Laser", "SK81 rapid", bmp),
                new Test("SpaceCannons/Cyborg", "Cyberspace master (long range)", bmp),
                new Test("SpaceCannons/Cyborg", "Head Cannon standard", bmp),
                new Test("Guns", "Glück Auf Gewehre ST 2 (BwDe)", bmp),
                new Test("Guns", "MG 'Finsternis' 4.9T (BwDe)", bmp),
                new Test("Guns", "Gatling Gun 'Echt Erzgebirge' (BwDe)", bmp),
                new Test("Guns", "Star 4", bmp),
                new Test("Guns", "MG 45 acc (12 sps)", bmp),
                new Test("Guns", "MG 45 rapid (30 sps)", bmp)
            }, true);
        }
    }

    public class Test : IAppTreeDataProvider
    {
        public Test(string path, string name, Bitmap icon)
        {
            Name = name;
            Path = path;
            Icon = icon;
        }

        public string Name
        {
            get;
            set;
        }

        public string Path
        {
            get;
            set;
        }

        public Bitmap Icon
        {
            get;
            set;
        }

        public bool CanBeDisplayed
        {
            get 
            {
                return true;
            }
        }
    }
}