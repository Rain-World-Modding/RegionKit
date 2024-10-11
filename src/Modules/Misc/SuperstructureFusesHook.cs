namespace RegionKit.Modules.Misc;

/// <summary>
/// By Woodensponge and Slime_Cubed<br/>
/// Updated by Xan to append a render patch, solving the following issues:<br/>
/// <list type="bullet">
/// <item>SuperStructureFuses could not render on Tile Layer 1 (depth was too low, so the tiles drew over it and hid it)</item>
/// <item>SuperStructureFuses had the incorrect offset on Tile Layer 3 (depth was too high, and overshot the correct location)</item>
/// </list>
/// </summary>
internal static class SuperstructureFusesHook
{
	public static void Apply()
	{
		On.SuperStructureFuses.ctor += SuperStructureFusesCtor;
		On.SuperStructureFuses.InitiateSprites += OnInitiatingSprites;
		On.SuperStructureFuses.DrawSprites += OnDrawingSprites;
	}

	public static void Undo()
	{
		On.SuperStructureFuses.ctor -= SuperStructureFusesCtor;
		On.SuperStructureFuses.InitiateSprites -= OnInitiatingSprites;
		On.SuperStructureFuses.DrawSprites -= OnDrawingSprites;
	}

	private static void SuperStructureFusesCtor(On.SuperStructureFuses.orig_ctor orig, SuperStructureFuses self, PlacedObject placedObject, IntRect rect, Room room)
	{
		orig(self, placedObject, rect, room);
		if (room.world.region?.name is "ED" or "CM")
			self.broken = 0f;
	}

	private static void OnDrawingSprites(On.SuperStructureFuses.orig_DrawSprites originalMethod, SuperStructureFuses @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		const float LAYER_1_CAMERA_DEPTH = -4f;
		const float LAYER_2_CAMERA_DEPTH = 6f;
		const float LAYER_3_CAMERA_DEPTH = 12f; // Vanilla computed this as 16, which caused the offset to be too aggressive and misalign with layer 3.

		originalMethod(@this, sLeaser, rCam, timeStacker, camPos);

		// Compute new depth to override its current value.
		Vector2 thisPos = @this.pos;
		var cameraDepth = @this.depth switch
		{
			0 => LAYER_1_CAMERA_DEPTH,
			1 => LAYER_2_CAMERA_DEPTH,
			_ => LAYER_3_CAMERA_DEPTH,
		};
		int index = 0;
		for (int x = 0; x < @this.lights.GetLength(0); x++)
		{
			for (int y = 0; y < @this.lights.GetLength(1); y++)
			{
				Vector2 adjustedPosition = rCam.ApplyDepth(thisPos + new Vector2(5f + x * 10f, 5f + y * 10f), cameraDepth);
				sLeaser.sprites[index].x = adjustedPosition.x - camPos.x;
				sLeaser.sprites[index].y = adjustedPosition.y - camPos.y;
				index++;
			}
		}
	}
	private static void OnInitiatingSprites(On.SuperStructureFuses.orig_InitiateSprites originalMethod, SuperStructureFuses @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
	{
		const float LAYER_1_RENDER_DEPTH = 1f - (0.5f / 30f); // Vanilla had 1/30 not 0.5/30, which caused it to render too low and not show on Layer 1
		const float LAYER_2_RENDER_DEPTH = 1f - (11f / 30f);
		const float LAYER_3_RENDER_DEPTH = 1f - (21f / 30f);

		// Compute the sprite depth. This is the same as the vanilla behavior, save for layer 1.
		originalMethod(@this, sLeaser, rCam);
		var renderDepth = @this.depth switch
		{
			0 => LAYER_1_RENDER_DEPTH,
			1 => LAYER_2_RENDER_DEPTH,
			_ => LAYER_3_RENDER_DEPTH,
		};
		for (int index = 0; index < sLeaser.sprites.Length; index++)
		{
			sLeaser.sprites[index].alpha = renderDepth;
		}
	}

}
