using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RegionKit
{
	/// <summary>
	/// NOTE: notify methods registered in <see cref="RegionKit.Modules.Objects._Module"/>
	/// </summary>
	public interface INotifyWhenRoomIsViewed
	{
		/// <summary>
		/// Called when the room is viewed
		/// </summary>
		void RoomViewed();
		/// <summary>
		/// Called when the room is no longer viewed
		/// </summary>
		void RoomNoLongerViewed();
	}
}
