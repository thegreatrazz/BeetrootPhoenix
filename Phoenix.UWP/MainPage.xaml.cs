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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections;
using System.Drawing;
using Windows.UI.Popups;
using System.Runtime.CompilerServices;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Hosting;
using Color = Windows.UI.Color;

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

        ObservableCollection<Song> SongLibrary { get; } = new ObservableCollection<Song>();

        ObservableCollection<Song> SongQueue { get; } = new ObservableCollection<Song>();

        Song CurrentSong { get; set; } = null;

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
            this.ScanLibrary();

            // set up media player events
            mediaPlayer.MediaOpened += this.MediaPlayer_MediaOpened;
            mediaPlayer.MediaEnded += this.MediaPlayer_MediaEnded;
            mediaPlayer.MediaFailed += this.MediaPlayer_MediaFailed;
            mediaPlayer.CurrentStateChanged += this.MediaPlayer_CurrentStateChanged;
            mediaPlayer.CommandManager.NextBehavior.EnablingRule = MediaCommandEnablingRule.Always;
            mediaPlayer.CommandManager.NextReceived += this.MediaPlayer_NextReceived;
        }

        private Song SongQueue_Dequeue()
        {
            if (SongQueue.Count == 0) return null;

            var song = SongQueue[0];
            SongQueue.RemoveAt(0);

            // TODO: Auto-generate queue if user wants to

            return song;
        }

        private void SongQueue_Enqueue(Song song)
        {
            SongQueue.Add(song);
        }

        private void SongQueue_MoveUp(int index)
        {
            if (index > 0)
            {
                var song = SongQueue[index];
                SongQueue.RemoveAt(index);
                SongQueue.Insert(index - 1, song);
            }
        }

        private void SongQueue_MoveDown(int index)
        {
            if (index < SongQueue.Count - 1)
            {
                var song = SongQueue[index];
                SongQueue.RemoveAt(index);
                SongQueue.Insert(index + 1, song);
            }
        }

        #region media player event handlers

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

        private async void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            // TODO: warn user, poll queue and play

            new MessageDialog("Beetroot failed to read this media file. Skipping.", "Failed to read media file.").ShowAsync();


            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                var song = SongQueue_Dequeue();
                CurrentSong = song;
            });

            if (CurrentSong == null)
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
                {
                    var song = SongQueue_Dequeue();
                    CurrentSong = song;
                });

                return;
            }

            mediaPlayer.Source = MediaSource.CreateFromStorageFile(CurrentSong.File);
            mediaPlayer.Play();
        }

        private async void MediaPlayer_MediaOpened(MediaPlayer sender, object args)
        {
            // TODO: update the Windows playback system as well

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Low, () =>
            {
                if (CurrentSong == null) return;

                MetaSongTitle.Text = CurrentSong.Title;
                if (CurrentSong.Artist != null)
                {
                    MetaSongAuthor.Text = CurrentSong.Artist;
                    MetaSongAuthor.Visibility = Visibility.Visible;
                }
                else
                {
                    MetaSongAuthor.Visibility = Visibility.Collapsed;
                }
            });
        }

        private void MediaPlayer_NextReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerNextReceivedEventArgs args)
        {
            var song = SongQueue_Dequeue();
            if (song == null) return;

            CurrentSong = song;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(song.File);
            mediaPlayer.Play();
        }

        private void MediaPlayer_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            // TODO: not gonna implement this... to be removed
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            var song = SongQueue_Dequeue();
            if (song == null) return;

            CurrentSong = song;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(song.File);
            mediaPlayer.Play();
        }

        #endregion

        private async void ScanLibrary()
        {
            /* TODO: maintain a list of folders for search music in
             *       for now, we only have the built-in Music library */

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
                        this.SongLibrary.Add(await Song.FromFile((StorageFile)item));
                    }
                }
            }
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            // get the size of the buttons on the title bar to avoid them
            LeftPaddingColumn.Width = new GridLength(sender.SystemOverlayLeftInset);
            RightPaddingColumn.Width = new GridLength(sender.SystemOverlayRightInset);
            AppTitleContainer.Margin = new Thickness(0, 0, sender.SystemOverlayRightInset, 0);
            MainContent.Margin = new Thickness(0, sender.Height, 0, 0);

            // update the titlebar height
            AppTitleBar.Height = sender.Height;
        }

        private void CoreTitleBar_IsVisibleChanged(CoreApplicationViewTitleBar sender, object args)
        {

        }

        private void DJPlayButton_Click(object sender, RoutedEventArgs e)
        {
            // if there is no media, pick something out and go
            if (mediaPlayer.Source == null)
            {
                var song = SongQueue_Dequeue();
                if (song == null) return;

                CurrentSong = song;
                mediaPlayer.Source = MediaSource.CreateFromStorageFile(song.File);
                mediaPlayer.Play();
            }

            var state = mediaPlayer.PlaybackSession.PlaybackState;

            // switch the state
            if (state == MediaPlaybackState.Paused) mediaPlayer.Play();
            if (state == MediaPlaybackState.Playing) mediaPlayer.Pause();

            // we don't do anything for the other states on purpuse
            // whether it's bufferring or opening, we shouldn't mess with that
        }

        private void DJSkipButton_Click(object sender, RoutedEventArgs e)
        {
            var song = SongQueue_Dequeue();
            if (song == null) return;

            CurrentSong = song;
            mediaPlayer.Source = MediaSource.CreateFromStorageFile(song.File);
            mediaPlayer.Play();
        }

        private void LibraryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var song = (Song)((ListView)sender).SelectedItem;

            // ignore if the selection is the same as the currently playing song
            //if (((MediaSource)mediaPlayer.Source).Uri.LocalPath == song.File.Path) return;

            SongQueue.Add(song);

            //mediaPlayer.Source = MediaSource.CreateFromStorageFile(song.File);
            //mediaPlayer.Play();
        }

        private void QueueListView_Delete(object sender, RoutedEventArgs e)
        {
            int index = QueueListView.Items.IndexOf(((Button)sender).Parent);
            SongQueue.RemoveAt(index);
        }

        private void QueueListView_MoveDown(object sender, RoutedEventArgs e)
        {
            int index = QueueListView.Items.IndexOf(sender);
            SongQueue_MoveDown(index);
        }

        private void QueueListView_MoveUp(object sender, RoutedEventArgs e)
        {
            int index = QueueListView.Items.IndexOf(sender);
            SongQueue_MoveUp(index);
        }

        private void LibrarySettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new LibrarySettings().ShowAsync();
        }

        private void RequestSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new RequestSettings().ShowAsync();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            new GeneralSettings().ShowAsync();
        }

        private async void ShareWithFriends_Clicked(Windows.UI.Xaml.Documents.Hyperlink sender, Windows.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            var appWindow = await AppWindow.TryCreateAsync();
            var titlebar = appWindow.TitleBar;
            titlebar.ExtendsContentIntoTitleBar = true;
            titlebar.ButtonBackgroundColor = Colors.Transparent;

            var appWindowFrame = new Frame();
            appWindowFrame.Navigate(typeof(RequestLink));

            ElementCompositionPreview.SetAppWindowContent(appWindow, appWindowFrame);

            await appWindow.TryShowAsync();

            appWindow.Closed += delegate
            {
                appWindowFrame.Content = null;
                appWindow = null;
            };
        }
    }
}
