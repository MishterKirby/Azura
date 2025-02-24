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
            Console.ReadLine();
        }

        private static void Twitch()
        {
            Console.WriteLine("Starting Twitch engine...");
            TwitchBot twitchBot = new TwitchBot();
        }

        private static void Spotify()
        {
            Console.WriteLine("Starting Spotify engine...");
            SpotifyControl spotifyControl = new SpotifyControl();
            
        }
    }
}