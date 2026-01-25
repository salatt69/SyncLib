using System;
using System.Collections.Generic;
using System.Text;

namespace SyncLib.Core
{
    internal enum SyncType : byte
    {
        Beat,
        Bar,
        Grid,
        Entry,
        Exit,
        CustomBar,
    }
}
