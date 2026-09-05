using System;
using System.Collections.Generic;
using System.Text;

namespace SYNC.Core
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
