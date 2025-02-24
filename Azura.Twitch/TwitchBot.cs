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

namespace Azura.Twitch
{ 
    class TwitchBot
    {
        // Fun stuff goes here

        

        string twitchID = File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(), "twitchkey.txt")).First();
        string twitchKey = File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(), "twitchkey.txt")).Last();
        string twitchChannel = File.ReadLines(Path.Combine(Directory.GetCurrentDirectory(), "twitchkey.txt")).ElementAt(1);


        TwitchClient client;
        

        public TwitchBot()
        {
            //Get credentials to log in to the desired bot account.
            //TODO: PLEASE for the love of god, don't hardcode this
            //This is just for testing purposes

            ConnectionCredentials creds = new(twitchID, twitchKey);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            //Get a websocket going for communication with Twitch's Helix API
            WebSocketClient wClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(wClient);
            client.Initialize(creds, twitchChannel, '!', '!', true);
            
            //Functions to define what the bot should do for the following events
            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnConnected += Client_OnConnected;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;


            //...and finally, we connect
            client.Connect();
            Console.WriteLine($"twitchChannel is {twitchChannel}");

        }

        private async void Client_OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
        {

            switch (e.Command.CommandText)
            {
                case "song":
                    client.SendMessage(twitchChannel, $"{SpotifyControl.songName} - {SpotifyControl.artistName}");
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
            File.WriteAllText("log.log", $"Connected as {e.BotUsername} to {twitchChannel}");
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
                client.SendMessage(twitchChannel, a[0] + a[1]);
            }
            else if (e.Command.ArgumentsAsString.Contains(':'))
            { 
                await SpotifyControl.Queue(e.Command.ArgumentsAsString); 
            }
            client.SendMessage(twitchChannel, "Added song to queue successfully!");
        }

        private void Shoutout(List<string> e)
        {
            foreach(string i in e)
            {
                client.SendMessage(twitchChannel, $"Check out the lovely {i} over at https://twitch.tv/{i}!");
            }
        }
    }
}