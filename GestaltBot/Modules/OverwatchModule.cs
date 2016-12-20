using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;

using System;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules
{
    class OverwatchModule : IModule
    {
        private ModuleManager m_manager;
        private DiscordClient m_client;


        void IModule.Install(ModuleManager manager)
        {

            m_manager = manager;
            m_client = manager.Client;
            manager.CreateCommands("", cmd =>
            {

                cmd.CreateCommand("overwatch")
                .Alias("ow")
                .Description("Check your overwatch stats")
                .Parameter("text", ParameterType.Unparsed)
                .Do(async (e) =>
                {

                    string input = e.Args[0];
                    string battletag = input.Replace('#', '-');

                    string returneddata = await GetWebsiteDataAsync(battletag);
                    Console.WriteLine(returneddata);

                    try
                    {
                        await e.Channel.SendMessage(e.User.Mention + " | Here are your stats.");
                        await e.Channel.SendMessage("http://masteroverwatch.com/profile/pc/eu/" + battletag);
                    }
                    catch 
                    {
                        await e.Channel.SendMessage("| Sorry i could't find anything!");
                    }

                });
            });
        }

        private async Task<string> GetWebsiteDataAsync(string battletag)
        {

            string url = "http://masteroverwatch.com/profile/pc/eu/" + battletag;
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
        private List <string> GetfavoriteHeroInfo(string data)
        {
            List<string> allurls = new List<string>();
            int ndx = data.IndexOf("summary-hero-name", StringComparison.Ordinal);

            //Console.WriteLine(ndx);
            while (ndx >= 0)
            {

                //Console.WriteLine(ndx);
                //ndx = data.IndexOf("\"", ndx, StringComparison.Ordinal);
                //Console.WriteLine(ndx);
                ndx += 2;
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
    }

    }
}
