using Dynamo.Search.SearchElements;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dynamo.UI.Views
{
    /// <summary>
    /// Interaction logic for LibrarySearchView.xaml
    /// </summary>
    public partial class LibrarySearchView : UserControl
    {
        public LibrarySearchView()
        {
            InitializeComponent();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem == null) return;

            var searchElement = listBoxItem.DataContext as SearchElementBase;
            if (searchElement != null)
            {
                searchElement.Execute();
                e.Handled = true;
            }
        }

        private void OnClassButtonCollapse(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var classButton = sender as ListViewItem;
            if ((classButton == null) || !classButton.IsSelected) return;

            classButton.IsSelected = false;
            e.Handled = true;
        }

        private void OnPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
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
