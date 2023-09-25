namespace RegionKit.API;

using FadePalette = RoomSettings.FadePalette;
using Impl = RegionKit.Modules.Misc.MoreFadePalettes;

/// <summary>
/// Functionality for adding more than 1 fade palette to rooms.
/// </summary>
public static class MoreFadePalettes
{
	/// <summary>
	/// Returns all fade palettes registered for a room.
	/// </summary>
	public static IEnumerable<FadePalette> GetAllFadePalettes(RoomSettings roomSettings)
	{
        ThrowIfModNotInitialized();
		return Impl.GetAllFades(roomSettings);
	}
	/// <summary>
	/// Gets an additional fade palette at a specified index.
	/// </summary>
	/// <returns>Selected FadePalette; null if index out of range.</returns>
	public static FadePalette? GetExtraFadePalette(RoomSettings roomSettings, int index)
	{
        ThrowIfModNotInitialized();
		return Impl.GetMoreFade(roomSettings, index);
	}
	/// <summary>
	/// Adds a given additional palette to a specified index for a specified room.
	/// </summary>
	/// <param name="roomSettings"></param>
	/// <param name="index">Index to insert palette at. If this is past the extra palette list length, palette will be added at the end.</param>
	/// <param name="palette">New palette to be added.</param>
	public static void SetExtraFadePalette(RoomSettings roomSettings, int index, FadePalette palette)
	{
        ThrowIfModNotInitialized();
		Impl.SetMoreFade(roomSettings, index, palette);
	}
    /// <summary>
    /// Removes an extra palette at a given index.
    /// </summary>
	public static void DeleteExtraFadePalette(RoomSettings roomSettings, int index)
	{
        ThrowIfModNotInitialized();
		Impl.DeleteMoreFade(roomSettings, index);
	}
}