using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Audio;
using GestaltBot.Enums;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

using YoutubeExtractor;


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules {
    class MusicModule : IModule {

        private ModuleManager m_manager;
        private DiscordClient m_client;
        private IAudioClient m_audioClient;
        private ReturnYoutubeInfo m_linkInfo;

        void IModule.Install(ModuleManager manager) {

            m_manager = manager;
            m_client = manager.Client;

            m_client.UsingAudio(x => {
                x.Mode = AudioMode.Outgoing;
            });

            AudioService voiceservice = m_client.GetService<AudioService>();

            manager.CreateCommands("", cmd => {

                cmd.CreateCommand("play")
                .Alias("p")
                .Description("Play the music or sounds")
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) => {

                    List<string> videos = new List<string>();
                    List<string> channels = new List<string>();
                    List<string> playlists = new List<string>();

                    string servername = e.Server.Name;
                    string channelname = e.User.VoiceChannel.Name;

                    try {

                        Discord.Channel voicechannel = m_client.FindServers(servername).FirstOrDefault().FindChannels(channelname).FirstOrDefault();

                        await GetYoutubeLink(e.Args[0]);
                        videos = m_linkInfo.videos;
                        Console.WriteLine(videos[0].ToString());


                        bool reading = false;
                        string url = "";
                        foreach(char readchar in videos.ToString()) {
                            if (readchar == '(') {
                                url = "";
                                reading = true;
                            }
                            if (readchar == ')')
                                reading = false;

                            if (reading)
                                url += readchar.ToString();
                        }

                        VideoInfo video = await DownloadYoutubeLink(url);
                       

                        if (videos != null && videos.Count != 0) {
                            Console.WriteLine("got it: " + videos.Count);
                        }
                        else {
                            await e.Channel.SendMessage(" | Sorry i couldn't find anything!");
                            return;
                        }

                        m_audioClient = await voiceservice.Join(voicechannel);
                        string path = Path.Combine("D:/downloads/", video.Title + video.AudioExtension);
                        SendAudio(path, m_audioClient);

                        await e.Channel.SendMessage("| Good evenin' lads :raised_hand:");
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                });

                cmd.CreateCommand("stop")
                .Alias("st")
                .Description("Stops the music bot or sound")
                .Do(async (e) => {

                    try {

                        string servername = e.Server.Name;
                        string channelname = e.User.VoiceChannel.Name;
                        Discord.Channel voicechannel = m_client.FindServers(servername).FirstOrDefault().FindChannels(channelname).FirstOrDefault();

                        StopAudio(m_audioClient);
                        await voiceservice.Leave(voicechannel);
                        await e.Channel.SendMessage("| Later :raised_hand:");
                    }
                    catch (Exception ex) {

                        Console.WriteLine(ex);
                        await e.Channel.SendMessage("| Uh oh something went wrong!");
                    }

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

        public void StopAudio(IAudioClient voiceclient) {
            voiceclient.Clear();
            voiceclient.Disconnect();
        }

        public async Task<bool> GetYoutubeLink(string searchargs) {

            YouTubeService youtubeservice = new YouTubeService(new BaseClientService.Initializer() {
                ApiKey = "AIzaSyALBS4vGHUM7KZM9J9qPHycecc4I4w0ffY",
                ApplicationName = GetType().ToString()
            });

            var searchrequest = youtubeservice.Search.List("snippet");
            searchrequest.Q = searchargs;

            SearchListResponse searchresponds = await searchrequest.ExecuteAsync();

            List<string> videos = new List<string>();
            List<string> channels = new List<string>();
            List<string> playlists = new List<string>();

            foreach (var searchresult in searchresponds.Items) {

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
            m_linkInfo = new ReturnYoutubeInfo(videos, channels, playlists);
            return true;
        }

        public async Task<VideoInfo> DownloadYoutubeLink(string url) {

            string fullurl = Path.Combine("www.youtube.com/" + url);

            Console.WriteLine(fullurl);

            IEnumerable<VideoInfo> videoinfos = DownloadUrlResolver.GetDownloadUrls(fullurl);

            VideoInfo video = videoinfos
                .Where(info => info.CanExtractAudio)
                .OrderBy(info => info.AudioBitrate)
                .First();

            if (video.RequiresDecryption) { DownloadUrlResolver.DecryptDownloadUrl(video); }

            AudioDownloader audiodownloader = new AudioDownloader(video, Path.Combine("D:/downloads/", video.Title + video.AudioExtension));

            audiodownloader.DownloadProgressChanged += (sender, args) => Console.WriteLine(args.ProgressPercentage * 0.85);
            audiodownloader.AudioExtractionProgressChanged += (sender, args) => Console.WriteLine(85 + args.ProgressPercentage * 0.15);

            return video;
        }
    }




    public struct ReturnYoutubeInfo {

        public List<string> videos;
        public List<string> channels;
        public List<string> playlists;

        public ReturnYoutubeInfo(List<string> videos, List<string> channels, List<string> playlists) {
            this.videos = videos;
            this.channels = channels;
            this.playlists = playlists;
        }
    }

}