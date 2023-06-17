namespace RegionKit.Modules.Effects;

/// <summary>
/// Sets the alpha of the bat hive color in the room
/// By LB/M4rbleL1ne
/// </summary>
public class HiveColorAlpha : UpdatableAndDeletable
{
	/// <inheritdoc cref="HiveColorAlpha"/>
	public HiveColorAlpha(Room room) => this.room = room;

	internal static void Apply() =>_CommonHooks.PostRoomLoad += PostRoomLoad;

	internal static void Undo() => _CommonHooks.PostRoomLoad -= PostRoomLoad;

	private static void PostRoomLoad(Room self)
	{
		for (var k = 0; k < self.roomSettings.effects.Count; k++)
		{
			if (self.roomSettings.effects[k].type == _Enums.HiveColorAlpha)
			{
				__logger.LogDebug($"HiveColorAlpha in room {self.abstractRoom.name}");
				self.AddObject(new HiveColorAlpha(self));
			}
		}
	}

	/// <summary>
	/// HiveColorAlpha Update method.
	/// </summary>
	public override void Update(bool eu)
	{
		base.Update(eu);
		if (room?.roomSettings is RoomSettings rs && rs.IsEffectInRoom(_Enums.HiveColorAlpha))
			Shader.SetGlobalFloat("_SwarmRoom", rs.GetEffectAmount(_Enums.HiveColorAlpha));
	}
}
