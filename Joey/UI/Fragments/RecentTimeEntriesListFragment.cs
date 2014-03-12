﻿using System;
using Android.OS;
using Android.Views;
using Android.Widget;
using Toggl.Phoebe;
using XPlatUtils;
using Toggl.Joey.Data;
using Toggl.Joey.UI.Adapters;
using Toggl.Joey.UI.Utils;
using Toggl.Joey.UI.Views;
using ListFragment = Android.Support.V4.App.ListFragment;

namespace Toggl.Joey.UI.Fragments
{
    public class RecentTimeEntriesListFragment : ListFragment
    {
        private ViewGroup mainLayout;
        private ViewGroup welcomeView;

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            mainLayout = (ViewGroup)inflater.Inflate (Resource.Layout.TimeEntriesListFragment, container, false);
            mainLayout.FindViewById<TextView> (Resource.Id.EmptyTitleTextView)
                .SetFont (Font.Roboto)
                .SetText (Resource.String.RecentTimeEntryNoItemsTitle);
            mainLayout.FindViewById<TextView> (Resource.Id.EmptyTextTextView)
                .SetFont (Font.RobotoLight);

            var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
            if (!settingsStore.GotWelcomeMessage) {
                ShowWelcomeView (inflater);
            }

            return mainLayout;
        }

        private void ShowWelcomeView (LayoutInflater inflater)
        {
            welcomeView = (ViewGroup)inflater.Inflate (Resource.Layout.WelcomeBox, null);
            welcomeView.FindViewById<TextView> (Resource.Id.StartTextView).SetFont (Font.Roboto);
            welcomeView.FindViewById<TextView> (Resource.Id.SwipeLeftTextView).SetFont (Font.RobotoLight);
            welcomeView.FindViewById<TextView> (Resource.Id.SwipeRightTextView).SetFont (Font.RobotoLight);
            welcomeView.FindViewById<TextView> (Resource.Id.TapToContinueTextView).SetFont (Font.RobotoLight);
            welcomeView.FindViewById<Button> (Resource.Id.GotItButton)
                .SetFont (Font.Roboto).Click += OnGotItButtonClick;
        }

        private void OnGotItButtonClick (object sender, EventArgs e)
        {
            var settingsStore = ServiceContainer.Resolve<SettingsStore> ();
            settingsStore.GotWelcomeMessage = true;
            ListView.RemoveHeaderView (welcomeView);
        }

        public override void OnViewCreated (View view, Bundle savedInstanceState)
        {
            base.OnViewCreated (view, savedInstanceState);
            ListView.SetClipToPadding (false);
        }

        public override void OnResume ()
        {
            EnsureAdapter ();
            base.OnResume ();
        }

        public override void OnListItemClick (ListView l, View v, int position, long id)
        {
            RecentTimeEntriesAdapter adapter = null;
            if (l.Adapter is HeaderViewListAdapter) {
                var headerAdapter = (HeaderViewListAdapter)l.Adapter;
                adapter = headerAdapter.WrappedAdapter as RecentTimeEntriesAdapter;
                // Adjust the position by taking into account the fact that we've got headers
                position -= headerAdapter.HeadersCount;
            } else if (l.Adapter is RecentTimeEntriesAdapter) {
                adapter = (RecentTimeEntriesAdapter)l.Adapter;
            }

            if (adapter == null || position < 0 || position >= adapter.Count)
                return;

            var model = adapter.GetModel (position);
            if (model == null)
                return;

            var entry = model.Continue ();

            // Scroll to top (where the new model will appear)
            ListView.SmoothScrollToPosition (0);

            // Notify that the user explicitly started something
            var bus = ServiceContainer.Resolve<MessageBus> ();
            bus.Send (new UserTimeEntryStateChangeMessage (this, entry));
        }

        public override bool UserVisibleHint {
            get { return base.UserVisibleHint; }
            set {
                base.UserVisibleHint = value;
                EnsureAdapter ();
            }
        }

        private void EnsureAdapter ()
        {
            if (ListAdapter == null && UserVisibleHint && IsAdded) {
                if (welcomeView != null) {
                    ListView.AddHeaderView (welcomeView);
                }
                ListAdapter = new RecentTimeEntriesAdapter ();
            }
        }
    }
}
