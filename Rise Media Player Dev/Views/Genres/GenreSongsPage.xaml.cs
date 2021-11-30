﻿using Microsoft.Toolkit.Uwp.UI;
using Rise.App.Common;
using Rise.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Navigation;

namespace Rise.App.Views
{
    public sealed partial class GenreSongsPage : Page
    {
        #region Variables
        /// <summary>
        /// Gets the app-wide MViewModel instance.
        /// </summary>
        public MainViewModel MViewModel => App.MViewModel;

        /// <summary>
        /// Gets the app-wide PViewModel instance.
        /// </summary>
        private PlaybackViewModel PViewModel => App.PViewModel;

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        private readonly NavigationHelper _navigationHelper;

        private static readonly DependencyProperty SelectedGenreProperty =
            DependencyProperty.Register("SelectedGenre", typeof(GenreViewModel), typeof(GenreSongsPage), null);

        private GenreViewModel SelectedGenre
        {
            get => (GenreViewModel)GetValue(SelectedGenreProperty);
            set => SetValue(SelectedGenreProperty, value);
        }

        public SongViewModel SelectedSong
        {
            get => MViewModel.SelectedSong;
            set => MViewModel.SelectedSong = value;
        }

        private AdvancedCollectionView Songs => MViewModel.FilteredSongs;
        private AdvancedCollectionView Albums => MViewModel.FilteredAlbums;
        private AdvancedCollectionView Artists => MViewModel.FilteredArtists;

        private string SortProperty = "Title";
        private SortDirection CurrentSort = SortDirection.Ascending;

        private RelayCommand _playCommand;
        public RelayCommand PlayCommand
        {
            get
            {
                if (_playCommand == null)
                {
                    _playCommand = new RelayCommand(async () => await StartPlaybackAsync());
                }

                return _playCommand;
            }
            set => _playCommand = value;
        }

        private RelayCommand _shuffleCommand;
        public RelayCommand ShuffleCommand
        {
            get
            {
                if (_shuffleCommand == null)
                {
                    _shuffleCommand = new RelayCommand(async () => await StartPlaybackAsync(0, true));
                }

                return _shuffleCommand;
            }
            set => _shuffleCommand = value;
        }

        private RelayCommand _sortCommand;
        public RelayCommand SortCommand
        {
            get
            {
                if (_sortCommand == null)
                {
                    _sortCommand = new RelayCommand(SortItems);
                }

                return _sortCommand;
            }
            set => _sortCommand = value;
        }

        private RelayCommand _viewCommand;
        public RelayCommand ViewCommand
        {
            get
            {
                if (_viewCommand == null)
                {
                    _viewCommand = new RelayCommand(ChangeView);
                }

                return _viewCommand;
            }
            set => _viewCommand = value;
        }

        private RelayCommand _editCommand;
        public RelayCommand EditCommand
        {
            get
            {
                if (_editCommand == null)
                {
                    _editCommand = new RelayCommand(async () => await SelectedSong.StartEdit());
                }

                return _editCommand;
            }
            set => _editCommand = value;
        }
        #endregion

        public GenreSongsPage()
        {
            InitializeComponent();
            NavigationCacheMode = NavigationCacheMode.Enabled;

            DataContext = this;
            _navigationHelper = new NavigationHelper(this);
            _navigationHelper.LoadState += NavigationHelper_LoadState;
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            if (e.NavigationParameter is GenreViewModel genre)
            {
                SelectedGenre = genre;
            }
            else if (e.NavigationParameter is string str)
            {
                SelectedGenre = App.MViewModel.Genres.First(g => g.Name == str);
            }

            Songs.Filter = s => ((SongViewModel)s).Genres.Contains(SelectedGenre.Name);
            Songs.SortDescriptions.Clear();
            Songs.SortDescriptions.Add(new SortDescription("Title", SortDirection.Ascending));

            Albums.Filter = a => ((AlbumViewModel)a).Genres.Contains(SelectedGenre.Name);
            Albums.SortDescriptions.Clear();
            Albums.SortDescriptions.Add(new SortDescription("Title", SortDirection.Ascending));
        }

        #region Event handlers
        private async void MainList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement).DataContext is SongViewModel song)
            {
                int index = MainList.Items.IndexOf(song);
                await StartPlaybackAsync(index);
            }
        }

        private void MainList_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if ((e.OriginalSource as FrameworkElement).DataContext is SongViewModel song)
            {
                SelectedSong = song;
                SongFlyout.ShowAt(MainList, e.GetPosition(MainList));
            }
        }

        private async void Props_Click(object sender, RoutedEventArgs e)
            => await SelectedSong.StartEdit();

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
            => await StartPlaybackAsync();

        private async void ShuffleButton_Click(object sender, RoutedEventArgs e)
            => await StartPlaybackAsync(0, true);

        private async Task StartPlaybackAsync(int index = 0, bool shuffle = false)
        {
            if (SelectedSong != null && index == 0)
            {
                index = MainList.Items.IndexOf(SelectedSong);
                SelectedSong = null;
            }

            IEnumerator<object> enumerator = Songs.GetEnumerator();
            List<SongViewModel> songs = new List<SongViewModel>();

            while (enumerator.MoveNext())
            {
                songs.Add(enumerator.Current as SongViewModel);
            }

            enumerator.Dispose();
            await PViewModel.StartMusicPlaybackAsync(songs.GetEnumerator(), index, songs.Count, shuffle);
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            await SelectedSong.StartEdit();
            SelectedSong = null;
        }

        private void ChangeView_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem item = sender as MenuFlyoutItem;
            string tag = item.Tag.ToString();

            switch (tag)
            {
                default:
                    break;
            }
        }

        private void SortItems(object param)
        {
            Songs.SortDescriptions.Clear();
            string by = param.ToString();

            switch (by)
            {
                case "Ascending":
                    CurrentSort = SortDirection.Ascending;
                    break;

                case "Descending":
                    CurrentSort = SortDirection.Descending;
                    break;

                case "Track":
                    Songs.SortDescriptions.
                        Add(new SortDescription("Disc", CurrentSort));
                    SortProperty = by;
                    break;

                default:
                    SortProperty = by;
                    break;
            }

            Songs.SortDescriptions.
                Add(new SortDescription(SortProperty, CurrentSort));
        }

        private void ChangeView(object param)
        {
            string view = param.ToString();
            switch (view)
            {
                case "Default":
                    CurrentSort = SortDirection.Ascending;
                    break;

                case "Descending":
                    CurrentSort = SortDirection.Descending;
                    break;

                default:
                    SortProperty = view;
                    break;
            }
        }
        #endregion

        #region NavigationHelper registration
        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
            => _navigationHelper.OnNavigatedTo(e);

        protected override void OnNavigatedFrom(NavigationEventArgs e)
            => _navigationHelper.OnNavigatedFrom(e);
        #endregion
    }

    [ContentProperty(Name = "GenreTemplate")]
    public class GenreContentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate GenreTemplate { get; set; }
        public DataTemplate AlbumTemplate { get; set; }

        public DataTemplate ArtistTemplate { get; set; }
        // public DataTemplate SeparatorTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            return GenreTemplate;
        }
    }
}