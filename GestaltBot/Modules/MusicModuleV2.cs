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

//We need this for discord
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Audio;

//We need this in order to download the videos from youtube
using YoutubeExtractor;


namespace GestaltBot.Modules
{
    class MusicModuleV2 : IModule
    {
        //Creating the ModuleManager and DiscordClient variables
        private ModuleManager m_ModuleManager;
        private DiscordClient m_DiscordClient;

        //We create a list with Queues because the bot would now be able to play diffrent songs on diffrent servers
        private List<Queue<string>> m_AllServerQueues = new List<Queue<string>>();
        private List<Discord.Server> m_AllMusicPlayingServers = new List<Server>();

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
                    bool isplaying = false;

                    Discord.Channel currentchannel = await CheckIfInVoiceChannelAsync(e);
                    bool downloaded = await DownloadYoutbeLinkAsync(inputtedargs, e);

                    SendAudioToDiscordBot(e, voiceservice, currentchannel, isplaying);

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
            //Creation of the channel name that hte player is in that called the bot
            string channelname;

            //Checking if the player is in the voice channel else give him a notice that he needs to join one
            if (eventargs.User.VoiceChannel != null)
                channelname = eventargs.User.VoiceChannel.Name;
            else
            {
                await eventargs.Channel.SendMessage(":japanese_goblin: | You need to be in a voice channel in order for me to join you");
                return null;
            }

            //Finding the voice channel and store it in a variable 
            voicechannel = m_DiscordClient.FindServers(eventargs.Server.Name).FirstOrDefault().FindChannels(channelname).FirstOrDefault();
            m_AllMusicPlayingServers.Add(eventargs.Server);
            return voicechannel;

        }
        private async Task<bool> DownloadYoutbeLinkAsync(string url, CommandEventArgs eventargs)
        {

            //See if the word list apears in the url
            int listindex = url.IndexOf("list=");
            //Init of listdata for later use
            List<string> playlisturls = new List<string>();

            //Adds the link to the list in case when the link isnt a youtube music list
            playlisturls.Add(url);

            VideoInfo video = null;
            //Checks in what server you are in and puts that in an int
            int serverlocationint = CheckWichServer(eventargs);
            m_AllServerQueues.Add(new Queue<string>());
            //Checks if its a list or not
            if (listindex > 0)
            {
                //Remove the normal url because its a youtube playlist
                playlisturls.Remove(playlisturls[0]);
                //Gets all the links from the youtube playlist and stores them in a list
                playlisturls = AddPlaylistLinksToQueue(url);
            }

            foreach (string videolink in playlisturls)
            {
                try
                {
                    //Get the video from the link and put it in the variable
                    IEnumerable<VideoInfo> videoinfos = DownloadUrlResolver.GetDownloadUrls(videolink, false);

                    video = videoinfos
                        .OrderByDescending(info => info.AudioBitrate)
                        .First(info => info.VideoType == VideoType.Mp4 && info.Resolution == 720 || info.Resolution == 480 || info.Resolution == 360);

                    if (video.RequiresDecryption) { DownloadUrlResolver.DecryptDownloadUrl(video); }

                    string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        RemoveIllegalPathCharacters(video.Title) + video.AudioExtension);

                    if (!File.Exists((path.Replace(" ", ""))) && !File.Exists(path))
                    {
                        VideoDownloader audiodownloader = new VideoDownloader(video, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        RemoveIllegalPathCharacters(video.Title) + video.AudioExtension));

                        //audiodownloader.DownloadProgressChanged += (sender, args) => Console.WriteLine(args.ProgressPercentage * 1);
                        audiodownloader.Execute();
                    }

                }
                catch
                {
                    playlisturls.Remove(videolink);
                }

                string localpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), RemoveIllegalPathCharacters(video.Title) + video.AudioExtension);
                m_AllServerQueues[serverlocationint].Enqueue(localpath);
                Console.WriteLine(m_AllServerQueues[serverlocationint].Peek());
            }


            await eventargs.Channel.SendMessage(":minidisc:| There are: **" + m_AllServerQueues[serverlocationint].Count + "** songs enqueued");
            return true;
        }


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

        private async void SendAudioToDiscordBot(CommandEventArgs eventargs, AudioService voiceclient, Discord.Channel currentchannel, bool isplaying)
        {

            bool playingmusicloop = false;

            int currentserver = CheckWichServer(eventargs);

            do
            {

                if (!isplaying && m_AllServerQueues[currentserver].Count > 0)
                {

                    if (!playingmusicloop)
                    {
                        m_BotAudioClient = await voiceclient.Join(currentchannel);
                        playingmusicloop = true;
                    }

                    string localpath = m_AllServerQueues[currentserver].Peek();

                    int ndx = m_AllServerQueues[currentserver].Peek().IndexOf("Documents", StringComparison.Ordinal);
                    int ndx2 = m_AllServerQueues[currentserver].Peek().IndexOf(".", ndx, StringComparison.Ordinal);
                    ndx += 9;

                    //Get the song title for later use
                    string videotitle = m_AllServerQueues[currentserver].Peek().Substring(ndx, ndx2 - ndx);
                    //Send in the chat what song you are currently playing
                    await eventargs.Channel.SendMessage(":dvd: | Now playing: **" + RemoveIllegalPathCharacters(videotitle) + "**" + " Added by: " + "**" + eventargs.User.Name + "**");
                    //Sets the game of the discord bot to the title of the song
                    m_DiscordClient.SetGame(RemoveIllegalPathCharacters(videotitle));

                    StreamAudio(localpath, isplaying);
                    isplaying = true;

                    if (m_AllServerQueues[currentserver].Count > 0)
                        m_AllServerQueues[currentserver].Dequeue();
                }
                else if (!isplaying && m_AllServerQueues[currentserver].Count == 0)
                {
                    //StopAudioAsync(m_audioClient, voiceservice, e);
                    playingmusicloop = false;
                }

            }
            while (playingmusicloop);

        }

        private async void StreamAudio(string localpath, bool playstream)
        {
            string path = localpath.Replace(" ", "");

            playstream = true;

            if (!File.Exists(path))
                File.Move(localpath, path);

            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i  " + path + " " +
                            "-f s16le -ar 48000 -ac 2 pipe:" + m_AllMusicPlayingServers.Count,
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

                if (bytecount == 0 /*|| m_skipSong*/)
                {
                    /*m_skipSong = false;*/
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
        private List<string> AddPlaylistLinksToQueue(string inputurl)
        {
            //Create a variable that we are going to use for storing our website data;
            string websitedata = "";

            //Creating a try catch as a failsafe
            try
            {
                //Creating a http request so we can get all the data from our link
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(inputurl);
                request.Accept = "text/html, application/xhtml+xml, */*";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";

                //Store our data in the response variable
                var response = (HttpWebResponse)request.GetResponse();

                //Read out the response and store it in the websitedata string variable
                using (Stream datastream = response.GetResponseStream())
                {
                    if (datastream == null)
                        return null;

                    using (StreamReader sr = new StreamReader(datastream))
                    {
                        websitedata = sr.ReadToEnd();
                    }
                }

            }
            catch
            {
                //Writing in the console to tell that the request failed somehow
                Console.WriteLine("404");
                //"/watch?
            }

            //Create a temporary list that we are going to store all the links from the youtube playlist
            List<string> allurls = new List<string>();
            //Create a variable where we aare going to tell what word the index should search for
            int ndx = websitedata.IndexOf("\"/watch?", StringComparison.Ordinal);

            //Get all the links from the httprequest we just created
            while (ndx >= 0)
            {
                ndx++;
                int ndx2 = websitedata.IndexOf("\"", ndx, StringComparison.Ordinal);
                string youtubeurl = websitedata.Substring(ndx, ndx2 - ndx);
                string fullurl = "https://youtube.com/" + youtubeurl;
                allurls.Add(fullurl);
                ndx = websitedata.IndexOf("\"/watch?", ndx2, StringComparison.Ordinal);
            }
            return allurls;
        }

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
}
