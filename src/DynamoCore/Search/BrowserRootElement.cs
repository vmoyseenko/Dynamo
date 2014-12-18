﻿using System.Collections.ObjectModel;
using System.Linq;
using Dynamo.Search.SearchElements;

namespace Dynamo.Search
{
    public class BrowserRootElement : BrowserItem
    {

        /// <summary>
        ///     The items inside of the browser item
        /// </summary>
        private ObservableCollection<BrowserItem> _items = new ObservableCollection<BrowserItem>();
        public override ObservableCollection<BrowserItem> Items { get { return _items; } set { _items = value; } }

        public ObservableCollection<BrowserRootElement> Siblings { get; set; }

        /// <summary>
        /// Name property </summary>
        /// <value>
        /// The name of the node </value>
        private string _name;
        public override string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Property specifies if BrowserItem has members only as children. No any subcategories.
        /// </summary>        
        public bool IsPlaceholder
        {
            get
            {
                // If all childs are derived from NodeSearchElement they all are members
                // not subcategories.
                return Items.Count > 0 && !Items.Any(it => !(it is NodeSearchElement));
            }
        }

        /// <summary>
        /// Specifies whether or not Root Category package / packages need update.
        /// Makes sense for packages only.
        /// TODO: implement as design clarifications are provided.
        /// </summary>
        public bool IsUpdateAvailable { get; set; }

        /// <summary>
        /// Specifies the type of library under the category. Can be Regular, Package, CustomDll 
        /// and CustomNode.
        /// TODO: implement as design clarifications are provided. 
        /// </summary>
        public SearchModel.ElementType ElementType { get; set; }

        public BrowserRootElement(string name, ObservableCollection<BrowserRootElement> siblings)
        {
            this.Height = 32;
            this.Siblings = siblings;
            this._name = name;
        }

        public void SortChildren()
        {
            this.Items = new ObservableCollection<BrowserItem>(this.Items.OrderBy(x => x.Name));
        }

        public BrowserRootElement(string name)
        {
            this.Height = 32;
            this.Siblings = null;
            this._name = name;
        }

        public override void Execute()
        {
            var endState = !this.IsExpanded;

            foreach (var ele in this.Siblings)
                ele.IsExpanded = false;

            // Collapse all expanded items on next level.
            if (endState)
            {
                foreach (var ele in this.Items)
                    ele.IsExpanded = false;
            }

            //Walk down the tree expanding anything nested one layer deep
            //this can be removed when we have the hierachy implemented properly
            if (this.Items.Count == 1)
            {
                BrowserItem subElement = this.Items[0];

                while (subElement.Items.Count == 1)
                {
                    subElement.IsExpanded = true;
                    subElement = subElement.Items[0];
                }

                subElement.IsExpanded = true;

            }

            this.IsExpanded = endState;
        }

    }
}