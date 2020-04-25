using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Id3;
using Windows.Storage.Streams;

namespace Phoenix.UWP
{
    class Song
    {
        public IStorageFile File { get; protected set; } = null;

        public string Title { get; protected set; } = null;

        public string Artist { get; protected set; } = null;

        public string Album { get; protected set; } = null;

        private Song() { }

        public static async Task<Song> FromFile(IStorageFile file)
        {
            var song = new Song() { File = file };
            song.Title = file.Name;

            if (file.Name.ToLower().EndsWith(".mp3"))
            {
                // add metadata for mp3s

                var fileStream = await file.OpenReadAsync();
                IBuffer buffer = new Windows.Storage.Streams.Buffer((uint)fileStream.Size);
                await fileStream.ReadAsync(buffer, buffer.Capacity, InputStreamOptions.None);

                byte[] data = new byte[fileStream.Size];
                using (var dataReader = DataReader.FromBuffer(buffer))
                {
                    dataReader.ReadBytes(data);
                }

                var mp3 = new Id3.Mp3(data);
                if (mp3.HasTags)
                {
                    foreach (var tag in mp3.GetAllTags())
                    {
                        if (tag.Title.IsAssigned) song.Title = tag.Title.Value;
                        if (tag.Artists.IsAssigned) song.Artist = ConcatenateArtists(tag.Artists.Value.ToArray());
                        if (tag.Album.IsAssigned) song.Album = tag.Album.Value;
                    }
                }
            }

            return song;
        }

        private static string ConcatenateArtists(params string[] artists)
        {
            if (artists.Length == 0) return null;
            var stringBuilder = new StringBuilder();
            var firstArtist = true;

            foreach (var artist in artists)
            {
                if (!firstArtist) stringBuilder.Append(", ");
                firstArtist = false;
                stringBuilder.Append(artist);
            }

            return stringBuilder.ToString();
        }

        public override string ToString()
        {
            var sp = new StringBuilder(Title);
            if (Artist != null) sp.Append(" (" + Artist + ")");
            return sp.ToString();
            //return file.Name;
        }
    }
}
