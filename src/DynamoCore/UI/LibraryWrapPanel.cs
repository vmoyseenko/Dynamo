﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Dynamo.Nodes.Search;
using Dynamo.UI.Controls;
using Dynamo.Utilities;
using System.Windows.Input;
using Dynamo.Search.SearchElements;

namespace Dynamo.Controls
{
    public class LibraryWrapPanel : WrapPanel
    {
        /// <summary>
        /// Field specifies the prospective index of selected class in collection.
        /// </summary>
        private int selectedClassProspectiveIndex = -1;
        private double classObjectWidth = double.NaN;
        private ObservableCollection<BrowserItem> collection;
        private BrowserInternalElement currentClass;

        // For the first time we set current index to -2, because "-1" is used,
        // when class button was clicked second time. And class button can also have "0" index.
        // So, let's use "-2" as start value for currentIndex.
        private int currentIndex = -2;

        protected override void OnInitialized(EventArgs e)
        {
            // ListView should never be null.
            var classListView = WPF.FindUpVisualTree<ListView>(this);
            collection = classListView.ItemsSource as ObservableCollection<BrowserItem>;
            collection.Add(new ClassInformation());
            classListView.SelectionChanged += OnClassViewSelectionChanged;
            this.KeyDown += OnLibraryWrapPanelKeyDown;

            base.OnInitialized(e);
        }

        private void OnLibraryWrapPanelKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var classButton = Keyboard.FocusedElement as ListBoxItem;

            // Enter collapses and expands class button.
            if (e.Key == Key.Enter)
            {
                classButton.IsSelected = !classButton.IsSelected;
                e.Handled = true;
                return;
            }

            var buttonsWrapPanel = sender as LibraryWrapPanel;
            var listButtons = buttonsWrapPanel.Children;

            // If focused element is NodeSearchElement, that means focused element is inside expanded class.
            // If user presses Up, we have to move back to selected class.
            if ((classButton.DataContext is NodeSearchElement) && (e.Key == Key.Up))
            {
                var selectedClassButton= listButtons.OfType<ListViewItem>().
                    Where(button => button.IsSelected).FirstOrDefault();
                if (selectedClassButton != null) selectedClassButton.Focus();
                e.Handled = true;
                return;
            }

            // If class is selected, we should move down to ClassDetails.
            if ((e.Key == Key.Down) && classButton.IsSelected)
            {
                int classInfoIndex = GetClassInformationIndex();
                var standardPanel = listButtons[classInfoIndex];
                var firstMemberList = WPF.FindChild<ListBox>(standardPanel,"primaryMembers");
                var generator = firstMemberList.ItemContainerGenerator;
                (generator.ContainerFromIndex(0) as ListBoxItem).Focus();
                
                e.Handled = true;
                return;
            }

            var selectedIndex = listButtons.IndexOf(classButton);
            int itemsPerRow = (int)Math.Floor(buttonsWrapPanel.ActualWidth / classButton.ActualWidth);

            int newIndex = GetIndexNextSelectedItem(e.Key, selectedIndex, itemsPerRow);

            // If index is out of range class list, that means we have to move to previous category
            // or to next member group.
            if ((newIndex < 0) || (newIndex > listButtons.Count))
            {
                e.Handled = false;
                return;
            }

            // Set focus on new item.
            listButtons[newIndex].Focus();

            e.Handled = true;
            return;
        }

        private int GetIndexNextSelectedItem(Key key, int selectedIndex, int itemsPerRow)
        {
            int newIndex = -1;
            int selectedRowIndex = selectedIndex / itemsPerRow + 1;

            switch (key)
            {
                case Key.Right:
                    {
                        newIndex = selectedIndex + 1;
                        int availableIndex = selectedRowIndex * itemsPerRow - 1;
                        if (newIndex > availableIndex) newIndex = selectedIndex;
                        break;
                    }
                case Key.Left:
                    {
                        newIndex = selectedIndex - 1;
                        int availableIndex = (selectedRowIndex - 1) * itemsPerRow;
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

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo.WidthChanged) // Only reorder when width changes.
                OrderListItems();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.Children == null || this.Children.Count == 0)
                return finalSize;

            // Find out class object size.
            // First item is always class object.
            if (double.IsNaN(classObjectWidth))
            {
                // Make sure our assumption of the first child being a 
                // StandardPanel still holds.
                var firstChild = this.Children[0];
                if (firstChild is StandardPanel)
                {
                    // If the following exception is thrown, please look at "LibraryWrapPanel.cs"
                    // where we insert both "BrowserItem" and "ClassInformation" items, ensure that
                    // the "ClassInformation" item is inserted last.
                    throw new InvalidOperationException("firstChild is StandardPanel. " +
                        "firstChild Type should be derived from BrowserItem");
                }

                classObjectWidth = firstChild.DesiredSize.Width;
            }

            double x = 0, y = 0, currentRowHeight = 0;

            int itemsPerRow = (int)Math.Floor(finalSize.Width / classObjectWidth);
            double sizeBetweenItems = (finalSize.Width - itemsPerRow*classObjectWidth) / (itemsPerRow+1);


            foreach (UIElement child in this.Children)
            {
                var desiredSize = child.DesiredSize;
                if ((x + desiredSize.Width) > finalSize.Width)
                {
                    x = 0;
                    y = y + currentRowHeight;
                    currentRowHeight = 0;
                }

                if ((child as FrameworkElement).DataContext is ClassInformation)
                //Then it's Standard panel, we do not need margin it.
                {
                    child.Arrange(new Rect(x, y, desiredSize.Width, desiredSize.Height));
                    x = x + desiredSize.Width;
                }
                else
                {
                    child.Arrange(new Rect(x + sizeBetweenItems, y, desiredSize.Width, desiredSize.Height));
                    x = x + desiredSize.Width + sizeBetweenItems;
                }
                if (desiredSize.Height > currentRowHeight)
                    currentRowHeight = desiredSize.Height;
            }

            return finalSize;
        }

        protected override bool HasLogicalOrientation
        {
            get { return false; } // Arrange items in two dimension.
        }

        private void OnClassViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var index = ((sender as ListView).SelectedIndex);

            // Every time, when user moves inside class details, class button selects.
            // As result class details repaints.
            // So, we don't need to repaint it everytime, that's why we use cached index.
            // If cached index equals current one, then we don't have to do anything.
            if (currentIndex != index) currentIndex = index;
            else return;

            int classInfoIndex = GetClassInformationIndex();

            // If user clicks on the same item when it is expanded, then 'OnClassButtonCollapse'
            // is invoked to deselect the item. This causes 'OnClassViewSelectionChanged' to be 
            // called again, with 'SelectedIndex' set to '-1', indicating that no item is selected,
            // in which case we need to hide the standard panel.
            if (index == -1)
            {
                if (classInfoIndex != -1)
                    (collection[classInfoIndex] as ClassInformation).ClassDetailsVisibility = false;
                OrderListItems();
                return;
            }
            else
                (collection[classInfoIndex] as ClassInformation).ClassDetailsVisibility = true;

            selectedClassProspectiveIndex = TranslateSelectionIndex(index);
            currentClass = collection[index] as BrowserInternalElement;
            OrderListItems(); // Selection change, we may need to reorder items.
        }

        /// <summary>
        /// Function counts prospective index of selected class in collection.
        /// In case if index of selected class bigger then index of ClassInformation
        /// object it should be decreased by 1 because at next stages ClassInformation
        /// will free occupied index.
        /// </summary>
        /// <param name="selection"></param>
        /// <returns></returns>
        private int TranslateSelectionIndex(int selection)
        {
            if (selection < GetClassInformationIndex())
                return selection;

            return selection - 1;
        }

        private int GetClassInformationIndex()
        {
            var query = collection.Select(c => c).Where(c => c is ClassInformation);
            var classObjectBase = query.ElementAt(0);
            return collection.IndexOf(classObjectBase);
        }

        private void OrderListItems()
        {
            if (double.IsNaN(this.ActualWidth))
                return;
            if (collection == null || (collection.Count <= 1) || currentClass == null)
                return;

            // Find out where ClassInformation object is positioned in collection.
            var currentClassInformationIndex = GetClassInformationIndex();
            var classObjectBase = collection[currentClassInformationIndex];

            // If there is no selection, then mark the StandardPanel as hidden.
            var classInformation = classObjectBase as ClassInformation;
            if (classInformation != null && (selectedClassProspectiveIndex == -1))
                return;

            //Add members of selected class to StandardPanel            
            classInformation.PopulateMemberCollections(currentClass as BrowserInternalElement);

            // When we know the number of items on a single row, through selected 
            // item index we will find out where the expanded StandardPanel sit.
            var itemsPerRow = ((int)Math.Floor(ActualWidth / classObjectWidth));
            var d = ((double)selectedClassProspectiveIndex) / itemsPerRow;
            var selectedItemRow = ((int)Math.Floor(d));

            // Calculate the correct index where ClassInformation object should go 
            // in the collection. If the selected item is on the first row (i.e. 
            // row #0), then the ClassInformation object should be at the index 
            // 'itemsPerRow'. Similarly, if the selected item is on row #N, then
            // ClassInformation object should be at the index '(N + 1) * itemsPerRow'.
            var correctClassInformationIndex = ((selectedItemRow + 1) * itemsPerRow);

            // But correctClassInformationIndex must be less than collection.Count.
            if (correctClassInformationIndex >= collection.Count)
                correctClassInformationIndex = collection.Count - 1;

            // We need to move the ClassInformation object to the right index.            
            collection.RemoveAt(currentClassInformationIndex);
            collection.Insert(correctClassInformationIndex, classInformation);
        }
    }
}
