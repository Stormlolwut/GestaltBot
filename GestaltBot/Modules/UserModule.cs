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

                cmd.CreateCommand("meme")
                .Description("Let the bot do all the hard work for finding dank meme's")
                .MinPermissions((int)DiscordAccesLevel.User)
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) => {

                    string text = e.Args[0];

                    string html = GetHtmlLink(text);
                    List<string> urls = ParseUrl(html);

                    int ramdomMemeIndex = random.Next(urls.Count - 1);
                    string randomMeme = urls[ramdomMemeIndex];

                    await e.Channel.SendMessage(e.User.Mention + " here is your fresh meme :tired_face: " + ":sweat_drops: " + " (" + text.ToUpper() + ") ");
                    await e.Channel.SendMessage(randomMeme);
                });


            });
        }

        private string GetHtmlLink(string topic) {

            string url = "https://www.google.com/search?q=" + topic + "&tbm=isch";
            string data = "";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "text/html, application/xhtml+xml, */*";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";

            var response = (HttpWebResponse)request.GetResponse();

            using (Stream dataStream = response.GetResponseStream()) {
                if (dataStream == null)
                    return "";

                using (var sr = new StreamReader(dataStream)) {
                    data = sr.ReadToEnd();
                }
            }
            return data;
        }
        private List<string> ParseUrl(string html) {

            var urls = new List<string>();

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