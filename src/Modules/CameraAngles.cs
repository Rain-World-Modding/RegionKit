using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace RegionKit.Modules
{
	/// <summary>
	/// Retrieves camera angles from the room, if rendered in Community Editor or Drizzle above I forgot what version number it was for both.
	/// Leaving as internal for now just in case
	/// </summary>
	internal static class CameraAngles
	{
		private static readonly ConditionalWeakTable<Room, CameraAngle[]?> cangleCWT = new();

		private static CameraAngle? GetAngle(Room room, int camera)
		{
			if (!cangleCWT.TryGetValue(room, out var cangleBox))
			{
				CameraAngle? angle = null;

				// Read file
				string path = WorldLoader.FindRoomFile(WorldLoader.RoomNameManipulator(room.abstractRoom.FileName, room.game), false, ".txt", true);
				if (path != null && File.Exists(path))
				{
					const string IDENTIFIER = "camera angles:";
					string[] lines = File.ReadAllLines(path);
					string? cangleLine = lines.FirstOrDefault(x => x.StartsWith(IDENTIFIER));
					if (cangleLine != null)
					{
						// Extract camera angles from line
						string[] cangleInfos = cangleLine[IDENTIFIER.Length..].Trim().Split('|');
						CameraAngle[] angles = new CameraAngle[cangleInfos.Length];

						LogDebug("FOUND CANGLES FOR ROOM " + room.abstractRoom.FileName);
						for (int i = 0; i < cangleInfos.Length; i++)
						{
							Vector2[] rawAngles = [.. cangleInfos[i].Split(';')
								.Select(x => x.Split(','))
								.Select(x => Custom.DegToVec(float.Parse(x[0], CultureInfo.InvariantCulture)) * float.Parse(x[1], CultureInfo.InvariantCulture))];
							angles[i] = new CameraAngle
							{
								tl = rawAngles[0],
								tr = rawAngles[1],
								br = rawAngles[2],
								bl = rawAngles[3]
							};
							LogDebug(angles[i]);
						}

						// Register and return
						cangleCWT.Add(room, angles);
						return angles[camera];
					}
				}

				// If could not find or parse cangles, then we use null to show that
				if (angle == null)
				{
					LogDebug("NO CANGLES FOUND FOR ROOM " + room.abstractRoom.FileName);
					cangleCWT.Add(room, null);
				}

				// Return
				return angle;
			}

			return cangleBox?[camera];
		}

		public static Vector2 ApplyDepthWithCangle(this RoomCamera self, Vector2 pos, float depth)
		{
			// Try to get angle
			CameraAngle? maybeAngle = GetAngle(self.room, self.currentCameraPosition);
			if (!maybeAngle.HasValue)
			{
				return self.ApplyDepth(pos, depth);
			}
			CameraAngle angle = maybeAngle.Value;

			// Apply angle stretch (from renderLightStart.lingo)
			Vector2 renderSize = new Vector2(1500f, 900f);
			Vector2 posOnCamera = pos - self.CamPos(self.currentCameraPosition);
			Vector2 edgePad = renderSize - self.game.rainWorld.options.ScreenSize;
			float xLerp = Mathf.InverseLerp(0f, renderSize.x, posOnCamera.x - edgePad.x / 2f);
			float yLerp = Mathf.InverseLerp(0f, renderSize.y, posOnCamera.y - edgePad.y / 2f);
			Vector2 topLerp = Vector2.Lerp(angle.tl, angle.tr, xLerp);
			Vector2 btmLerp = Vector2.Lerp(angle.bl, angle.br, xLerp);
			Vector2 sizePad = new Vector2(1400f, 800f) - self.game.rainWorld.options.ScreenSize;
			Vector2 depthPnt = Custom.ApplyDepthOnVector(posOnCamera, new Vector2(1400f / 2f, 800f * 2f / 3f) - sizePad / 2f, depth);

			return Vector2.Lerp(btmLerp, topLerp, yLerp) * (depth - 5f) * 1.5f * 2.5f + pos + (depthPnt - posOnCamera);
		}

		private struct CameraAngle
		{
			public Vector2 tl;
			public Vector2 tr;
			public Vector2 br;
			public Vector2 bl;

			public override readonly string ToString()
			{
				return $"CANGLE {{\n  TL: ({tl.x}, {tl.y})\n  TR: ({tr.x}, {tr.y})\n  BR: ({br.x}, {br.y})\n  BL: ({bl.x}, {bl.y})\n}}";
			}
		}
	}
}
