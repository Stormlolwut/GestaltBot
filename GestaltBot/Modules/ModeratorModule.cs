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

        private ModuleManager m_manager;
        private DiscordClient m_client;
        public static bool m_filterOn;


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



                cmd.CreateCommand("allowNSFW")
                .Alias("allow")
                .Description("Allows/Disallows nsfw content in the chat room")
                .Parameter("bool", ParameterType.Unparsed)
                .MinPermissions((int)DiscordAccesLevel.MemeKnight)
                .Do(async (e) => {

                    bool boolconvert = Convert.ToBoolean(e.Args[0]);
                    m_filterOn = boolconvert;
                    await e.Channel.SendMessage("NSFW filter set to: " + boolconvert);
                });
            });
        }
    }
}
