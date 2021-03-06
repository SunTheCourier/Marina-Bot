using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Marina.Attributes;
using Marina.Properties;
using Marina.Save;
using Marina.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Console = Marina.Utils.Console;

namespace Marina.Commands
{
    public class Client : ModuleBase<SocketCommandContext>
    {
        static Client()
        {
            Program.Initialize += delegate (object? sender, DiscordSocketClient client)
            {
                async Task UpdatePresence(SocketGuild arg)
                {
                    if (client.Guilds.Count > 1)
                        await client.SetGameAsync($"on {client.Guilds.Count} servers | m.help");
                    else
                        await client.SetGameAsync($"on {client.Guilds.Count} server | m.help");
                }

                client.Connected += async delegate
                {
                    if (client.Guilds.Count > 1)
                        await client.SetGameAsync($"on {client.Guilds.Count} servers | m.help");
                    else
                        await client.SetGameAsync($"on {client.Guilds.Count} server | m.help");
                };

                client.LeftGuild += UpdatePresence;
                client.JoinedGuild += UpdatePresence;
                //Clean Up
                client.LeftGuild += async delegate (SocketGuild guild)
                {
                    SaveHandler.LogSave.Remove(guild.Id);
                    foreach(ulong val in SaveHandler.LockdownSave[guild.Id])
                    {
                        SocketTextChannel channel = guild.GetTextChannel(val);
                        OverwritePermissions? permissions = channel.GetPermissionOverwrite(guild.EveryoneRole);

                        if (permissions.HasValue && permissions.Value.SendMessages == PermValue.Deny)
                        {
                            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole,
                                permissions.Value.Modify(sendMessages: PermValue.Inherit));
                        }
                    }
                };
            };
        }

        [Command("BanUser")]
        [RequireTeamOwnerAttributeManual]
        public async Task AddUserToBlacklist(IUser user)
        {
            if (!SaveHandler.BlacklistSave.Contains(user.Id))
            {
                SaveHandler.BlacklistSave.Add(user.Id);
                await ReplyAsync("User added to blacklist!");
            }
            else
            {
                await ReplyAsync("User already in blacklist!");
            }
        }

        [Command("UnbanUser")]
        [RequireTeamOwnerAttributeManual]
        public async Task RemoveUserFromBlacklist(IUser user)
        {
            if (SaveHandler.BlacklistSave.Remove(user.Id))
                await ReplyAsync("User removed from blacklist");
            else
                await ReplyAsync("User not added to blacklist!");
        }


        [Command("Announce")]
        [RequireTeamOwnerAttributeManual]
        public async Task Announce(params string[] announcement)
        {
            foreach (SocketGuild guild in Context.Client.Guilds)
                try
                {
                    await guild.Owner.SendMessageAsync(string.Join(" ", announcement));
                }
                catch
                {
                    // ignored
                }

            await ReplyAsync("Announcement sent to all Guild Owners! :ok_hand:");
        }

        [Command("Shutdown")]
        [Alias("Quit", "Shutoff", "Stop")]
        [RequireTeamOwnerAttributeManual]
        public async Task Shutdown()
        {
            await ReplyAsync("Shutting down!");
            await Context.Client.StopAsync();
            SaveHandler.SaveAll(false);
            await Console.WriteLog("***********************Shutdown via Command!***********************");
            Environment.Exit(0);
        }

        [Command("Say")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [HideCommand]
        public async Task ForceSay(params string[] str)
        {
            try
            {
                await Context.Message.DeleteAsync();
            }
            catch
            {
                // ignored
            }

            await ReplyAsync(string.Join(' ', str));
        }


        [Command("CSay")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [HideCommand]
        public async Task ChannelForceSay(SocketTextChannel channel, params string[] str)
        {
            try
            {
                await Context.Message.DeleteAsync();
            }
            catch
            {
                // ignored
            }

            await channel.SendMessageAsync(string.Join(' ', str));
        }

        [Command("Servers")]
        [RequireTeamOwnerAttributeManual]
        public async Task GetServers()
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle("Server List");
            builder.WithColor(Color.Teal);

            switch (Context.Client.Guilds.Count)
            {
                case 1:
                    builder.Description += "I am currently only in this server.";
                    break;
                default:
                    builder.Description += $"```{Context.Client.Guilds.Count} total servers:\n";
                    foreach (SocketGuild guild in Context.Client.Guilds)
                        builder.Description += $"{guild.Name}\n";

                    builder.Description += "```";
                    break;
            }

            if (builder.Length > EmbedBuilder.MaxDescriptionLength)
            {
                string[] msgs = Misc.ConvertToDiscordSendable(builder.Description);
                for (int i = 0; i < msgs.Length; i++)
                {
                    builder.Description = msgs[i];
                    if (msgs.Length - 1 == i)
                        builder.WithCurrentTimestamp();

                    await ReplyAsync(embed: builder.Build());
                    if (i == 0)
                        builder.Title = null;
                }
            }
            else
            {
                builder.WithCurrentTimestamp();
                await ReplyAsync(embed: builder.Build());
            }
        }

        [Command("Save")]
        [RequireTeamOwnerAttributeManual]
        public async Task ForceSave()
        {
            SaveHandler.SaveAll();
            await ReplyAsync("Saved all data!");
        }

        [Command("Version")]
        [Alias("Commit")]
        [Summary("Revision Number")]
        public async Task GetVersion() =>
            await ReplyAsync($"Git Commit: {Encoding.UTF8.GetString(Resources.CurrentCommit)}");

        [Command("Info"), Alias("Invite", "Source", "Vote")]
        [Summary("Bot information!")]
        public async Task GetInfo() =>
            await ReplyAsync("You can find my voting, source, and invite links here: https://link.sunthecourier.net/marina-bot");

        [Command("SetMode")]
        [RequireTeamOwnerAttributeManual]
        public async Task SetPriorityMode(ProcessPriorityClass mode)
        {
            using Process p = Process.GetCurrentProcess();
            p.PriorityClass = mode;
            await ReplyAsync($"Set priority to `{p.PriorityClass}`");
        }

        [Command("Mode")]
        [RequireTeamOwnerAttributeManual]
        public async Task GetPriorityMode()
        {
            using Process p = Process.GetCurrentProcess();
            await ReplyAsync($"Priority is set to `{p.PriorityClass}`");
        }
    }
}