using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Phoenix.UWP
{
    public sealed partial class LibrarySettings : ContentDialog
    {
        private ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        private bool UseDefaultLibrary { get; set; }

        private ObservableCollection<IStorageFolder> LibraryFolders { get; } = new ObservableCollection<IStorageFolder>();

        public LibrarySettings()
        {
            this.InitializeComponent();

            UseDefaultLibrary = (bool)localSettings.Values["UseDefaultLibrary"];
            ParseFolders();
        }

        #region folder serialiser

        private async void ParseFolders()
        {
            return;

            // TODO: Figure out how UWP manages known folders
            var libraryPath = (string)localSettings.Values["LibraryPath"];
            foreach (var folder in libraryPath.Split(';'))
            {
                if (folder.Length == 0) continue;
                LibraryFolders.Add(await StorageFolder.GetFolderFromPathAsync(folder));
            }
        }

        private string SaveFolders()
        {
            var sb = new StringBuilder();

            bool first = true;
            foreach (var folder in LibraryFolders)
            {
                if (!first) sb.Append(";");
                first = false;
                sb.Append(folder.Path);
            }

            return sb.ToString();
        }

        #endregion

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            localSettings.Values["UseDefaultLibrary"] = UseDefaultLibrary;
            localSettings.Values["LibraryPath"] = SaveFolders();
        }

        private async void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) { return; }

            if (!LibraryFolders.Contains(folder)) LibraryFolders.Add(folder);
        }

        private void Hyperlink_Click(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            Launcher.LaunchFolderAsync(KnownFolders.MusicLibrary);
        } 
    }
}
