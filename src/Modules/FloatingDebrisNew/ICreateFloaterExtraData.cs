using Watcher;

namespace RegionKit.Modules.FloatingDebrisNew
{
	/// <summary>
	/// Specifies that the floaters created by this <see cref="FloatingDebris.Floater.IFloaterSpawner"/> have additional data attached to it
	/// and defines a method to create the data class used by it.
	/// </summary>
	public interface ICreateFloaterExtraData
	{
		/// <summary>Identifier to associate with this floater data</summary>
		public string DataKeyword { get; }

		/// <summary>
		/// Factory to create a <see cref="IFloaterExtraData"/> implementation.
		/// This will be called per floating debris control point.
		/// </summary>
		/// <returns>The extra data implementation</returns>
		public IFloaterExtraData CreateData();
	}
}
