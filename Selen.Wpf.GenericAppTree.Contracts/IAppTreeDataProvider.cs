using System.Drawing;

namespace Selen.Wpf.GenericAppTree.Contracts
{
    public interface IAppTreeDataProvider
    {
        string Name { get; }
        string Path { get; }
        Bitmap Icon { get; }

        bool CanBeDisplayed { get; }
    }
}