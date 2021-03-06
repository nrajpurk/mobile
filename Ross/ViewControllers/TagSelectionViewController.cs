using System;
using System.Collections.Generic;
using CoreGraphics;
using Foundation;
using UIKit;
using Toggl.Ross.Theme;
using GalaSoft.MvvmLight.Helpers;
using System.Linq;
using Toggl.Phoebe.Data.Models;
using Toggl.Phoebe.ViewModels;
using Toggl.Phoebe.Reactive;

namespace Toggl.Ross.ViewControllers
{
    public class TagSelectionViewController : ObservableTableViewController<ITagData>
    {
        private TagListVM viewModel;
        private Guid workspaceId;
        private List<string> previousSelectedTags;
        private IOnTagSelectedHandler handler;

        public TagSelectionViewController(Guid workspaceId, IReadOnlyList<string> previousSelectedTags, IOnTagSelectedHandler handler) : base(UITableViewStyle.Plain)
        {
            Title = "TagTitle".Tr();
            this.workspaceId = workspaceId;
            this.previousSelectedTags = previousSelectedTags.ToList();
            this.handler = handler;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.Apply(Style.Screen);
            EdgesForExtendedLayout = UIRectEdge.None;

            viewModel = new TagListVM(StoreManager.Singleton.AppState, workspaceId);

            // Set ObservableTableViewController settings
            // ObservableTableViewController is a helper class
            // from Mvvm light package.

            TableView.RowHeight = 60f;
            TableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            CreateCellDelegate = CreateTagCell;
            BindCellDelegate = BindCell;
            DataSource = viewModel.TagCollection;

            var addBtn = new UIBarButtonItem(UIBarButtonSystemItem.Add, OnAddNewTag);
            var saveBtn = new UIBarButtonItem("TagSet".Tr(), UIBarButtonItemStyle.Plain, OnSaveBtn).Apply(Style.NavLabelButton);
            NavigationItem.RightBarButtonItems = new [] { saveBtn, addBtn};
        }

        private UITableViewCell CreateTagCell(NSString cellIdentifier)
        {
            return new TagCell(cellIdentifier);
        }

        private void BindCell(UITableViewCell cell, ITagData tagData, NSIndexPath path)
        {
            // Set selected tags.
            var isSelected = previousSelectedTags.Exists(tag => tag == tagData.Name);
            ((TagCell)cell).Bind(tagData.Name, isSelected);
        }

        protected override void OnRowSelected(object item, NSIndexPath indexPath)
        {
            base.OnRowSelected(item, indexPath);

            var cell = (TagCell)TableView.CellAt(indexPath);
            cell.Checked = !cell.Checked;

            if (cell.Checked)
            {
                previousSelectedTags.Add(((ITagData)item).Name);
            }
            else
            {
                previousSelectedTags.RemoveAll(t => t == ((TagData)item).Name);
            }

            TableView.DeselectRow(indexPath, true);
        }

        private void OnAddNewTag(object sender, EventArgs evnt)
        {
            var vc = new NewTagViewController(workspaceId, handler);
            NavigationController.PushViewController(vc, true);
        }

        private void OnSaveBtn(object s, EventArgs e)
        {
            handler.OnModifyTagList(previousSelectedTags);
        }

        private class TagCell : UITableViewCell
        {
            private const float CellSpacing = 4f;
            private UILabel nameLabel;

            public TagCell(NSString cellIdentifier) : base(UITableViewCellStyle.Default, cellIdentifier)
            {
                InitView();
            }

            public TagCell(IntPtr handle) : base(handle)
            {
                InitView();
            }

            void InitView()
            {
                this.Apply(Style.Screen);
                ContentView.Add(nameLabel = new UILabel().Apply(Style.TagList.NameLabel));
                BackgroundView = new UIView().Apply(Style.TagList.RowBackground);
            }

            public override void LayoutSubviews()
            {
                base.LayoutSubviews();

                var contentFrame = new CGRect(0, CellSpacing / 2, Frame.Width, Frame.Height - CellSpacing);
                SelectedBackgroundView.Frame = BackgroundView.Frame = ContentView.Frame = contentFrame;

                contentFrame.X = 15f;
                contentFrame.Y = 0;
                contentFrame.Width -= 15f;

                if (Checked)
                {
                    // Adjust for the checkbox accessory
                    contentFrame.Width -= 40f;
                }

                nameLabel.Frame = contentFrame;
            }

            public void Bind(string labelString, bool isChecked)
            {
                if (string.IsNullOrWhiteSpace(labelString))
                {
                    nameLabel.Text = "TagNoNameTag".Tr();
                }
                else
                {
                    nameLabel.Text = labelString;
                }

                Checked = isChecked;
            }

            public bool Checked
            {
                get { return Accessory == UITableViewCellAccessory.Checkmark; }
                set
                {
                    Accessory = value ? UITableViewCellAccessory.Checkmark : UITableViewCellAccessory.None;
                    SetNeedsLayout();
                }
            }
        }
    }
}
