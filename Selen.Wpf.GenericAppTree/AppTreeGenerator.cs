using System.Collections.Generic;
using System.Linq;
using Selen.Wpf.GenericAppTree.Contracts;

namespace Selen.Wpf.GenericAppTree
{
    public class AppTreeGenerator
    {
        public AppTreeContainer Generate(IEnumerable<IAppTreeDataProvider> dataProviders)
        {
            return Generate(dataProviders, false);
        }

        public AppTreeContainer Generate(IEnumerable<IAppTreeDataProvider> dataProviders, bool enablePreview)
        {
            var root = new AppTreeContainer("root", "root");

            foreach (var item in dataProviders.Where(el => el.CanBeDisplayed))
            {
                var path = item.Path.Split('/');
                var lastContainer = root;

                int i;
                for (i = 0; i < path.Length; i++)
                {
                    var container = lastContainer.ContainerChildren.FirstOrDefault(el => el.Header == path[i]);
                    if (container == null)
                    {
                        container = new AppTreeContainer(i.ToString(), path[i], enablePreview);
                        lastContainer.AddChild(container);
                    }

                    lastContainer = container;
                }

                lastContainer.AddChild(new AppTreeItem(i.ToString(), item.Name, item, item.Icon));
            }

            return root;
        }
    }
}