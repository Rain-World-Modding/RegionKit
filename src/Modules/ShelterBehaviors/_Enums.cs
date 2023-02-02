namespace RegionKit.Modules.ShelterBehaviors;
///<inheritdoc/>
public static class _Enums
{
	/// <summary>
	/// Signleton POM object for customizing shelter behaviors. required in a room for others to work
	/// </summary>
	public static readonly PlacedObject.Type ShelterBhvrManager = new("ShelterBhvrManager", true);
	/// <summary>
	/// An additional door
	/// </summary>
	public static readonly PlacedObject.Type ShelterBhvrPlacedDoor = new("ShelterBhvrPlacedDoor", true);
	/// <summary>
	/// A zone where player can sleep
	/// </summary>
	public static readonly PlacedObject.Type ShelterBhvrTriggerZone = new("ShelterBhvrTriggerZone", true);
	/// <summary>
	/// A zone where player can't sleep
	/// </summary>
	/// <returns></returns>
	public static readonly PlacedObject.Type ShelterBhvrNoTriggerZone = new("ShelterBhvrNoTriggerZone", true);
	/// <summary>
	/// Shows HoldToTrigger tutorial
	/// </summary>
	/// <returns></returns>
	public static readonly PlacedObject.Type ShelterBhvrHTTTutorial = new("ShelterBhvrHTTTutorial", true);
	/// <summary>
	/// An additional spawn position for the shelter
	/// </summary>
	/// <returns></returns>
	public static readonly PlacedObject.Type ShelterBhvrSpawnPosition = new("ShelterBhvrSpawnPosition", true);
}
