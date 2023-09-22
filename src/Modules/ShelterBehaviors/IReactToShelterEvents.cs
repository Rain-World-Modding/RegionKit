
/// <summary>
/// Interface used to notify <see cref="UpdatableAndDeletable"/>s about shelter door related events. Notifications are issued by an instance of <see cref="RegionKit.Modules.ShelterBehaviors.ShelterBehaviorManager"/> in the room.
/// </summary>
public interface IReactToShelterEvents
{
	/// <summary>
	/// Notification about shelter closing/opening state.
	/// </summary>
	/// <param name="newFactor">New value of close/open factor; similar to <see cref="ShelterDoor.closedFac"/>.</param>
	/// <param name="closeSpeed">Current speed of doors closing.</param>
	void ShelterEvent(float newFactor, float closeSpeed);
}
