using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using GestaltBot.Enums;
using GestaltBot.Types;
using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules {
    class ModeratorModule : IModule{

        public static List<Channel> m_allowedNSFW = new List<Channel>();

        private ModuleManager m_manager;
        private DiscordClient m_client;
        private Configurations m_config;
        const string accesconfig = "configurations.json";

        void IModule.Install(ModuleManager manager) {

            Random random = new Random();

            m_manager = manager;
            m_client = manager.Client;
     

            //Lets you talkt to the bot without the command talk
            manager.CreateCommands("", cmd => {

                cmd.CreateCommand("prune")
                .Alias("p")
                .Description("Let's you mass remove messages on a channel")
                .MinPermissions((int)DiscordAccesLevel.MemeKnight)
                .Parameter("number", ParameterType.Unparsed)
                .Do(async (e) => {

                    string number = e.Args[0];
                    int convertednum = Int32.Parse(number);

                    if (convertednum > 100) {

                        await e.Channel.SendMessage(e.User.Mention + ":no_entry: | I can only remove 100 messages at a time.");
                        convertednum = 100;

                    }

                    Message[] messagestodelete = await e.Channel.DownloadMessages(convertednum);
                    await e.Channel.DeleteMessages(messagestodelete);
                    await e.Channel.SendMessage(e.User.Mention + ":white_check_mark: | I removed " + messagestodelete.Length + " messages for you.");
                });

                cmd.CreateCommand("announce")
                .Alias("a")
                .Description("Sends a message to all the people in the discord server")
                .MinPermissions((int)DiscordAccesLevel.MemeLord)
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) => {

                    List<User> markedforannounce = new List<User>();
                    string text = e.Args[0];
                    string announcer = e.User.Mention;

                    markedforannounce = (e.Server.Users.ToList<User>());
                    Console.WriteLine(markedforannounce.Count);
                    for (int i = 0; i < markedforannounce.Count; i++) {
                        try {
                            await markedforannounce[i].SendMessage(":satellite: | Good day, " + markedforannounce[i].Mention + " this is a announcement from: " + announcer + ": **[" + text + "]** ");
                        }
                        catch {}
                    }
                    await e.Channel.SendMessage(":alarm_clock: | Announcement complete :)");
                });

                cmd.CreateCommand("nsfwfilter")
                .Alias("filter")
                .Description("Allows/Disallows nsfw content in the chat room")
                .Parameter("bool", ParameterType.Unparsed)
                .MinPermissions((int)DiscordAccesLevel.MemeKing)
                .Do(async (e) => {

                    bool boolconvert = Convert.ToBoolean(e.Args[0]);

                    if (boolconvert) {
                        RemoveServerToNSFW(e);
                        await e.Channel.SendMessage(":underage:  | NSFW is now disallowed in this channel");
                    }
                    else {
                        AddServerToNSFW(e);
                        await e.Channel.SendMessage(":spy: | NSFW is now allowed in this channel");
                    }
                });
            });
        }

        private bool m_isChannelIn;
        private void AddServerToNSFW(CommandEventArgs e) {

            Channel channel = e.Channel;
            for (int i = 0; i < m_allowedNSFW.Count; i++) {
                if (m_allowedNSFW[i] == channel) {
                    m_isChannelIn = true;
                }
            }

            if (!m_isChannelIn) {
                m_allowedNSFW.Add(channel);
                Console.WriteLine(channel);
                Console.WriteLine(m_allowedNSFW.Count);
            }
            m_isChannelIn = false;
        }
        private void RemoveServerToNSFW(CommandEventArgs e) {

            Channel channel = e.Channel;
            for (int i = 0; i < m_allowedNSFW.Count; i++) {
                if(m_allowedNSFW[i] == channel) {
                    m_allowedNSFW.Remove(channel);
                }
            }
        }
    }
}

