﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Microsoft.Toolkit.Uwp.Notifications
{
    /// <summary>
    /// Provides a way to monitor and react to changes to your app's notifications. To obtain an instance of this, call <see cref="ToastHistoryChangeTracker.GetChangeReaderAsync"/>.
    /// </summary>
    public sealed class ToastHistoryChangeReader
    {
        private ToastHistoryChangeRecord[] _currentRecords;
        private List<ToastHistoryChange> _changes;

        private ToastHistoryChangeReader() { }

        internal static async Task<ToastHistoryChangeReader> Initialize()
        {
            ToastHistoryChangeReader reader = new Notifications.ToastHistoryChangeReader();

            try
            {
                // Get the (unsorted) changes
                ToastHistoryChangeRecord[] changeRecords = await ToastHistoryChangeDatabase.GetChangesAsync();

                // We store the current records so they can be used for AcceptChanges
                reader._currentRecords = changeRecords;

                // If it was null, that would mean we're in an invalid state
                if (changeRecords != null)
                {
                    // Sort and convert them into the public type
                    List<ToastHistoryChange> changes = changeRecords
                        .OrderBy(i => i)
                        .Select(i => new ToastHistoryChange(i))
                        .ToList();

                    for (int i = changes.Count - 1; i >= 0; i--)
                    {
                        var change = changes[i];

                        // If the curr change type is AddedViaPush, we need to flip the first previous
                        // duplicate tag/group to ReplacedByPush (unless it's already dismissed)

                        // If the curr change type is ReplacedByPush, we need to flip first previous
                        // duplicate tag/group to ReplacedByPush (unless it's already dismissed)

                        // If the curr change type is DismissedByUser, we need to flip first previous
                        // duplicate tag/group to also be DismissedByUser

                        // Subsequent previous ones will be updated when we hit them in the loop

                        for (int x = i - 1; x >= 0; x--)
                        {
                            var prev = changes[x];
                            if (prev.Tag.Equals(change.Tag) && prev.Group.Equals(change.Group))
                            {
                                switch (change.ChangeType)
                                {
                                    case ToastHistoryChangeType.AddedViaPush:
                                    case ToastHistoryChangeType.ReplacedViaPush:
                                        if (prev.ChangeType != ToastHistoryChangeType.DismissedByUser)
                                        {
                                            prev.ChangeType = ToastHistoryChangeType.ReplacedViaPush;
                                        }

                                        break;

                                    case ToastHistoryChangeType.DismissedByUser:
                                        prev.ChangeType = ToastHistoryChangeType.DismissedByUser;
                                        break;
                                }

                                break;
                            }
                        }
                    }

                    reader._changes = changes;
                    return reader;
                }
            }
            catch { }

            // Either null answer or exception brings us down to this point
            reader._changes = new List<ToastHistoryChange>()
                {
                    new ToastHistoryChange()
                    {
                        ChangeType = ToastHistoryChangeType.ChangeTrackingLost
                    }
                };
            return reader;
        }

        /// <summary>
        /// Asynchronously gets the list of <see cref="ToastHistoryChange"/> objects that occurred since the creation of this reader. To get new changes, you need to obtain a new <see cref="ToastHistoryChangeReader"/>.
        /// </summary>
        /// <returns>A list of <see cref="ToastHistoryChange"/> objects.</returns>
        public IAsyncOperation<IList<ToastHistoryChange>> ReadChangesAsync()
        {
            return Task.FromResult<IList<ToastHistoryChange>>(_changes.ToList()).AsAsyncOperation();
        }

        /// <summary>
        /// Call this method to indicate that you have processed and accepted all changes and you don't want the system to show them to you again.
        /// </summary>
        /// <returns>An async action.</returns>
        public IAsyncAction AcceptChangesAsync()
        {
            return AcceptChangesAsyncHelper().AsAsyncAction();
        }

        private async Task AcceptChangesAsyncHelper()
        {
            if (_currentRecords != null)
            {
                await ToastHistoryChangeDatabase.AcceptChangesAsync(_currentRecords);
                _currentRecords = null;
            }
        }
    }
}
