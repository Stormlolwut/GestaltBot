using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Discord.Audio;
using GestaltBot.Enums;

using NAudio;
using NAudio.Wave;
using NAudio.CoreAudioApi;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestaltBot.Modules {
    class MusicModule : IModule {

        private ModuleManager m_manager;
        private DiscordClient m_client;


        void IModule.Install(ModuleManager manager) {

            m_manager = manager;
            m_client = manager.Client;
            IAudioClient m_audioClient;

            manager.CreateCommands("", cmd => {

                cmd.CreateCommand("play")
                .Alias("p")
                .Description("Play the music or sounds")
                .Do(async (e) => {

                    m_client.UsingAudio(x => {
                        x.Mode = AudioMode.Outgoing;
                    });

                    string servername = e.Server.Name;
                    string channelname = e.User.VoiceChannel.Name;
                    Channel voicechannel = m_client.FindServers(servername).FirstOrDefault().FindChannels(channelname).FirstOrDefault();

                    try {

                        AudioService voicservice = m_client.GetService<AudioService>();
                        m_audioClient = await voicservice.Join(voicechannel);
                        await e.Channel.SendMessage("| Good evenin' lads :raised_hand:");
                        string path = "C:/Users/storm/Documents/GitHub/GestaltBot/GestaltBot/Modules/Shia LaBeouf Live - Rob Cantor.mp3";
                        SendAudio(path, m_audioClient);
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex.ToString());
                    }
                });

                cmd.CreateCommand("stop")
                .Alias("st")
                .Description("Stops the music bot or sound")
                .Do(async (e) => {

                    AudioService audioservice = m_client.GetService<AudioService>();
                    //m_audioClient = audioservice.Client;
                    await audioservice.Leave(e.Server);        
                    await e.Channel.SendMessage("| Later :raised_hand:");
                });
            });

        }
        public void SendAudio(string filepath, IAudioClient voiceclient) {

            var channelCount = m_client.GetService<AudioService>().Config.Channels;
            var OutFormat = new WaveFormat(48000, 16, channelCount);
            
            using (var MP3reader = new Mp3FileReader(filepath))
            using (var resampler = new MediaFoundationResampler(MP3reader, OutFormat)) {
                resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                byte[] buffer = new byte[blockSize];
                int byteCount;

                while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0) // Read audio into our buffer, and keep a loop open while data is present
                {
                    if (byteCount < blockSize) {

                        for (int i = byteCount; i < blockSize; i++)
                            buffer[i] = 0;
                    }
                    voiceclient.Send(buffer, 0, blockSize); // Send the buffer to Discord
                }
            }
        }
    }
}

