﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Dynamo.Search.SearchElements;
using Dynamo.ViewModels;
using Dynamo.Search;
using Dynamo.Utilities;
using Dynamo.Nodes.Search;
using Dynamo.Controls;
using System.Linq;

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

        #region MethodButton

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

            // TODO: this focus setter should be removed in future.
            // This item may get focus just by keyboard.
            listBoxItem.Focus();
        }

        #endregion

        #region ClassButton

        private void OnClassButtonCollapse(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var classButton = sender as ListViewItem;
            if ((classButton == null) || !classButton.IsSelected) return;

            classButton.IsSelected = false;
            e.Handled = true;
        }

        private void OnClassButtonGotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            ListViewItem listViewItem = e.OriginalSource as ListViewItem;

            // We select class only, when it was selected!
            // But not, when it got focus.
            if (listViewItem != null && listViewItem.IsSelected)
                return;

            e.Handled = true;
        }

        #endregion

        private void OnPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void OnNoMatchFoundButtonClick(object sender, RoutedEventArgs e)
        {
            var searchViewModel = this.DataContext as SearchViewModel;

            // Clear SearchText in ViewModel, as result search textbox clears as well.
            searchViewModel.SearchText = "";
        }

        #region ToolTip methods

        private void OnListBoxItemMouseEnter(object sender, MouseEventArgs e)
        {
            ListBoxItem fromSender = sender as ListBoxItem;
            libraryToolTipPopup.PlacementTarget = fromSender;
            libraryToolTipPopup.SetDataContext(fromSender.DataContext);
        }

        private void OnPopupMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            libraryToolTipPopup.SetDataContext(null);
        }

        #endregion

        #region Key navigation

        // Key navigation functions as bubbling scheme:
        // Category(CategoryListView) <- CategoryContent(StackPanel) <- 
        // <- SubClasses(SubCategoryListView) OR MemberGroups(MemberGroupsListBox)
        // When element can't move further, it notifies its' parent about that.
        // And then parent decides what to do with it.


        // This event is raised only, when we can't go down, to next member.
        // I.e. we are now at the last member button and we have to move to next member group.
        private void MemberGroupsKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key != Key.Down) && (e.Key != Key.Up))
                return;

            var memberInFocus = (Keyboard.FocusedElement as ListBoxItem).Content as BrowserInternalElement;
            var memberGroups = (sender as ListBox).Items;
            var memberGroupListBox = sender as ListBox;

            int focusedMemberGroupIndex = 0;

            // Find out to which memberGroup focused member belong.
            for (int i = 0; i < memberGroups.Count; i++)
            {
                var memberGroup = memberGroups[i] as SearchMemberGroup;
                if (memberGroup.ContainsMember(memberInFocus))
                {
                    focusedMemberGroupIndex = i;
                    break;
                }
            }

            int nextFocusedMemberGroupIndex = focusedMemberGroupIndex;
            // If user presses down, then we need to set focus to the next member group.
            // Otherwise to previous.
            if (e.Key == Key.Down)
                nextFocusedMemberGroupIndex++;
            if (e.Key == Key.Up)
                nextFocusedMemberGroupIndex--;

            // The member group list box does not attempt to process the key event if it 
            // has moved beyond its available list of member groups. In this case, the 
            // key event is considered not handled and will be left to the parent visual 
            // (e.g. class button or another category) to handle.
            e.Handled = false;
            if (nextFocusedMemberGroupIndex < 0 || nextFocusedMemberGroupIndex > memberGroups.Count - 1)
                return;

            var item = GetListItemByIndex(memberGroupListBox, nextFocusedMemberGroupIndex);
            var nextFocusedMembers = WPF.FindChild<ListBox>(item, "MembersListBox");

            // When moving on to the next member group list below (by pressing down arrow),
            // the focus should moved on to the first member in the member group list. Likewise,
            // when moving to the previous member group list above, the focus should be set on 
            // the last member in that list.
            var itemIndex = 0;
            if (e.Key == Key.Up)
                itemIndex = nextFocusedMembers.Items.Count - 1;

            GetListItemByIndex(nextFocusedMembers, itemIndex).Focus();

            e.Handled = true;
        }

        // The 'StackPanel' that contains 'SubCategoryListView' and 'MemberGroupsListBox'
        // handles this message. If none of these two list boxes are handling the key 
        // message, that means the currently focused list box item is the first/last item
        // in these two list boxes. When key message arrives here, it is then the 'StackPanel'
        // responsibility to move the focus on to the adjacent list box.
        private void OnCategoryContentKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key != Key.Down) && (e.Key != Key.Up))
                return;

            // Member in focus(in this scenario) can be only first/last member button or first/last class button.
            var memberInFocus = Keyboard.FocusedElement as FrameworkElement;
            var searchCategoryElement = sender as FrameworkElement;

            // memberInFocus is method button.
            if (memberInFocus.DataContext is NodeSearchElement)
            {
                var searchCategoryContent = searchCategoryElement.DataContext as SearchCategory;

                // Gotten here because of last method being listed, pressing 'down' array cannot 
                // move down further. Return here to allow higher level visual element to handle
                // the navigation (to a separate category).
                if (e.Key == Key.Down)
                    return;

                // Otherwise, pressed Key is Up.

                // No class is found in this 'SearchCategory', return from here so that higher level
                // element gets to handle the navigational keys to move focus to the previous category.
                if (searchCategoryContent.Classes.Count == 0)
                    return;

                // Otherwise, we move to first class button.
                var listItem = FindFirstChildListItem(searchCategoryElement, "SubCategoryListView");
                if (listItem != null)
                    listItem.Focus();

                e.Handled = true;
                return;
            }

            // memberInFocus is class button.
            if (memberInFocus.DataContext is BrowserInternalElement)
            {
                // We are at the first row of class list. User presses up, we have to move to previous category.
                // We handle it further.
                if (e.Key == Key.Up)
                    return;

                // Otherwise user pressed down, we have to move to first member button.
                var memberGroupsListBox = WPF.FindChild<ListBox>(searchCategoryElement, "MemberGroupsListBox");
                var listItem = FindFirstChildListItem(memberGroupsListBox, "MembersListBox");
                if (listItem != null)
                    listItem.Focus();

                e.Handled = true;
                return;
            }
        }

        private ListBoxItem FindFirstChildListItem(FrameworkElement parent, string listName)
        {
            var list = WPF.FindChild<ListBox>(parent, listName);
            var generator = list.ItemContainerGenerator;
            return generator.ContainerFromIndex(0) as ListBoxItem;
        }

        /// <summary>
        /// 'CategoryListView' element contains the following child elements (either 
        /// directly, or indirectly nested): 'StackPanel', 'SubCategoryListView',
        /// 'MemberGroupsListBox' and 'MembersListBox'. If none of these child elements 
        /// choose to process the key event, it gets bubbled up here. This typically 
        /// happens for the following scenarios:
        /// 
        /// 1. Down key is pressed when selection is on last entry of 'MembersListBox'
        /// 2. Up key is pressed when selection is on item on first row of 'SubCategoryListView'
        /// 3. Up key is pressed when selection is on the first entry of 'MembersListBox'
        ///    and there are no classes.
        /// 
        /// </summary>
        private void OnCategoryKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key != Key.Down) && (e.Key != Key.Up))
                return;

            // Member in focus(in this scenario) can be only first/last member button or class button at the first row.
            var memberInFocus = Keyboard.FocusedElement as FrameworkElement;
            var memberInFocusContext = memberInFocus.DataContext as BrowserInternalElement;
            var categoryListView = sender as ListView;

            int categoryIndex = 0;
            for (int i = 0; i < categoryListView.Items.Count; i++)
            {
                var category = categoryListView.Items[i] as SearchCategory;
                if (category.ContainsClassOrMember(memberInFocusContext))
                {
                    categoryIndex = i;
                    break;
                }
            }

            if (e.Key == Key.Down)
                categoryIndex++;
            if (e.Key == Key.Up)
                categoryIndex--;

            // The selection cannot be moved further up, returning here without handling the key event 
            // so that parent visual element gets to handle it and move selection up to 'Top Result' list.
            if (categoryIndex < 0) return;
            // We are at the last member and there is no way to move down.
            if (categoryIndex >= categoryListView.Items.Count)
            {
                e.Handled = true;
                return;
            }

            var nextFocusedCategory = GetListItemByIndex(categoryListView, categoryIndex);
            var nextFocusedCategoryContent = nextFocusedCategory.Content as SearchCategory;

            if (e.Key == Key.Up)
            {
                var memberGroupsList = WPF.FindChild<ListBox>(nextFocusedCategory, "MemberGroupsListBox");
                var lastMemberGroup = GetListItemByIndex(memberGroupsList, memberGroupsList.Items.Count - 1);
                var membersList = WPF.FindChild<ListBox>(lastMemberGroup, "MembersListBox");

                // If key is up, then we have to select the last method button.
                GetListItemByIndex(membersList, membersList.Items.Count - 1).Focus();
            }
            else // Otherwise, Down was pressed, and we have to select first class/method button.
            {
                if (nextFocusedCategoryContent.Classes.Count > 0)
                {
                    // If classes are presented, then focus on first class.
                    FindFirstChildListItem(nextFocusedCategory, "SubCategoryListView").Focus();
                }
                else
                {
                    // If there are no classes, then focus on first method.
                    var memberGroupsList = FindFirstChildListItem(nextFocusedCategory, "MemberGroupsListBox");
                    FindFirstChildListItem(memberGroupsList, "MembersListBox").Focus();
                }
            }
            e.Handled = true;
        }

        // "MainGrid" contains in itself top result and list of found search categories.
        // And main grid consider what element should be in focus, if none of them could handle focus.
        // The following scenarios could happen:
        // 1. Down key is pressed when focus is on top result.
        // 2. Up key is pressed when focus is on top result.
        // 3. Up key is pressed when focus is on first row of first category.
        private void OnMainGridKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key != Key.Down) && (e.Key != Key.Up))
                return;
            var librarySearchViewElement = sender as FrameworkElement;

            // We are at the top result. If down was pressed, 
            // that means we have to move to first class/method button.
            if (e.Key == Key.Down)
            {
                var firstCategory = FindFirstChildListItem(librarySearchViewElement, "CategoryListView");
                var firstCategoryContent = firstCategory.Content as SearchCategory;
                // If classes presented, set focus on the first class button.
                if (firstCategoryContent.Classes.Count > 0)
                {
                    FindFirstChildListItem(firstCategory, "SubCategoryListView").Focus();
                    e.Handled = true;
                    return;
                }

                // Otherwise, set focus on the first method button.
                var firstMemberGroup = FindFirstChildListItem(firstCategory, "MemberGroupsListBox");
                FindFirstChildListItem(firstMemberGroup, "MembersListBox").Focus();
            }
            else // Otherwise, Up was pressed. So, we have to move to top result.
            {
                var topResult = WPF.FindChild<ListBox>(this, "topResultListBox");
                topResult.Focus();
            }

            e.Handled = true;
        }

        private ListBoxItem GetListItemByIndex(ListBox parent, int index)
        {
            if (parent.Equals(null)) return null;

            var generator = parent.ItemContainerGenerator;
            if ((index >= 0) && (index < parent.Items.Count))
                return generator.ContainerFromIndex(index) as ListBoxItem;

            return null;
        }

        #endregion

    }
}
