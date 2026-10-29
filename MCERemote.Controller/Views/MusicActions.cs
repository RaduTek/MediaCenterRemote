using MCERemote.Core;
using System;
using System.Threading;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Actions shared by the music library pages.
    /// </summary>
    internal static class MusicActions
    {
        /// <summary>
        /// Opens a library entry: albums and playlists open their songs, artists and genres
        /// open their albums and songs, and songs show the song menu.
        /// </summary>
        /// <param name="page">The page the entry was chosen on.</param>
        /// <param name="restoreFocus">Restores focus on the page after the song menu closes.</param>
        public static void Open(Controls.PageView page, LibraryEntry entry, Action restoreFocus)
        {
            switch (entry.Kind)
            {
                case LibraryItemKind.Album:
                case LibraryItemKind.Playlist:
                    page.ParentFrame?.OpenPage(new MusicCollection(entry.Kind, entry.Title));
                    break;

                case LibraryItemKind.Artist:
                case LibraryItemKind.Genre:
                    page.ParentFrame?.OpenPage(new MusicCategory(entry.Kind, entry.Title));
                    break;

                case LibraryItemKind.Song:
                    ShowSongMenu(page, entry, restoreFocus);
                    break;
            }
        }

        /// <summary>
        /// Shows the menu of a song, with its details and the options to play it or add it
        /// to the playback queue.
        /// </summary>
        public static void ShowSongMenu(Controls.PageView page, LibraryEntry song, Action restoreFocus)
        {
            var host = page.ParentFrame?.DialogHost;
            if (host == null)
                return;

            var connection = page.Connection;
            var sourceURL = song.SourceURL;

            var menu = new Controls.MenuDialog { Title = song.Title };
            menu.AddDetail(song.Subtitle + " - " + song.Album);
            // menu.AddDetail(song.DurationText);
            menu.AddOption("Play", () => Send(connection, controller => controller.PlaySong(sourceURL)));
            menu.AddOption("Add to Queue", () => Send(connection, controller => controller.QueueSong(sourceURL)));
            menu.Closed += (s, e) => restoreFocus?.Invoke();

            host.ShowDialog(menu);
        }

        /// <summary>
        /// Sends a command to the receiver in the background, then refreshes the player status.
        /// </summary>
        private static void Send(Connection connection, Action<IControllerService> command)
        {
            var controller = connection?.Controller;
            if (controller == null)
                return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    command(controller);
                }
                catch
                { }

                App.Current.Dispatcher.Invoke((Action)(() => connection.RefreshNow()));
            });
        }
    }
}
