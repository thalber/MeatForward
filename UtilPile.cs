using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeatForward
{
    internal static class UtilPile
    {
        internal static bool IndexInRange<T>(this T[] arr, int index) => index > -1 && index < arr.Length;
        internal static ulong? getCatID (this Discord.WebSocket.SocketGuildChannel ch)
        {
            foreach (var cat in ch.Guild.CategoryChannels) if (cat.Channels.Contains(ch)) return cat.Id;
            return null;
        }
    }

    internal class MeatActivity : IActivity
    {
        internal MeatActivity()
        {

        }

        public string aname;
        public string desc;
        public ActivityType atype = ActivityType.Watching;
        public string Name => aname;

        public ActivityType Type => ActivityType.Watching;

        public ActivityProperties Flags => ActivityProperties.None;

        public string Details => desc;
    }
}
