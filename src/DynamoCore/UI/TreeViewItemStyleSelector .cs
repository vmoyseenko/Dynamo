using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Dynamo.UI
{
    class TreeViewItemStyleSelector : StyleSelector
    {
        public Style TreeViewItemStyle1 { get; set; }
        public Style TreeViewItemStyle2 { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            return TreeViewItemStyle2;
        }
    }
}
