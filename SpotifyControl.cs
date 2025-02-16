using Newtonsoft.Json;
using SpotifyAPI;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using Swan;


namespace Azura.Spotify
{
    public class SpotifyControl
    {
        //initializations for Spotify to authenticate properly
        private const string CredentialsPath = "credentials.json";
        private static readonly string? clientId = "382ae32290d1431883f8825bc43a3ed1";
        private static readonly EmbedIOAuthServer _server = new(new Uri("http://localhost:8888/callback"), 8888);
        private static SpotifyClient spotify;

        //public variables for Twitch to use on commands
        public static string songName;
        public static string artistName;


        public SpotifyControl()
        {
            Initialize().Wait();
        }

        public static async Task Initialize()
        {
            if (string.IsNullOrEmpty(clientId))
            {
                throw new NullReferenceException("NO SPOTIFY KEY PASSED, MAKE SURE 'SPOTIFY_CLIENT_ID' IS PROPERLY SET");
            }
            //if the credentials exist, start, otherwise request request them
            if (File.Exists(CredentialsPath))
            {
                await Start();
            }
            else
            {
                await Authenticate();
            }
        }

        private static async Task Authenticate()
        {
            var (verifier, challenge) = PKCEUtil.GenerateCodes();

            await _server.Start();
            _server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await _server.Stop();
                var token = await new OAuthClient().RequestToken(new PKCETokenRequest(clientId!, response.Code!, _server.BaseUri, verifier));
                await File.WriteAllTextAsync(CredentialsPath, JsonConvert.SerializeObject(token));
                await Start();
            };

            var request = new LoginRequest(_server.BaseUri, clientId!, LoginRequest.ResponseType.Code)
            {
                CodeChallenge = challenge,
                CodeChallengeMethod = "S256",
                Scope = new[] { Scopes.UserReadPlaybackState, Scopes.UserModifyPlaybackState }
            };

            var uri = request.ToUri();
            try
            {
                BrowserUtil.Open(uri);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to open a browser, please manually navigate to: " + uri + "and continue.");
            }
        }
        private static async Task Start()
        {
            //read token from stored JSON file
            var json = await File.ReadAllTextAsync(CredentialsPath);
            var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);
            var auth = new PKCEAuthenticator(clientId!, token!);

            //if the token needs to be refreshed, overwrite the old one
            auth.TokenRefreshed += (sender, token) => File.WriteAllText(CredentialsPath, JsonConvert.SerializeObject(token));

            //initialize a connection with Spotify using said token
            var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(auth);
            spotify = new SpotifyClient(config);
            var user = await spotify.UserProfile.Current();

            //initialize a variable to store the currently playing song (null since we don't know what's playing yet)
            CurrentlyPlaying playing = null;

            Console.WriteLine("Ready!");


            // Query currently playing song every 5 seconds
            //TODO: There must be a better way to achieve this :noia_poke:
            var periodicTimer = new PeriodicTimer(TimeSpan.FromSeconds(2));
            while (await periodicTimer.WaitForNextTickAsync())
            {
                try
                {
                    playing = await spotify.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest(PlayerCurrentlyPlayingRequest.AdditionalTypes.Track));
                }
                catch (APIException e)
                {
                    Console.WriteLine(e.Message);
                }

                if (playing?.Item is FullTrack song)
                {
                    songName = song.Name;
                    artistName = song.Artists.First().Name;
                }
            }

        }

        public static async Task Skip()
        {
            await spotify.Player.SkipNext();
        }
        public static async Task Queue(string uri)
        {
            uri = uri.Replace("https://open.spotify.com/track/", "");
            int index = uri.IndexOf("?si=");
            var newUri = uri.Substring(0, index);
            newUri = "spotify:track:" + newUri;
            Console.WriteLine(newUri);
            await spotify.Player.AddToQueue(new PlayerAddToQueueRequest(newUri));
        }
    }
}