using AndroGETrackerML.Model.Enum;
using BroadCapture;
using BroadCapture.Extensions;
using BroadCapture.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using RDapter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using Utilities.Shared;

namespace AndroGETracker
{
    public partial class CommandsHandler : BaseCommandModule
    {
        [RequireOwner]
        [Command("addchannel")]
        [Description("Add channel (owner only).")]
        public async Task AddChannel(CommandContext ctx, [RemainingText] ulong id)
        {
            Config.Instance.Discord_TextChannel_Id.Add(id);
            var channel = await ctx.Client.GetChannelAsync(id);
            if (channel != null)
            {
                Program.Channels.Add(channel);
                await ctx.RespondAsync($"Channel id {id} has been added.");
            }
            else
            {
                await ctx.RespondAsync("No channel found.");
            }
        }
        [RequireOwner]
        [Command("removechannel")]
        [Description("Remove channel (owner only).")]
        public async Task RemoveChannel(CommandContext ctx, [RemainingText] ulong id)
        {
            Config.Instance.Discord_TextChannel_Id.Add(id);
            var channel = Program.Channels.SingleOrDefault(x => x.Id == id);
            if (channel != null)
            {
                Program.Channels.Remove(channel);
                await ctx.RespondAsync($"Channel id {id} has been removed.");

            }
            else
            {
                await ctx.RespondAsync("No channel found.");
            }
        }
    }
}