using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules {
    class OverwatchModule : IModule {
        private ModuleManager m_manager;
        private DiscordClient m_client;


        void IModule.Install(ModuleManager manager) {

            m_manager = manager;
            m_client = manager.Client;
            manager.CreateCommands("", cmd => {

                cmd.CreateCommand("overwatch")
                .Alias("o")
                .Description("Check your overwatch stats")
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) => {

                    string input = e.Args[0];
                    string battletag = input.Replace('#', '-');

                    try {
                        await e.Channel.SendMessage("http://masteroverwatch.com/profile/pc/eu/" + battletag);
                    }
                    catch (Exception ex) {
                        await e.Channel.SendMessage(ex.ToString());
                    }

                });
            });
        }
    }
}
