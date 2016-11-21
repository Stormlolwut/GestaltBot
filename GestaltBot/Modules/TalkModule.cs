using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using GestaltBot.Enums;
using ChatterBotAPI;

using System;

namespace GestaltBot.Modules {
    class TalkModule : IModule {

        private ModuleManager m_manager;
        private DiscordClient m_client;

        void IModule.Install(ModuleManager manager) {

            Random random = new Random();

            m_manager = manager;
            m_client = manager.Client;

            //Lets you talkt to the bot without the command talk
            manager.CreateCommands("", cmd => {

                cmd.CreateCommand("say")
                .Alias("s")
                .Description("You can talk to him/her, whatever you prefer")
                .MinPermissions((int)DiscordAccesLevel.MemePeasant)
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) => {
                    ChatterBotFactory factory = new ChatterBotFactory();
                    
                    //Switch to cleverbot or pandorabot
                    ChatterBot cleverbot = factory.Create(ChatterBotType.CLEVERBOT);
                    ChatterBotSession cleverbotsession = cleverbot.CreateSession();

                    ChatterBot pandorabot = factory.Create(ChatterBotType.PANDORABOTS, "b0dafd24ee35a477");
                    ChatterBotSession pandorabotsession = pandorabot.CreateSession();

                    string s = e.Args[0];
                    s = cleverbotsession.Think(s);
                    await e.Channel.SendMessage(e.User.Mention + " | " + s);

                    //Console.WriteLine("bot2> " + s);
                    //s = cleverbotsession.Think(s);
                    //await e.Channel.SendMessage(s);


                });
            });
        }
    }
}
