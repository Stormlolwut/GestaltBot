using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using GestaltBot.Enums;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules {
    class ModeratorModule : IModule {


        public static List<Channel> m_allowedNSFW = new List<Channel>();

        private ModuleManager m_manager;
        private DiscordClient m_client;


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

                        await e.Channel.SendMessage(e.User.Mention + " | I can only remove 100 messages at a time. :no_entry: ");
                        convertednum = 100;

                    }

                    Message[] messagestodelete = await e.Channel.DownloadMessages(convertednum);
                    await e.Channel.DeleteMessages(messagestodelete);
                    await e.Channel.SendMessage(e.User.Mention + " | I removed " + messagestodelete.Length + " messages for you :white_check_mark: ");
                });

                cmd.CreateCommand("announce")
                .Alias("a")
                .Description("Sends a message to all the people in the discord server")
                .MinPermissions((int)DiscordAccesLevel.MemeKnight)
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) => {

                    List<User> markedforannounce = new List<User>();
                    string text = e.Args[0];
                    string announcer = e.User.Mention;

                    for (int i = 0; i < e.Server.UserCount; i++) {
                        markedforannounce = e.Server.Users.ToList<User>();
                    }

                    for (int i = 0; i < markedforannounce.Count; i++) {
                        await markedforannounce[i].SendMessage(":satellite: Good day, " + markedforannounce[i].Mention + " this is a announcement from: " + announcer + ": **[" + text + "]** ");
                    }

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
                        await e.Channel.SendMessage("NSFW is now disallowed in this channel");
                    }
                    else {
                        AddServerToNSFW(e);
                        await e.Channel.SendMessage("NSFW is now allowed in this channel :smirk: ");
                    }
                });
            });
        }

        private bool m_isChannelIn;
        private void AddServerToNSFW(CommandEventArgs e) {

            Channel channel = e.Channel;
            for (int i = 0; i < m_allowedNSFW.Count; i++) {
                if(m_allowedNSFW[i] == channel) {
                    m_isChannelIn = true;
                }
            }

            if (!m_isChannelIn) {
                Console.WriteLine(channel);
                Console.WriteLine(m_allowedNSFW.Count);
                m_allowedNSFW.Add(channel);
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

