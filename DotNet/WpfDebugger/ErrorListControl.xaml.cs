﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using Disassembler;

namespace WpfDebugger
{
    /// <summary>
    /// Interaction logic for ErrorListControl.xaml
    /// </summary>
    public partial class ErrorListControl : UserControl
    {
        public ErrorListControl()
        {
            InitializeComponent();
            this.DataContext = new ErrorListViewModel(null);
        }

        private BinaryImage image;

        public BinaryImage Image
        {
            get { return image; }
            set
            {
                image = value;
                this.DataContext = new ErrorListViewModel(image);
            }
        }

        private void ToolBar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolBar = sender as ToolBar;
            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Hidden;
            }
        }

#if false
        private void lvErrors_DoubleClick(object sender, EventArgs e)
        {
            if (lvErrors.SelectedIndices.Count == 1)
            {
                Error error = (Error)lvErrors.SelectedItems[0].Tag;
                document.Navigator.SetLocation(error.Location, this);
            }
        }
#endif
    }

    // Note: while we might be able to use the supplied WPF Filtering
    // capability as described in 
    // http://msdn.microsoft.com/en-us/library/ms752347.aspx#filtering,
    // I think it's better to handle filtering ourselves as that will
    // be faster.
    class ErrorListViewModel : INotifyPropertyChanged
    {
        private ErrorListItem[] allItems;

        public ErrorListItem[] Items { get; private set; }

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int MessageCount { get; private set; }

        public bool HasErrors { get { return ErrorCount > 0; } }
        public bool HasWarnings { get { return WarningCount > 0; } }
        public bool HasMessages { get { return MessageCount > 0; } }

        private ErrorCategory filter = ErrorCategory.None;

        /// <summary>
        /// This should only be called internally, because it doesn't
        /// raise PropertyChanged notifications on ShowXXX properties!
        /// </summary>
        private ErrorCategory Filter
        {
            get { return filter; }
            set
            {
                if (filter == value)
                    return;
                filter = value;
                UpdateFilter();
            }
        }

        public bool ShowErrors
        {
            get { return Filter.HasFlag(ErrorCategory.Error); }
            set
            {
                if (value && ErrorCount > 0)
                    Filter |= ErrorCategory.Error;
                else
                    Filter &= ~ErrorCategory.Error;
            }
        }

        public bool ShowWarnings
        {
            get { return Filter.HasFlag(ErrorCategory.Warning); }
            set
            {
                if (value && WarningCount > 0)
                    Filter |= ErrorCategory.Warning;
                else
                    Filter &= ~ErrorCategory.Warning;
            }
        }

        public bool ShowMessages
        {
            get { return Filter.HasFlag(ErrorCategory.Message); }
            set
            {
                if (value && MessageCount > 0)
                    Filter |= ErrorCategory.Message;
                else
                    Filter &= ~ErrorCategory.Message;
            }
        }

        public ErrorListViewModel(BinaryImage image)
        {
            if (image == null)
                return;

            int errorCount = 0;
            int warningCount = 0;
            int messageCount = 0;

            int n = image.Errors.Count;
            allItems = new ErrorListItem[n];
            for (int i = 0; i < n; i++)
            {
                Error error = image.Errors[i];
                allItems[i] = new ErrorListItem(error);

                switch (error.Category)
                {
                    case ErrorCategory.Error: errorCount++; break;
                    case ErrorCategory.Warning: warningCount++; break;
                    case ErrorCategory.Message: messageCount++; break;
                }
            }
            Array.Sort(allItems,
                       (x, y) => x.Error.Location.LinearAddress.CompareTo(y.Error.Location.LinearAddress));

            //this.Items = items.ToArray();
            this.Items = null; // no item to display initially
            this.ErrorCount = errorCount;
            this.WarningCount = warningCount;
            this.MessageCount = messageCount;
            this.ShowErrors = true;
        }

        /// <summary>
        /// Refresh Items[] according to Filter.
        /// </summary>
        private void UpdateFilter()
        {
            if (allItems == null)
                return;

            Items = (from errorItem in allItems
                     where (errorItem.Error.Category & filter) != 0
                     select errorItem
                   ).ToArray();

            if (PropertyChanged != null)
            {
                var e = new PropertyChangedEventArgs("Items");
                PropertyChanged(this, e);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal class ErrorListItem
        {
            public Error Error { get; private set; }

            public ErrorListItem(Error error)
            {
                this.Error = error;
            }

            public Pointer Location
            {
                get { return Error.Location; }
            }

            public string Message
            {
                get { return Error.Message; }
            }
        }
    }
}
