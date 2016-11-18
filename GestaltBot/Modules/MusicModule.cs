using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Audio;
using GestaltBot.Enums;

using NAudio;
using NAudio.FileFormats.Map;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GestaltBot.Modules {
    class MusicModule : IModule {

        private ModuleManager m_manager;
        private DiscordClient m_client;

        private Discord.Channel m_voiceChannel;
        private ReturnYoutubeInfo m_linkInfo;
        private Queue<string> m_songQueue = new Queue<string>();

        private bool m_isplaying;

        private IAudioClient m_audioClient;

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

                    m_voiceChannel = m_client.FindServers(servername).FirstOrDefault().FindChannels(channelname).FirstOrDefault();

                    try {

                        await GetYoutubeLink(e.Args[0]);
                        videos = m_linkInfo.videos;
                        
                        string url = await ReadOutLink(videos[0].ToString());
                 
                        VideoInfo video = await DownloadYoutubeLink(url);
                        
                        
                        if (videos == null && url.Count() == 0) {
                            await e.Channel.SendMessage(":no_entry:  | Sorry i couldn't find anything!");
                            return;
                        }
                        bool playingmusicloop = false;

                        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), RemoveIllegalPathCharacters(video.Title) + video.AudioExtension);
                        m_songQueue.Enqueue(path);
                        await e.Channel.SendMessage(":cd: | This song is queued: **" + RemoveIllegalPathCharacters(video.Title) + "**");

                        do {

                            if (!m_isplaying && m_songQueue.Count > 0) {

                                if (!playingmusicloop) {
                                    m_audioClient = await voiceservice.Join(m_voiceChannel);
                                    playingmusicloop = true;
                                }

                                path = m_songQueue.Peek();
                                await e.Channel.SendMessage(":cd: | Now playing: **" + RemoveIllegalPathCharacters(video.Title) + "**");
                                SendAudio(path, m_audioClient);
                                m_songQueue.Dequeue();
                            }
                            else if (!m_isplaying && m_songQueue.Count == 0) {
                                StopAudio(m_audioClient, voiceservice, e);
                                playingmusicloop = false;
                            }

                        }
                        while (playingmusicloop);
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

                        StopAudio(m_audioClient, voiceservice, e);
                    }
                    catch (Exception ex) {

                        Console.WriteLine(ex);
                        await e.Channel.SendMessage(":sos: | Uh oh, something went wrong!");
                    }

                });
            });

        }
        public void SendAudio(string filepath, IAudioClient voiceclient) {
            
            int channelcount = m_client.GetService<AudioService>().Config.Channels;
            WaveFormat outformat = new WaveFormat(48000, 16, channelcount);

            using (var MP3reader = new MediaFoundationReader(filepath))
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

                    m_isplaying = true;
                    voiceclient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                    
                }
                m_isplaying = false;
            }
        }

        public async void StopAudio(IAudioClient voiceclient, AudioService voiceservice, CommandEventArgs e) {
            if(voiceclient != null) {

                string servername = e.Server.Name;
                string channelname = e.Channel.Name;
                Discord.Channel voicechannel = m_client.FindServers(servername).FirstOrDefault().FindChannels(channelname).FirstOrDefault();

                m_isplaying = false;

                await voiceservice.Leave(voicechannel);
                voiceclient.Clear();
                await voiceclient.Disconnect();
                await e.Channel.SendMessage(":raised_hand: | See you later!");
            }
            else {
                await e.Channel.SendMessage(":japanese_goblin: | I am not in your channel you dumb fuck!");
            }
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

        public async Task<string> ReadOutLink(string video) {

            bool reading = false;

            foreach (char readchar in video.ToString()) {

                if (readchar == '(') {
                    video = "";
                    reading = true;
                }
                if (readchar == ')')
                    reading = false;

                if (reading && readchar != '(')
                    video += readchar.ToString();
                Console.WriteLine(video);
            }

            return video;
        }

        public async Task<VideoInfo> DownloadYoutubeLink(string url) {

            try {

                string fullurl = Path.Combine("https://www.youtube.com/watch?v=" + url);

                IEnumerable<VideoInfo> videoinfos = DownloadUrlResolver.GetDownloadUrls(fullurl,false);

                VideoInfo video = videoinfos
                    .OrderByDescending(info => info.AudioBitrate)
                    .First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 360);

                if (video.RequiresDecryption) { DownloadUrlResolver.DecryptDownloadUrl(video); }
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    RemoveIllegalPathCharacters(video.Title) + video.AudioExtension);
                if (File.Exists(path)) { return video; }

                VideoDownloader audiodownloader = new VideoDownloader(video, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                    RemoveIllegalPathCharacters(video.Title) + video.AudioExtension));

                    audiodownloader.DownloadProgressChanged += (sender, args) => Console.WriteLine(args.ProgressPercentage * 1);
                    audiodownloader.Execute();
                    return video;
                
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
                return null;
            }
        }
        private static string RemoveIllegalPathCharacters(string path) {
            string regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
            return r.Replace(path, "");
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