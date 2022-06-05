using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MeatForward
{
    /// <summary>
    /// snapshot settings
    /// </summary>
    [Flags]
    internal enum SnapshotMode : int
    {
        /// <summary>
        /// Save channel specific overrides
        /// </summary>
        SaveOverrides = 0b0001,
        /// <summary>
        /// Omitted roles and users should be stripped of all permissions
        /// </summary>
        ForceNullifyOmitted = 0b0010,
        /// <summary>
        /// Role and user excepts are treated as whitelist. Blacklist by default
        /// </summary>
        Whitelist = 0b0100,
        /// <summary>
        /// All options selected
        /// </summary>
        All = 0b0111
    }
}
