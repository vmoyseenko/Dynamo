using System;
using System.Windows;
using System.Windows.Controls;
using Dynamo.Nodes.Search;
using Dynamo.Search.SearchElements;

namespace Dynamo.Controls
{
    public class ClassObjectTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ClassObjectTemplate { get; set; }
        public DataTemplate ClassDetailsTemplate { get; set; }
        public DataTemplate NestedClassesTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is ClassInformation)
                return ClassDetailsTemplate;

            if (item is BrowserInternalElement)
            {
                if (ConsistOfNestedClasses(item as BrowserInternalElement)) return NestedClassesTemplate;
                return ClassObjectTemplate;
            }

            const string message = "Unknown object bound to collection";
            throw new InvalidOperationException(message);
        }

        private bool ConsistOfNestedClasses(BrowserInternalElement rootElement)
        {
            // Go deeper in item for 2 levels. 1st level - class button,
            // 2nd level - class member.
            // E.g. 1st lvl - Color, 2nd lvl - Red.
            // But for nested classes 2nd lvl will be BrowserInternalElement,
            // That's how we know whether it is nested structure or not.
            foreach (BrowserInternalElement classItem in rootElement.Items)
                if (classItem is SearchElementBase) continue;
                    else return true;

            return false;
        }
    }
}
