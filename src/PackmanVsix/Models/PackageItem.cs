using System;
using System.Collections.Generic;

namespace PackmanVsix.Models
{
    public class PackageItem : BindableBase
    {
        private IReadOnlyList<PackageItem> _children;
        private bool _isChecked;
        private bool _isMain;
        private PackageItemType _itemType;
        private string _name;
        private bool _isExpanded;

        public PackageItem(InstallDialogViewModel parent)
        {
            Children = new PackageItem[0];
            ParentModel = parent;
        }

        public InstallDialogViewModel ParentModel { get; }

        public IReadOnlyList<PackageItem> Children
        {
            get { return _children; }
            set { Set(ref _children, value); }
        }

        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (Set(ref _isChecked, value))
                {
                    foreach (PackageItem child in Children)
                    {
                        child.IsChecked = value;
                    }

                    if (ItemType == PackageItemType.File)
                    {
                        if (value)
                        {
                            ParentModel.SelectedFiles.Add(FullPath);
                        }
                        else
                        {
                            ParentModel.SelectedFiles.Remove(FullPath);
                        }
                    }
                }
            }
        }

        public string FullPath { get; set; }

        public bool IsMain
        {
            get { return _isMain; }
            set
            {
                if (Set(ref _isMain, value) && value)
                {
                    IsChecked = true;
                }
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { Set(ref _isExpanded, value); }
        }

        public PackageItemType ItemType
        {
            get { return _itemType; }
            set { Set(ref _itemType, value); }
        }

        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value, StringComparer.Ordinal); }
        }
    }
}