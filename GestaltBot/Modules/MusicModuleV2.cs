using System;
using System.Diagnostics;
//We use this because we can then use Async
using System.Threading;
//We use this because we can then create lists Queues etc
using System.Collections.Generic;
using System.Linq;
//We use this because we can then get the links from a youtube video
using System.Net;
//We use this because we can then execute external programs
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.Modules;
using Discord.Audio;

//We need this in order to download the videos from youtube
using YoutubeExtractor;
using WrapYoutubeDl;

namespace GestaltBot.Modules
{
    class MusicModuleV2 : IModule
    {
        //Creating the ModuleManager and DiscordClient variables
        private ModuleManager m_ModuleManager;
        private DiscordClient m_DiscordClient;

        //We create a list with Queues because the bot would now be able to play diffrent songs on diffrent servers
        private List<Queue<string>> m_AllServerQueues = new List<Queue<string>>();
        private List<Queue<SongData>> m_AllServerQueuesSongs = new List<Queue<SongData>>();

        private List<Discord.Server> m_AllMusicPlayingServers = new List<Server>();
        private List<bool> m_BotJoinedChannelInServer = new List<bool>();
        private List<bool> m_BotStartedPlayingMusicServer = new List<bool>();
        private List<ServerData> m_AllServerData = new List<ServerData>();

        private IAudioClient m_BotAudioClient;

        void IModule.Install(ModuleManager manager)
        {
            //Init of the module manager and the client;
            m_ModuleManager = manager;
            m_DiscordClient = manager.Client;

            //Enable that the bot can send audio in discord
            m_DiscordClient.UsingAudio(audio =>
            {
                audio.Mode = AudioMode.Outgoing;
            });

            //Get thet voiceservices where we are going to stream our music too
            AudioService voiceservice = m_DiscordClient.GetService<AudioService>(true);

            manager.CreateCommands("", cmd =>
            {

                //Create a command that will be executed by the bot
                cmd.CreateCommand("play").Alias("p").Description("will play the youtube link you send the bot").Parameter("youtbe link", ParameterType.Unparsed).Do(async (e) =>
                {
                    //Put the arguments after the command in a variable
                    string inputtedargs = e.Args[0];

                    try

                    {
                        if (m_AllServerData[CheckWichServer(e)].isinchannel == false)
                        {
                            Discord.Channel currentchannel = await CheckIfInVoiceChannelAsync(e);
                            m_BotAudioClient = await voiceservice.Join(currentchannel);
                            //m_BotJoinedChannelInServer.Add(true);
                            m_AllServerData.Add(new ServerData());
                            m_AllServerData[CheckWichServer(e)].isinchannel = true;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        Discord.Channel currentchannel = await CheckIfInVoiceChannelAsync(e);
                        m_BotAudioClient = await voiceservice.Join(currentchannel);
                        //m_BotJoinedChannelInServer.Add(true);
                        m_AllServerData.Add(new ServerData());
                        m_AllServerData[CheckWichServer(e)].isinchannel = true;
                    }

                    //bool downloaded = await DownloadYoutbeLinkAsync(inputtedargs, e);
                    bool awaitbool = await DownloadYoutubeLinkV2Async(inputtedargs, e);
                    SendAudioToDiscordBotAsync(e, voiceservice);

                });
                cmd.CreateCommand("skip").Description("Skips the current song to the next song in the queue").Do(async (e) =>
                {
                    await e.Channel.SendMessage("Skipping...");
                    m_AllServerData[CheckWichServer(e)].allowstream = false;

                });
                cmd.CreateCommand("join").Description("Lets the bot join your channel").Do(async (e) =>
                {
                    Discord.Channel currentchannel = await CheckIfInVoiceChannelAsync(e);
                    m_BotAudioClient = await voiceservice.Join(currentchannel);
                    //m_BotJoinedChannelInServer.Add(true);
                    m_AllServerData.Add(new ServerData());
                    m_AllServerData[CheckWichServer(e)].isinchannel = true;
                });
                cmd.CreateCommand("repeat").Description("Repeats current song").Do(async (e) =>
                {
                    try
                    {
                        if (m_AllServerData[CheckWichServer(e)].isrepeat == false)
                        {
                            m_AllServerData[CheckWichServer(e)].isrepeat = true;
                            await e.Channel.SendMessage(":curly_loop:| Song is on repeat!");
                        }
                        else
                        {
                            await e.Channel.SendMessage(":curly_loop:| Song is off repeat!");
                            m_AllServerData[CheckWichServer(e)].isrepeat = false;
                        }
                    }
                    catch
                    {
                        await e.Channel.SendMessage(":octagonal_sign:| Can't repeat right now");
                    }
                });

            });
        }

        /// <summary>
        /// Checks if the user that calls the play command is in a voice channel
        /// </summary>
        /// <param name="eventargs"></param>
        private async Task<Discord.Channel> CheckIfInVoiceChannelAsync(CommandEventArgs eventargs)
        {
            //Creation of the voicechannel that the bot wil join 
            Discord.Channel voicechannel;

            //Checking if the player is in the voice channel else give him a notice that he needs to join one
            try
            {
                voicechannel = m_DiscordClient.FindServers(eventargs.Server.Name).FirstOrDefault().FindChannels(eventargs.User.VoiceChannel.Name).FirstOrDefault();
                m_AllMusicPlayingServers.Add(eventargs.Server);
                return voicechannel;
            }
            catch
            {
                await eventargs.Channel.SendMessage(":japanese_goblin: | You need to be in a voice channel in order for me to join you");
                return null;
            }

        }

        private async Task<bool> DownloadYoutubeLinkV2Async(string url, CommandEventArgs eventargs)
        {

            int songcount = m_AllServerQueuesSongs.Count;
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "youtube-dl",
                Arguments = $"-o Downloads/{eventargs.Server.Name}/%(id)s.mp3 --write-info-json --yes-playlist --extract-audio --audio-format mp3 {url}",
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
            });
            process.WaitForExit();



            var jsonfiles = Directory.EnumerateFiles($@"Downloads/{eventargs.Server.Name}", "*.info.json");
            Directory.CreateDirectory(@"Downloads/" + eventargs.Server.Name);

            string[] jsonfilename = new string[jsonfiles.Count()];
            int filenameindex = 0;

            foreach (string jsonfile in jsonfiles)
            {
                jsonfilename[filenameindex] = jsonfile.Substring($@"Downloads/{eventargs.Server.Name}/".Length);
                filenameindex++;
            }

            for (int i = 0; i < jsonfilename.Length; i++)
            {
                string json = "";
                using (StreamReader sr = new StreamReader($"Downloads/{eventargs.Server.Name}/{jsonfilename[i]}"))
                {
                    json = sr.ReadToEnd();
                }

                File.Delete($"Downloads/{eventargs.Server.Name}/{jsonfilename[i]}");

                string[] taginfo = { "\"fulltitle\"", "\"webpage_url\"", "\"playlist\"" };
                for (int j = 0; j < taginfo.Length; j++)
                {
                    int ndx = json.IndexOf(taginfo[j], StringComparison.Ordinal);
                    int ndx2 = json.IndexOf("\"", ndx += taginfo[j].Length + 3, StringComparison.Ordinal);

                    taginfo[j] = json.Substring(ndx, ndx2 - ndx);
                }

                string[] songinfo = { eventargs.User.Name, $"Downloads/{eventargs.Server.Name}/{jsonfilename[i].Remove(jsonfilename[i].Length - 10)}" };

                m_AllServerQueuesSongs.Add(new Queue<SongData>());
                SongData sd = new SongData();

                sd.GetSongData(taginfo[0], taginfo[1], taginfo[2], songinfo[0], songinfo[1]);
                m_AllServerQueuesSongs[CheckWichServer(eventargs)].Enqueue(sd);
            }

            return true;

        }



        //private async Task<bool> DownloadYoutbeLinkAsync(string url, CommandEventArgs eventargs)
        //{

        //    //See if the word list apears in the url
        //    int listindex = url.IndexOf("list=");
        //    //Init of listdata for later use
        //    List<string> playlisturls = new List<string>();

        //    //Adds the link to the list in case when the link isnt a youtube music list
        //    playlisturls.Add(url);

        //    VideoInfo video = null;
        //    //Checks in what server you are in and puts that in an int
        //    int serverlocationint = CheckWichServer(eventargs);

        //    m_AllServerQueues.Add(new Queue<string>());
        //    //m_AllServerQueuesSongs.Add(new Queue<string>());

        //    //Checks if its a list or not
        //    if (listindex > 0)
        //    {
        //        //Remove the normal url because its a youtube playlist
        //        playlisturls.Remove(playlisturls[0]);
        //        //Gets all the links from the youtube playlist and stores them in a list
        //        playlisturls = AddPlaylistLinksToQueue(url);
        //    }

        //    foreach (string videolink in playlisturls)
        //    {
        //        try
        //        {
        //            //Get the video from the link and put it in the variable
        //            IEnumerable<VideoInfo> videoinfos = DownloadUrlResolver.GetDownloadUrls(videolink, false);

        //            video = videoinfos
        //                .OrderByDescending(info => info.AudioBitrate)
        //                .First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 720 || info.Resolution == 480 || info.Resolution == 360);

        //            if (video.RequiresDecryption) { DownloadUrlResolver.DecryptDownloadUrl(video); }

        //            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        //                RemoveIllegalPathCharacters(video.Title) + video.AudioExtension);

        //            if (!File.Exists((path.Replace(" ", ""))) && !File.Exists(path))
        //            {
        //                VideoDownloader audiodownloader = new VideoDownloader(video, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        //                RemoveIllegalPathCharacters(video.Title) + video.AudioExtension));

        //                //audiodownloader.DownloadProgressChanged += (sender, args) => Console.WriteLine(args.ProgressPercentage * 1);
        //                audiodownloader.Execute();
        //            }

        //        }
        //        catch
        //        {
        //            playlisturls.Remove(videolink);
        //        }

        //        string localpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), RemoveIllegalPathCharacters(video.Title) + video.AudioExtension);
        //        m_AllServerQueues[serverlocationint].Enqueue(localpath);

        //        //m_AllServerQueuesSongs[serverlocationint].Enqueue(eventargs.User.Name);
        //    }


        //    await eventargs.Channel.SendMessage(":minidisc:| There are: **" + m_AllServerQueues[serverlocationint].Count + "** songs enqueued");
        //    return true;
        //}

        /// <summary>
        /// Checks the server that you are on so you dont add it to someone else's playlist
        /// </summary>
        /// <param name="eventargs"></param>
        /// <returns></returns>
        private int CheckWichServer(CommandEventArgs eventargs)
        {

            for (int i = 0; i < m_AllMusicPlayingServers.Count;)
            {
                if (m_AllMusicPlayingServers[i] == eventargs.Server)
                    return i;
                i++;
            }

            return 0;
        }

        private async void SendAudioToDiscordBotAsync(CommandEventArgs eventargs, AudioService voiceclient)
        {

            bool startedplayingloop = false;

            int currentserver = CheckWichServer(eventargs);
            // m_BotStartedPlayingMusicServer.Add(false);

            do
            {
                //bool isplaying = m_BotStartedPlayingMusicServer[currentserver];
                bool isplaying = m_AllServerData[CheckWichServer(eventargs)].isplaying;

                if (!isplaying && m_AllServerQueuesSongs[currentserver].Count > 0)
                {

                    //string localpath = m_AllServerQueues[currentserver].Peek();
                    string localpath = m_AllServerQueuesSongs[currentserver].Peek().localpath;

                    //int ndx = m_AllServerQueues[currentserver].Peek().IndexOf("Documents", StringComparison.Ordinal);

                    //int ndx2 = m_AllServerQueues[currentserver].Peek().IndexOf(".", ndx, StringComparison.Ordinal);
                    //ndx += 9;

                    //Get the song title for later use
                    //string videotitle = m_AllServerQueues[currentserver].Peek().Substring(ndx, ndx2 - ndx);
                    string videotitle = m_AllServerQueuesSongs[currentserver].Peek().title;

                    //Send in the chat what song you are currently playing
                    await eventargs.Channel.SendMessage(":dvd: | Now playing: **" + RemoveIllegalPathCharacters(videotitle) + "**" + " Added by: " + "**" + m_AllServerQueuesSongs[currentserver].Peek().requestname/*m_AllServerQueuesSongs[currentserver].Peek()*/ + "**");

                    //await eventargs.Channel.SendFile($"{m_AllServerQueuesSongs[currentserver].Peek().tumbnailpath}");

                    //Sets the game of the discord bot to the title of the song
                    m_DiscordClient.SetGame(RemoveIllegalPathCharacters(videotitle));

                    if (!startedplayingloop)
                    {
                        startedplayingloop = true;
                    }

                    StreamAudioAsync(eventargs, localpath, isplaying);

                }
                else if (!isplaying && m_AllServerQueuesSongs[currentserver].Count == 0)
                {
                    m_AllServerData.RemoveAt(CheckWichServer(eventargs));
                    //m_BotJoinedChannelInServer.RemoveAt(CheckWichServer(eventargs));
                    //StopAudioAsync(m_audioClient, voiceservice, e);
                    startedplayingloop = false;
                }

            }
            while (startedplayingloop);

        }

        private async void StreamAudioAsync(CommandEventArgs eventargs, string localpath, bool playstream)
        {

            playstream = true;
            //bool musicplaying = m_BotStartedPlayingMusicServer[CheckWichServer(eventargs)];
            m_AllServerData[CheckWichServer(eventargs)].allowstream = true;
            m_AllServerData[CheckWichServer(eventargs)].isplaying = true;
            //musicplaying = true;

            /*if (!File.Exists(path))
                File.Move(localpath, path);
            else
            {
                File.Delete(localpath);
            }*/

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i  " + localpath + " " +
                            "-f s16le -ar 48000 -ac 2 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            });

            Thread.Sleep(2000);

            int blocksize = 3840;

            byte[] buffer = new byte[blocksize];
            int bytecount;

            while (playstream)
            {
                bytecount = process.StandardOutput.BaseStream
                    .Read(buffer, 0, blocksize);

                if (bytecount == 0 || m_AllServerData[CheckWichServer(eventargs)].allowstream == false)
                {
                    if (m_AllServerData[CheckWichServer(eventargs)].isrepeat == false)
                    {
                        m_AllServerQueuesSongs[CheckWichServer(eventargs)].Dequeue();
                    }
                    await eventargs.Channel.SendIsTyping();
                    m_AllServerData[CheckWichServer(eventargs)].isplaying = false;
                    break;
                }
                    m_BotAudioClient.Send(buffer, 0, bytecount);
            }

            m_BotAudioClient.Wait();
        }

        /// <summary>
        /// Gets all the available video links from the user inputted url and stores them all in a list
        /// </summary>
        /// <param name="inputurl"></param>
        /// <returns></returns>
        //private List<string> AddPlaylistLinksToQueue(string inputurl)
        //{
        //    //Create a variable that we are going to use for storing our website data;
        //    string websitedata = "";

        //    //Creating a try catch as a failsafe
        //    try
        //    {
        //        //Creating a http request so we can get all the data from our link
        //        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(inputurl);
        //        request.Accept = "text/html, application/xhtml+xml, */*";
        //        request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";

        //        //Store our data in the response variable
        //        var response = (HttpWebResponse)request.GetResponse();

        //        //Read out the response and store it in the websitedata string variable
        //        using (Stream datastream = response.GetResponseStream())
        //        {
        //            if (datastream == null)
        //                return null;

        //            using (StreamReader sr = new StreamReader(datastream))
        //            {
        //                websitedata = sr.ReadToEnd();
        //            }
        //        }

        //    }
        //    catch
        //    {
        //        //Writing in the console to tell that the request failed somehow
        //        Console.WriteLine("404");
        //        //"/watch?
        //    }


        //    //Create a temporary list that we are going to store all the links from the youtube playlist
        //    List<string> allurls = new List<string>();
        //    //Create a variable where we aare going to tell what word the index should search for
        //    int ndx = websitedata.IndexOf("\"/watch?", StringComparison.Ordinal);

        //    //Searches the playlist lenght from youtube so that we can use that later
        //    int playlistlenghtndx = websitedata.IndexOf("playlist-length", StringComparison.Ordinal);
        //    //Add 2 to get the right position
        //    playlistlenghtndx += 17;
        //    //Find the end int of the playlist
        //    int playlistlenghtendndx = websitedata.IndexOf("v", playlistlenghtndx, StringComparison.Ordinal);
        //    int playlistlenght = Int32.Parse(websitedata.Substring(playlistlenghtndx, (playlistlenghtendndx - 1) - playlistlenghtndx));

        //    //Get all the links from the httprequest we just created
        //    while (allurls.Count < playlistlenght)
        //    {
        //        ndx++;
        //        int ndx2 = websitedata.IndexOf("\"", ndx, StringComparison.Ordinal);
        //        string youtubeurl = websitedata.Substring(ndx, ndx2 - ndx);
        //        string fullurl = "https://youtube.com/" + youtubeurl;
        //        allurls.Add(fullurl);
        //        ndx = websitedata.IndexOf("\"/watch?", ndx2, StringComparison.Ordinal);
        //    }
        //    return allurls;
        //}

        /// <summary>
        /// A small function that removes certain chars from a string if asked
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string RemoveIllegalPathCharacters(string path)
        {
            string regexsearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            Regex regex = new Regex(string.Format("[{0}]", Regex.Escape(regexsearch)));
            return regex.Replace(path, "");
        }

    }

    public struct SongData
    {
        public string title;
        public string url;
        public string localpath;
        public string tumbnailpath;
        public string playlistname;
        public string requestname;

        public void GetSongData(string title, string url, string playlistname, string requestname, string localpath)
        {
            this.title = title;
            this.url = url;
            this.playlistname = playlistname;
            this.requestname = requestname;
            this.localpath = localpath;
        }

    }

    public class ServerData
    {
        public bool isplaying = false;
        public bool isinchannel = false;
        public bool isrepeat = false;
        public bool allowstream = false;

    }

}
