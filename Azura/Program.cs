using Azura.Spotify;
using Azura.Twitch;

namespace Azura
{
    class MainProgram
    {
        static void Main(string[] args)
        {
            Twitch();
            Spotify();
            Console.ReadKey();
        }

        private static void Twitch()
        {
            TwitchBot twitchBot = new TwitchBot();
        }

        private static void Spotify()
        {
            SpotifyControl spotifyControl = new SpotifyControl();
        }
    }
}