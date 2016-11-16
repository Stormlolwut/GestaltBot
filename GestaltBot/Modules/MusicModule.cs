using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Audio;
using GestaltBot.Enums;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules {
    class MusicModule : IModule {

        private ModuleManager m_manager;
        private DiscordClient m_client;


        void IModule.Install(ModuleManager manager) {

            m_manager = manager;
            m_client = manager.Client;
            IAudioClient m_audioClient;

            manager.CreateCommands("", cmd => {

                cmd.CreateCommand("play")
                .Alias("p")
                .Description("Play the music or sounds")
                .Do(async (e) => {

                    m_client.UsingAudio(x => {
                        x.Mode = AudioMode.Outgoing;
                    });

                    string servername = e.Server.Name;
                    string channelname = e.User.VoiceChannel.Name;
                    Discord.Channel voicechannel = m_client.FindServers(servername).FirstOrDefault().FindChannels(channelname).FirstOrDefault();

                    try {

                        AudioService voicservice = m_client.GetService<AudioService>();
                        m_audioClient = await voicservice.Join(voicechannel);
                        await e.Channel.SendMessage("| Good evenin' lads :raised_hand:");
                        string path = "C:/Users/storm/Documents/GitHub/GestaltBot/GestaltBot/Modules/Shia LaBeouf Live - Rob Cantor.mp3";
                        GetYoutubeLink(e.Args[0]);
                        SendAudio(path, m_audioClient);
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                });

                cmd.CreateCommand("stop")
                .Alias("st")
                .Description("Stops the music bot or sound")
                .Do(async (e) => {

                    AudioService audioservice = m_client.GetService<AudioService>();
                    //m_audioClient = audioservice.Client;
                    await audioservice.Leave(e.Server);
                    await e.Channel.SendMessage("| Later :raised_hand:");
                });
            });

        }
        public void SendAudio(string filepath, IAudioClient voiceclient) {

            int channelcount = m_client.GetService<AudioService>().Config.Channels;
            WaveFormat outformat = new WaveFormat(48000, 16, channelcount);

            using (var MP3reader = new Mp3FileReader(filepath))
            using (var resampler = new MediaFoundationResampler(MP3reader, outformat)) {
                resampler.ResamplerQuality = 60;
                int blockSize = outformat.AverageBytesPerSecond / 50;
                byte[] buffer = new byte[blockSize];
                int byteCount;

                while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) {
                    if (byteCount < blockSize) {

                        for (int i = byteCount; i < blockSize; i++)
                            buffer[i] = 0;
                    }
                    voiceclient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                }
            }
        }
        public async void GetYoutubeLink(string searchargs) {

            YouTubeService youtubeservice = new YouTubeService(new BaseClientService.Initializer() {
                ApiKey = "AIzaSyALBS4vGHUM7KZM9J9qPHycecc4I4w0ffY",
                ApplicationName = GetType().ToString()
            });

            var searchrequest = youtubeservice.Search.List("snippet");
            searchrequest.Q = searchargs;

            SearchListResponse searchresponds =  await searchrequest.ExecuteAsync(); 

            List<string> videos = new List<string>();
            List<string> channels = new List<string>();
            List<string> playlists = new List<string>();

            foreach(var searchresult in searchresponds.Items) {

                switch (searchresult.Id.Kind) {
                    case "youtube#video":
                        videos.Add(string.Format("{0} ({1})", searchresult.Snippet.Title, searchresult.Id.VideoId));
                        break;
                    case "youtube#channel":
                        channels.Add(string.Format("{0} ({1})", searchresult.Snippet.Title, searchresult.Id.ChannelId));
                        break;
                    case "youtube#playlist":
                        playlists.Add(string.Format("{0}({1})", searchresult.Snippet.Title, searchresult.Id.PlaylistId));
                        break;
                }
            }
            Console.WriteLine(string.Format("Videos:\n{0}\n", string.Join("\n", videos)));
            Console.WriteLine(string.Format("Channels:\n{0}\n", string.Join("\n", channels)));
            Console.WriteLine(string.Format("Playlists:\n{0}\n", string.Join("\n", playlists)));
        }
    }
}

