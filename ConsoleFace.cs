using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static MeatForward.UtilPile;

namespace MeatForward
{
    internal static class ConsoleFace
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">must be int32 enum</typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        internal static T cPromptFlags<T>(string msg) where T : struct, Enum
        {
            if (Enum.GetUnderlyingType(typeof(T)) != typeof(int)) throw new ArgumentException("enum must be an int32");
            int res = 0;
            Console.WriteLine(msg + " (choose options)");
            foreach (var val in Enum.GetValues(typeof(T)))
            {
                if (cPromptBinary($"{val}?")) res |= (int)val;
            }
            return (T)(object)res;
            //c# i hate you
        }
        internal static bool cPromptBinary(string message)
        {
            var r = cPrompt(message, new[] { "y", "n" });
            switch (r)
            {
                case "n": return false;
                case "y": return true;
            }
            return false;
        }

        internal static T cPromptEnum<T>(string msg)
            where T : struct, Enum
            => Enum.Parse<T>(cPrompt(msg, Enum.GetNames<T>()));
        internal static T cPrompt<T>(string msg, T[] options, bool byIndexToo = false)
        {
            if (options.Length == 0) throw new ArgumentException("Can not have zero choices");
            StringBuilder sb = new(msg);
            sb.Append(" (");
            foreach (var option in options) sb.Append((byIndexToo ? $"{option}[{Array.IndexOf(options, option)}]" : option) + "/");
            sb.Length--;
            sb.Append(")\n");
            Console.Write(sb.ToString());
        takeAnswer:
            var r = cPromptAny("> ");
            foreach (var option in options)
            {
                if (r == option?.ToString()) return option;
            }
            if (byIndexToo && int.TryParse(r, out var rind) && options.IndexInRange(rind)) return options[rind];
            Console.WriteLine("Invalid input");
            goto takeAnswer;
        }
        
        internal static string cPromptAny(string? msg = null)
        {
            if (msg is not null) Console.Write(msg);
            return Console.ReadLine() ?? string.Empty;
        }
    }
}
