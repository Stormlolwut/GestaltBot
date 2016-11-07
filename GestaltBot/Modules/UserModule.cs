﻿using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using GestaltBot.Enums;


using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules {
    public class UserModule : IModule {

        private ModuleManager m_manager;
        private DiscordClient m_client;

        void IModule.Install(ModuleManager manager) {

            Random random = new Random();
            m_manager = manager;
            m_client = manager.Client;

            manager.CreateCommands("", cmd => {

                cmd.CreateCommand("commands")
                .Alias("c")
                .Description("Gives you all the commands")
                .Do(async (e) => {
                await e.User.SendMessage(
                        "**User Commands** \n" +
                        "```" +
                        " 1. !commands [Sends you a private message with all the commands]\n" +
                        " 2. !meme {searchword} [Finds a random image on google for you and posts it in the chat]\n" +
                        " 3. !say {what you want to say} [Talk with me about anything]\n" +
                        " 4. !play [Play like one amazing song once]" +
                        " 5. !stop [Disconnects from the server and gives out a free error]\n" +
                        " 6. !overwatch {yourbatletag#1234} [See your stats on overwatch]\n" + 
                        "```" +
                        "\n" +
                        "**Meme Knight** \n" +
                        "```" +
                        " 1. !prune {*1/100*} [Removes a certain amount of messages]\n" +
                        "```" +
                        "\n" +
                        "**Meme Lord**\n" +
                        "```" +
                        " 2. !announce {announcement} [Gives a private message to everyone on the guild] \n" +
                        "```" +
                        "\n" +
                        "**Meme King**\n" +
                        "```" +
                        " 3. !nsfwfilter {false/true} [Sets the nsfw filter for !meme on and or off]" +
                        "```"
                        );
                    await e.Channel.SendMessage(":eye_in_speech_bubble: | Commands send to you in private.");
                });

                cmd.CreateCommand("meme")
                .Alias("m")
                .Description("Let the bot do all the hard work for finding dank meme's")
                .MinPermissions((int)DiscordAccesLevel.MemePeasant)
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) => {
                    List<Channel> nsfwlist = ModeratorModule.m_allowedNSFW;
                    string text = e.Args[0];
                    string filter ="&safe=on";

                    for (int i = 0; i < ModeratorModule.m_allowedNSFW.Count; i++) {
                        if (e.Channel == nsfwlist[i]) {
                            filter = "&safe=off";
                        }
                    }

                    string html = GetHtmlLink(text, filter);
                    List<string> urls = ParseUrl(html);

                    if(urls.Count> 0) {

                        int ramdomMemeIndex = random.Next(urls.Count - 1);
                        string randomMeme = urls[ramdomMemeIndex];

                        await e.Channel.SendMessage(e.User.Mention + ":ok_hand: | Here is your fresh meme." + " [**" + text + "**] ");
                        await e.Channel.SendMessage(randomMeme);
                    }
                    else {
                        await e.Channel.SendMessage(e.User.Mention + ":no_entry: | I am sorry i couldnt find anything" + " [**" + text + "**] ");
                    }

                });

            });

        }

        private string GetHtmlLink(string topic, string filter) {

            //Move this to the moderator module within the NSFW filter instead of here!


            Console.WriteLine(filter);
            string url = "https://www.google.com/search?q=" + topic + filter + "&tbm=isch";
            string data = "";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";

            var response = (HttpWebResponse)request.GetResponse();

            using (Stream datastream = response.GetResponseStream()) {
                if (datastream == null)
                    return "";

                using (var sr = new StreamReader(datastream)) {
                    data = sr.ReadToEnd();
                    //Console.WriteLine(data);
                }
            }
            return data;
        }
        private List<string> ParseUrl(string html) {

            var urls = new List<string>();
            Console.WriteLine(urls.Count);

            int ndx = html.IndexOf("\"ou\"", StringComparison.Ordinal);

            while (ndx >= 0) {
                ndx = html.IndexOf("\"", ndx + 4, StringComparison.Ordinal);
                ndx++;
                int ndx2 = html.IndexOf("\"", ndx, StringComparison.Ordinal);
                string url = html.Substring(ndx, ndx2 - ndx);
                urls.Add(url);
                ndx = html.IndexOf("\"ou\"", ndx2, StringComparison.Ordinal);
            }

            return urls;
        }
    }

}
