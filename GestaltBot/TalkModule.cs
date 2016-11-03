using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using GestaltBot.Enums;
using ChatterBotAPI;

using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules {
    class TalkModule : IModule {

        private ModuleManager m_manager;
        private DiscordClient m_client;

        void IModule.Install(ModuleManager manager) {

            Random random = new Random();

            m_manager = manager;
            m_client = manager.Client;
            
            manager.CreateCommands("", cmd => {

                cmd.CreateCommand("talk")
                .Alias("t")
                .Description("Let the bot do all the hard work for finding dank meme's")
                .MinPermissions((int)DiscordAccesLevel.MemePeasant)
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) => {
                    ChatterBotFactory factory = new ChatterBotFactory();

                    ChatterBot bot1 = factory.Create(ChatterBotType.CLEVERBOT);
                    ChatterBotSession bot1session = bot1.CreateSession();

                    ChatterBot bot2 = factory.Create(ChatterBotType.PANDORABOTS, "b0dafd24ee35a477");
                    ChatterBotSession bot2session = bot2.CreateSession();

                    string s = e.Args[0];
                    while (true) {

                        Console.WriteLine("bot1> " + s);

                        s = bot2session.Think(s);
                        await e.Channel.SendMessage(s);

                        Console.WriteLine("bot2> " + s);

                        s = bot1session.Think(s);
                        await e.Channel.SendMessage(s);
                    }

                });
            });
        }
    }
}
