using Watcher;
using static RegionKit.Modules.FloatingDebrisNew.IFloaterExtraData;

namespace RegionKit.Modules.FloatingDebrisNew
{
	public static class _Extensions
	{
		public static T? GetExtraDataAt<T>(this FloatingDebris.Floater floater, int index) where T : IFloaterExtraData
		{
			return Implementation.GetFloaterDataAt<T>(floater.owner.data, index);
		}
	}
}
