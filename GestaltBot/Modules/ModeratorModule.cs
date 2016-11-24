﻿using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using GestaltBot.Enums;
using GestaltBot.Types;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules
{
    class ModeratorModule : IModule
    {

        public static List<Channel> m_allowedNSFW = new List<Channel>();

        private ModuleManager m_manager;
        private DiscordClient m_client;
        private Configurations m_config;
        const string accesconfig = "configurations.json";

        void IModule.Install(ModuleManager manager)
        {

            Random random = new Random();

            m_manager = manager;
            m_client = manager.Client;


            //Lets you talkt to the bot without the command talk
            manager.CreateCommands("", cmd =>
            {

                cmd.CreateCommand("prune")
                .Alias("p")
                .Description("Let's you mass remove messages on a channel")
                .MinPermissions((int)DiscordAccesLevel.MemeKnight)
                .Parameter("number", ParameterType.Unparsed)
                .Do(async (e) =>
                {

                    string number = e.Args[0];
                    int convertednum = Int32.Parse(number);

                    if (convertednum > 100)
                    {

                        await e.Channel.SendMessage(e.User.Mention + ":no_entry: | I can only remove 100 messages at a time.");
                        convertednum = 100;

                    }

                    Message[] messagestodelete = await e.Channel.DownloadMessages(convertednum);
                    await e.Channel.DeleteMessages(messagestodelete);
                    await e.Channel.SendMessage(e.User.Mention + ":white_check_mark: | I removed " + messagestodelete.Length + " messages for you.");
                });

                cmd.CreateCommand("pornparty")
                .Alias("pparty")
                .Description("Something that should not have been a thing")
                .MinPermissions((int)DiscordAccesLevel.MemeLord)
                .Parameter("number", ParameterType.Unparsed)
                .Do(async (e) =>
                {
                    string name = e.Args[0];

                    string webdata = await ReturnData();
                    List<string> url = GetLinks(webdata);
                    IEnumerable<User> targetuser = e.Channel.FindUsers(name);

                    await e.Channel.SendMessage(":eggplant: | Sending a suprise to " + targetuser.FirstOrDefault().Name);
                    SendPicturesAsync(targetuser, url);

                });

                cmd.CreateCommand("announce")
                .Alias("a")
                .Description("Sends a message to all the people in the discord server")
                .MinPermissions((int)DiscordAccesLevel.MemeLord)
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) =>
                {

                    List<User> markedforannounce = new List<User>();
                    string text = e.Args[0];
                    string announcer = e.User.Mention;

                    markedforannounce = (e.Server.Users.ToList<User>());
                    Console.WriteLine(markedforannounce.Count);
                    for (int i = 0; i < markedforannounce.Count; i++)
                    {
                        try
                        {
                            await markedforannounce[i].SendMessage(":satellite: | Good day, " + markedforannounce[i].Mention + " this is a announcement from: " + announcer + ": **[" + text + "]** ");
                        }
                        catch { }
                    }
                    await e.Channel.SendMessage(":alarm_clock: | Announcement complete :)");
                });

                cmd.CreateCommand("nsfwfilter")
                .Alias("filter")
                .Description("Allows/Disallows nsfw content in the chat room")
                .Parameter("bool", ParameterType.Unparsed)
                .MinPermissions((int)DiscordAccesLevel.MemeKing)
                .Do(async (e) =>
                {

                    bool boolconvert = Convert.ToBoolean(e.Args[0]);

                    if (boolconvert)
                    {
                        RemoveServerToNSFW(e);
                        await e.Channel.SendMessage(":underage:  | NSFW is now disallowed in this channel");
                    }
                    else
                    {
                        AddServerToNSFW(e);
                        await e.Channel.SendMessage(":spy: | NSFW is now allowed in this channel");
                    }
                });
            });
        }

        private bool m_isChannelIn;

        private async Task<string> ReturnData()
        {
            Random random = new Random();
            string url = "https://rule34.paheal.net/post/list/" + random.Next(1, 5);
            string data = "";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Accept = "text/html, application/xhtml+xml, */*";
                request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";

                var response = (HttpWebResponse)request.GetResponse();

                using (Stream datastream = response.GetResponseStream())
                {
                    if (datastream == null)
                        return "";

                    using (StreamReader sr = new StreamReader(datastream))
                    {
                        data = sr.ReadToEnd();
                        Console.WriteLine(data);
                    }
                }
            }
            catch
            {
                Console.WriteLine("404");
            }
            return data;
        }
        private List<string> GetLinks(string data)
        {
            List<string> allurls = new List<string>();
            int ndx = data.IndexOf("_images", StringComparison.Ordinal);

            //Console.WriteLine(ndx);
            while (ndx >= 0)
            {

                //Console.WriteLine(ndx);
                //ndx = data.IndexOf("\"", ndx, StringComparison.Ordinal);
                //Console.WriteLine(ndx);
                ndx++;
                int ndx2 = data.IndexOf("\"", ndx, StringComparison.Ordinal);
                //Console.WriteLine(ndx2);
                string url = data.Substring(ndx, ndx2 - ndx);
                string fullurl = "http://rule34-data-007.paheal.net/_" + url;
                //Console.WriteLine(fullurl);
                allurls.Add(fullurl);
                ndx = data.IndexOf("_images", ndx2, StringComparison.Ordinal);
                //Console.WriteLine(ndx);
            }
            return allurls;
        }
        private async void SendPicturesAsync(IEnumerable<User> user, List<string> url)
        {
            for (int i = 0; i < url.Count; i++)
            {
                await user.FirstOrDefault().SendMessage(url[i]);
                url.Remove(url[i]);
            }
        }
        private void AddServerToNSFW(CommandEventArgs e)
        {

            Channel channel = e.Channel;
            for (int i = 0; i < m_allowedNSFW.Count; i++)
            {
                if (m_allowedNSFW[i] == channel)
                {
                    m_isChannelIn = true;
                }
            }

            if (!m_isChannelIn)
            {
                m_allowedNSFW.Add(channel);
            }
            m_isChannelIn = false;
        }
        private void RemoveServerToNSFW(CommandEventArgs e)
        {

            Channel channel = e.Channel;
            for (int i = 0; i < m_allowedNSFW.Count; i++)
            {
                if (m_allowedNSFW[i] == channel)
                {
                    m_allowedNSFW.Remove(channel);
                }
            }
        }
    }
}

