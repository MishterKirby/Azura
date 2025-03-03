using System;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Clients;
using System.ComponentModel;
using Azura.Spotify;
using System.Runtime.CompilerServices;
using EmbedIO;
using Azura.Auth;
using TwitchLib.Api;
using SpotifyAPI.Web;

namespace Azura.Twitch
{ 
    class TwitchBot
    {
        // Fun stuff goes here

        string twitchKey;

        private static List<string> scopes = new List<string> { "chat:read", "whispers:read", "whispers:edit", "chat:edit", "channel:moderate" };

        TwitchClient client;

        public async Task Authenticate()
        { 
            validate();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AuthorizationCodeUrl(TwitchAuth.TwitchAppID, TwitchAuth.TwitchRedirectUri, scopes)) { UseShellExecute = true });
            //Console.WriteLine($"Please authorize here:\n{AuthorizationCodeUrl(TwitchAuth.TwitchAppID, TwitchAuth.TwitchRedirectUri, scopes)}");
            var server = new TwitchWebServer(TwitchAuth.TwitchRedirectUri);

            var api = new TwitchAPI();
            var auth = await server.Listen();
            var resp = await api.Auth.GetAccessTokenFromCodeAsync(auth.Code, TwitchAuth.TwitchClientSecret, TwitchAuth.TwitchRedirectUri, TwitchAuth.TwitchAppID);
            
            twitchKey = resp.AccessToken;

        }

        private static string AuthorizationCodeUrl(string clientId, string redirectUri, List<string> scopes)
        {
            var scopesStr = String.Join('+', scopes);

            return "https://id.twitch.tv/oauth2/authorize?" +
                   $"client_id={clientId}&" +
                   $"redirect_uri={System.Web.HttpUtility.UrlEncode(redirectUri)}&" +
                   "response_type=code&" +
                   $"scope={scopesStr}";
        }

        static void validate()
        {
            if (String.IsNullOrEmpty(TwitchAuth.TwitchAppID))
            {
                throw new ArgumentNullException("TwitchAppID is null or empty");
            }
            if (String.IsNullOrEmpty(TwitchAuth.TwitchClientSecret))
            {
                throw new ArgumentNullException("TwitchClientSecret is null or empty");
            }
            if (String.IsNullOrEmpty(TwitchAuth.TwitchRedirectUri))
            {
                throw new ArgumentNullException("TwitchRedirectUri is null or empty");
            }

        }

        public TwitchBot()
        {
            //Get credentials to log in to the desired bot account.
            //TODO: PLEASE for the love of god, don't hardcode this
            //This is just for testing purposes


            Authenticate().GetAwaiter().GetResult();

            ConnectionCredentials creds = new(TwitchAuth.chatUsername, twitchKey);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            //Get a websocket going for communication with Twitch's Helix API
            WebSocketClient wClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(wClient);
            client.Initialize(creds, TwitchAuth.streamerChannel, '!', '!', true);
            
            //Functions to define what the bot should do for the following events
            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnConnected += Client_OnConnected;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;


            //...and finally, we connect
            client.Connect();

        }

        private async void Client_OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
        {

            switch (e.Command.CommandText)
            {
                case "song":
                    client.SendMessage(TwitchAuth.streamerChannel, $"{SpotifyControl.songName} - {SpotifyControl.artistName}");
                    break;
                case "skip" when e.Command.ChatMessage.IsModerator:
                case "skip" when e.Command.ChatMessage.IsBroadcaster:
                    await SpotifyControl.Skip();
                    break;
                case "songreq":
                    await SongRequest(e);
                    break;
                case "so":
                    Shoutout(e.Command.ArgumentsAsList);
                    break;
            }
            
        }


        private void Client_OnConnected(object? sender, OnConnectedArgs e)
        {
            //Logs the connection to stdout
            //TODO: Log to file and make it all fancy~
            File.WriteAllText("log.log", $"Connected as {e.BotUsername} to {TwitchAuth.streamerChannel}");
        }

        private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            //throw new WarningException();
        }

        private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
        {
            //Sends a message confirming that indeed the bot is online
            string channelJoin = $"Connected to {e.Channel}";
            client.SendMessage(e.Channel, channelJoin);
        }

        private void Client_OnLog(object? sender, OnLogArgs e)
        {
            //mwehehe log ALL the things!!!111!111!!
            Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }


        private async Task SongRequest(OnChatCommandReceivedArgs e)
        {
            if (e.Command.ArgumentsAsString.Contains('-'))
            {
                string[] a = e.Command.ArgumentsAsString.Split('-');
                await SpotifyControl.Queue(a[0], a[1]); 
            }
            else if (e.Command.ArgumentsAsString.Contains(':'))
            { 
                await SpotifyControl.Queue(e.Command.ArgumentsAsString); 
            }
            client.SendMessage(TwitchAuth.streamerChannel, "Added song to queue successfully!");
        }

        private void Shoutout(List<string> e)
        {
            foreach(string i in e)
            {
                client.SendMessage(TwitchAuth.streamerChannel, $"Check out the lovely {i} over at https://twitch.tv/{i}!");
            }
        }
    }
}