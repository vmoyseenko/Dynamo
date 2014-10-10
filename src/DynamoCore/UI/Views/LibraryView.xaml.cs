using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dynamo.UI.Views
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : UserControl
    {
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

        private void OnEditClick(object sender, RoutedEventArgs e)
        {
            // Logic of original TreeView should be saved until
            // new design is not implemented.
#if false
            var menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                var element = menuItem.DataContext as CustomNodeSearchElement;
                if (element != null)
                {
                    if (dynamoViewModel.OpenCommand.CanExecute(element.Path))
                        dynamoViewModel.OpenCommand.Execute(element.Path);
                }
            }
#endif
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
            var expanderContent = (sender as FrameworkElement);
            var buttons = Dynamo.Utilities.WPF.FindChild<ListView>(expanderContent, "");
            if (buttons != null)
                buttons.UnselectAll();
        }

        // Here we can move left, right, up, down.
        private void OnClassButtonKeyDown(object sender, KeyEventArgs e)
        {
            var classButton = sender as ListViewItem;
            var listViewButtons = Dynamo.Utilities.WPF.FindUpVisualTree<ListView>(classButton);
            var selectedIndex = listViewButtons.SelectedIndex;
            int itemsPerRow = (int)Math.Floor(listViewButtons.ActualWidth / classButton.ActualWidth);

            int newIndex = GetIndexNextSelectedItem(e.Key, selectedIndex, itemsPerRow);

            if ((newIndex >= 0) && (newIndex < listViewButtons.Items.Count))
                listViewButtons.SelectedIndex = newIndex;

            // Set focus on new selected item.
            var item = listViewButtons.ItemContainerGenerator.ContainerFromIndex(listViewButtons.SelectedIndex) as ListViewItem;
            item.Focus();

            e.Handled = true;
            return;
        }

        private int GetIndexNextSelectedItem(Key key, int selectedIndex, int itemsPerRow)
        {
            int newIndex = -1;
            int numberOfSelectedRow = selectedIndex / itemsPerRow + 1;

            switch (key)
            {
                case Key.Right:
                    {
                        newIndex = selectedIndex + 1;
                        int availableIndex = numberOfSelectedRow * itemsPerRow - 1;
                        if (newIndex > availableIndex) newIndex = selectedIndex;
                        break;
                    }
                case Key.Left:
                    {
                        newIndex = selectedIndex - 1;
                        int availableIndex = (numberOfSelectedRow - 1) * itemsPerRow;
                        if (newIndex < availableIndex) newIndex = selectedIndex;
                        break;
                    }
                case Key.Down:
                    {
                        newIndex = selectedIndex + itemsPerRow + 1;
                        // +1 because one of items is always ClassInformation.
                        break;
                    }
                case Key.Up:
                    {
                        newIndex = selectedIndex - itemsPerRow;
                        break;
                    }
            }
            return newIndex;
        }

    }
}
