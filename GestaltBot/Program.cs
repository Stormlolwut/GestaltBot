using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using GestaltBot.Enums;
using GestaltBot.Types;



using System;
using System.Security;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot
{
    class Program
    {
        static void Main(string[] args) => new Program().Start();

        private DiscordClient m_client;
        private Configurations m_config;

        public void Start()
        {

            const string accesconfig = "configurations.json";

            try
            {
                m_config = Configurations.LoadFile(accesconfig);
            }
            catch
            {

                m_config = new Configurations();

                Console.WriteLine("The example bot's configuration file has been created. Please enter a valid token.");
                Console.Write("Token: ");

                m_config.Token = Console.ReadLine();
                m_config.SaveFile(accesconfig);

            }

            m_client = new DiscordClient(x =>
            {

                x.AppName = "Gestalt";
                x.AppVersion = "0.0.1";
                x.LogLevel = LogSeverity.Info;

            })
            .UsingCommands(x =>
            {

                x.AllowMentionPrefix = m_config.MentionPrefix;
                x.PrefixChar = m_config.Prefix;
                x.HelpMode = HelpMode.Public;
            })
            .UsingPermissionLevels((u, c) => (int)GetPermissions(u, c))
            .UsingModules();

            m_client.Log.Message += (s, e) => Console.WriteLine($"[{e.Severity}] {e.Source}: {e.Message}");

            m_client.AddModule<Modules.UserModule>();
            m_client.AddModule<Modules.TalkModule>();
            m_client.AddModule<Modules.ModeratorModule>();
            m_client.AddModule<Modules.MusicModule>();
            m_client.AddModule<Modules.OverwatchModule>();

            m_client.ExecuteAndWait(async () =>
            {

                while (true)
                {
                    try
                    {
                        await m_client.Connect(m_config.Token, TokenType.Bot);
                        break;
                    }
                    catch (Exception ex)
                    {
                        m_client.Log.Error("Login Failed", ex);
                        await Task.Delay(m_client.Config.FailedReconnectDelay);
                    }
                }
            });
        }

        private void M_client_UserJoined(object sender, UserEventArgs e)
        {
            throw new NotImplementedException();
        }

        private DiscordAccesLevel GetPermissions(User user, Channel channel)
        {
            if (user.IsBot)
                return DiscordAccesLevel.Blocked;

            if (m_config.Owners.Contains(user.Id))
                return DiscordAccesLevel.BotOwner;

            if (!channel.IsPrivate)
            {
                if (user == channel.Server.Owner)
                    return DiscordAccesLevel.MemeKing;

                if (user.ServerPermissions.ManageServer)
                    return DiscordAccesLevel.MemeLord;

                if (user.ServerPermissions.MuteMembers)
                    return DiscordAccesLevel.MemeKnight;
            }

            return DiscordAccesLevel.MemePeasant;
        }

    }
}
