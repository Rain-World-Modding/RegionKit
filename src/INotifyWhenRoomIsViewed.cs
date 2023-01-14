using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegionKit
{
    /// <summary>
    /// NOTE: notify methods registered in <see cref="MiscPO.MiscPOStatic"/>
    /// </summary>
    public interface INotifyWhenRoomIsViewed
    {
        void RoomViewed();
        void RoomNoLongerViewed();
    }
}
