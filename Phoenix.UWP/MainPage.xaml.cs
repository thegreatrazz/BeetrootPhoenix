using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System.Diagnostics;
using Windows.Media.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Phoenix.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        int libraryIndex = 0;
        List<StorageFile> musicLibrary = new List<StorageFile>();
        MediaPlayer mediaPlayer = new MediaPlayer();

        public MainPage()
        {
            this.InitializeComponent();

            // make the window buttons transparent
            var titlebar = ApplicationView.GetForCurrentView().TitleBar;
            titlebar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            titlebar.ButtonInactiveBackgroundColor = Windows.UI.Colors.Transparent;

            // combine the titlebar and window
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            coreTitleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            coreTitleBar.IsVisibleChanged += CoreTitleBar_IsVisibleChanged;

            // set XAML element as draggable region
            Window.Current.SetTitleBar(AppTitleBar);

            // Scan Library
            ScanLibrary();

            // set up media player events
            mediaPlayer.MediaOpened += this.MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += this.MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += this.MediaPlayer_MediaFailed;
            mediaPlayer.CurrentStateChanged += this.MediaPlayer_CurrentStateChanged;
            mediaPlayer.CommandManager.PreviousBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            mediaPlayer.CommandManager.PreviousReceived += this.MediaPlayer_PreviousReceived;
            mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            mediaPlayer.CommandManager.NextReceived += this.MediaPlayer_NextReceived;
        }

        private async void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                var state = sender.PlaybackSession.PlaybackState;
                switch (state)
                {
                    case MediaPlaybackState.Playing:
                        this.DJPlayButton.Content = "\uE769";
                        break;
                    case MediaPlaybackState.Paused:
                        this.DJPlayButton.Content = "\uE768";
                        break;
                    default:
                        break;
                }
            });
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            libraryIndex++;
            if (libraryIndex >= musicLibrary.Count) libraryIndex = 0;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(musicLibrary[libraryIndex]);
            mediaPlayer.Play();
        }

        private async void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            var displayUpdater = mediaPlayer.SystemMediaTransportControls.DisplayUpdater;
            await displayUpdater.CopyFromFileAsync(Windows.Media.MediaPlaybackType.Music, musicLibrary[this.libraryIndex]);

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                MetaSongTitle.Text = musicLibrary[libraryIndex].DisplayName;
            });
        }

        private void MediaPlayer_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
        {
            mediaPlayer.Pause();
            libraryIndex++;
            if (libraryIndex >= musicLibrary.Count) libraryIndex = 0;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(musicLibrary[libraryIndex]);
            mediaPlayer.Play();
        }

        private void MediaPlayer_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            mediaPlayer.Pause();
            libraryIndex--;
            if (libraryIndex < 0) libraryIndex = musicLibrary.Count - 1;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(musicLibrary[libraryIndex]);
            mediaPlayer.Play();
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            libraryIndex++;
            if (libraryIndex >= musicLibrary.Count) libraryIndex = 0;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(musicLibrary[libraryIndex]);
            mediaPlayer.Play();
        }

        private async void ScanLibrary()
        {
            var musicFolder = KnownFolders.MusicLibrary;
            List<StorageFile> files = new List<StorageFile>();
            Queue<StorageFolder> toExplore = new Queue<StorageFolder>();
            toExplore.Enqueue(musicFolder);

            while (toExplore.Count > 0)
            {
                var folder = toExplore.Dequeue();
                foreach (var item in await folder.GetItemsAsync())
                {
                    if (item is StorageFolder)
                    {
                        toExplore.Enqueue((StorageFolder)item);
                    }
                    else if (item is StorageFile)
                    {
                        files.Add((StorageFile)item);
                    }
                }
            }

            foreach (var file in files) Debug.WriteLine(file.Path);
            musicLibrary = files.OrderBy(a => Guid.NewGuid()).ToList();
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(musicLibrary[0]);
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // Get the size of the buttons on the title bar to avoid them
            LeftPaddingColumn.Width = new GridLength(sender.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
            AppTitleContainer.Margin = new Thickness(0, 0, sender.SystemOverlayRightInset, 0);
            MainContent.Margin = new Thickness(0, sender.Height, 0, 0);

            // Update the titlebar height
            AppTitleBar.Height = sender.Height;
        }

        private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {

        }

        private void DJPlayButton_Click(object sender, RoutedEventArgs e)
        {
            var state = mediaPlayer.PlaybackSession.PlaybackState;

            // switch the state
            if (state == MediaPlaybackState.Paused) mediaPlayer.Play();
            if (state == MediaPlaybackState.Playing) mediaPlayer.Pause();

            // we don't do anything for the other states on purpuse
            // whether it's bufferring or opening, we shouldn't mess with that
        }

        private void DJSkipButton_Click(object sender, RoutedEventArgs e)
        {
            var state = mediaPlayer.PlaybackSession.PlaybackState;
            mediaPlayer.Pause();
            libraryIndex++;
            if (libraryIndex >= musicLibrary.Count) libraryIndex = 0;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(musicLibrary[libraryIndex]);
            if (state == MediaPlaybackState.Playing) mediaPlayer.Play();
        }
    }
}
