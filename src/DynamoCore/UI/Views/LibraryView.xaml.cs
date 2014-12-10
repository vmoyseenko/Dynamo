﻿using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Dynamo.Controls;
using Dynamo.Nodes.Search;
using Dynamo.Search.SearchElements;
using Dynamo.Utilities;

namespace Dynamo.UI.Views
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : UserControl
    {
        // This property is used to prevent a bug.
        // When user clicks on category it scrolls instead of category content expanding.
        // The reason is "CategoryTreeView" does not show full content because it is too
        // big. On category clicking WPF makes autoscroll and doesn't expand content of 
        // category. We are counting count of calling BringIntoViewCount() functions.        
        private int bringIntoViewCount;
        private int BringIntoViewCount
        {
            get
            {
                return bringIntoViewCount;
            }
            set
            {
                bringIntoViewCount = value >= 2 ? 2 : value;
            }
        }

        public LibraryView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// When user tries to use mouse wheel there can be several cases.
        /// </summary>
        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 1 case. 
            // User presses Shift and uses mouse wheel. That means user tries to 
            // scroll horizontally to the right side, to see the whole long name of node.
            // So, we go further, in scrollbar, that is under this one, that's why Handled is false.
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                e.Handled = false;
                return;
            }

            // 2 case.
            // User uses just mouse wheel. That means user wants to scroll down LibraryView.
            // So, we just change VerticalOffset, and there is no need to go further and change something.
            // Set Handled to true.
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void OnClassButtonCollapse(object sender, MouseButtonEventArgs e)
        {
            var classButton = sender as ListViewItem;
            if ((classButton == null) || !classButton.IsSelected) return;

            classButton.IsSelected = false;
            e.Handled = true;
        }

        /// When a category is collapsed, the selection of underlying sub-category 
        /// list is cleared. As a result any visible StandardPanel will be hidden.
        private void OnExpanderCollapsed(object sender, System.Windows.RoutedEventArgs e)
        {
            BringIntoViewCount++;
            var expanderContent = (sender as FrameworkElement);
            expanderContent.BringIntoView(new Rect(0.0, 0.0, 100.0, 20.0));

            var buttons = Dynamo.Utilities.WPF.FindChild<ListView>(expanderContent, "");
            if (buttons != null)
                buttons.UnselectAll();

            e.Handled = true;
        }

        private void OnSubCategoryListViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

        private void OnMemberMouseEnter(object sender, MouseEventArgs e)
        {
            FrameworkElement fromSender = sender as FrameworkElement;
            libraryToolTipPopup.PlacementTarget = fromSender;
            libraryToolTipPopup.SetDataContext(fromSender.DataContext);
        }

        private void OnPopupMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            libraryToolTipPopup.SetDataContext(null);
        }

        private void OnTreeViewItemPreviewMouseLeftButton(object sender, MouseButtonEventArgs e)
        {
            var categoryButton = sender as TreeViewItem;
            if (!(categoryButton.DataContext is BrowserRootElement))
                return;

            var wrapPanels = new List<LibraryWrapPanel>();
            WPF.FindChildren<LibraryWrapPanel>(categoryButton, string.Empty, wrapPanels);
            if (wrapPanels.Count == 0)
                return;

            var selectedElement = e.OriginalSource as FrameworkElement;
            var selectedClass = selectedElement.DataContext as BrowserInternalElement;
            // Continue work with real class: not null, child of BrowserInternalElementForClasses.
            if (selectedClass == null || selectedClass is NodeSearchElement ||
                !(selectedClass.Parent is BrowserInternalElementForClasses))
                return;

            // Go through all available for current top category LibraryWrapPanel.
            // Select class if wrapPanel contains selectedClass.
            // Unselect class in other case.
            foreach (var wrapPanel in wrapPanels)
            {
                if (wrapPanel.MakeOrClearSelection(selectedClass))
                {
                    e.Handled = true;
                    selectedElement.BringIntoView();
                }
            }
        }

        private void OnRequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            // Because of bug we mark event as handled for all automatic requests 
            // until count of our requests less than 1. First request is done for
            // opened top category when dynamo starts.
            e.Handled = BringIntoViewCount < 2;
        }

        private void OnAddButtonPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var button = sender as Button;
                var contextMenu = button.ContextMenu;
                contextMenu.Placement = PlacementMode.Bottom;
                contextMenu.PlacementTarget = button;
                contextMenu.IsOpen = true;
            }

            e.Handled = true;
        }
    }
}
