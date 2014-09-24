using System;
using System.Windows;
using System.Windows.Controls;
using Dynamo.Nodes.Search;
using Dynamo.Search.SearchElements;

namespace Dynamo.Controls
{
    public class SubclassesTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SubclassesTemplate { get; set; }
        public DataTemplate ClassDetailsTemplate { get; set; }
        public DataTemplate NestedClassesTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ClassInformation)
                return ClassDetailsTemplate;

            if (item is BrowserRootElement)
            {
                if (ConsistOfNestedClasses(item as BrowserRootElement))
                    return NestedClassesTemplate;
                return SubclassesTemplate;
            }

            const string message = "Unknown object bound to collection";
            throw new InvalidOperationException(message);
        }

        private bool ConsistOfNestedClasses(BrowserRootElement rootElement)
        {
            // Go deeper in item for 2 levels. 1st level - class button,
            // 2nd level - class member.
            // E.g. 1st lvl - Color, 2nd lvl - Red.
            // But for nested classes 2nd lvl will be BrowserInternalElement,
            // That's how we know whether it is nested structure or not.
            foreach (var classItem in rootElement.Items)
            {
                foreach (var classMember in classItem.Items)
                {
                    if (classMember is SearchElementBase) continue;
                    else return true;
                }
            }

            return false;
        }
    }
}
