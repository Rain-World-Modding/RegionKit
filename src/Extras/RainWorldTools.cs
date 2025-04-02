namespace RegionKit.Extras;

public static class RainWorldTools
{
	/// <summary>
	/// Current RainWorld instance. Uses Unity lookup, may be slow.
	/// </summary>
	public static RainWorld CRW
		=> UnityEngine.Object.FindObjectOfType<RainWorld>();
	/// <summary>
	/// Gets a <see cref="StaticWorld"/> template object by type.
	/// </summary>
	/// <param name="t"></param>
	/// <returns></returns>
	public static CreatureTemplate GetCreatureTemplate(CreatureTemplate.Type t)
	{
		return StaticWorld.creatureTemplates[(int)t];
	}
	/// <summary>
	/// Finds specified subprocess in ProcessManager (looks at both mainloop and side processes).
	/// </summary>
	/// <typeparam name="T">Type of subprocess.</typeparam>
	/// <param name="manager">must not be null.</param>
	/// <returns>Found subprocess; null if none.</returns>
	public static T? FindSubProcess<T>(this ProcessManager manager)
		where T : MainLoopProcess
	{
		BangBang(manager, nameof(manager));
		if (manager.currentMainLoop is T tmain) return tmain;
		foreach (MainLoopProcess sideprocess in manager.sideProcesses) if (sideprocess is T tside) return tside;
		return null;
	}
	/// <summary>
	/// Attempts to find an <see cref="UpdatableAndDeletable"/> of specified type
	/// </summary>
	public static T? FindUpdatableAndDeletable<T>(this Room rm)
	{
		BangBang(rm, nameof(rm));
		for (int i = 0; i < rm.updateList.Count; i++)
		{
			if (rm.updateList[i] is T t) return t;
		}
		return default;
	}
	/// <summary>
	/// Yields all <see cref="UpdatableAndDeletable"/>s of specified type.
	/// </summary>
	public static IEnumerable<T> FindAllUpdatableAndDeletable<T>(this Room rm)
	{
		for (int i = 0; i < rm.updateList.Count; i++)
		{
			if (rm.updateList[i] is T t) yield return t;
		}
	}

	internal static D? FindPlacedObjectData<D>(this RoomSettings roomSettings)
	{
		for (int i = 0; i < roomSettings.placedObjects.Count; i++)
		{
			if (roomSettings.placedObjects[i]?.data is D d) return d;
		}
		return default;
	}
	internal static IEnumerable<D> FindAllPlacedObjectsData<D>(this RoomSettings roomSettings)
	{
		for (int i = 0; i < roomSettings.placedObjects.Count; i++)
		{
			if (roomSettings.placedObjects[i]?.data is D d) yield return d;
		}
	}
	public static IEnumerable<IntVector2> ReturnTiles(this IntRect ir)
	{
		for (int i = ir.left; i <= ir.right; i++)
		{
			for (int j = ir.bottom; j <= ir.top; j++)
			{
				yield return new(i, j);
			}
		}
	}
	public static FContainer ReturnFContainer(this RoomCamera rcam, ContainerCodes cc)
		=> rcam.ReturnFContainer(cc.ToString());
	public static IEnumerable<Modules.RoomZones.IRoomZone> GetZones(this Room room, params int[] tags)
	{
		foreach (UpdatableAndDeletable uad in room.updateList)
		{
			if (uad is Modules.RoomZones.IRoomZone zone && tags.Contains(zone.Tag)) yield return zone;
		}
	}

	/// <summary>
	/// removes any lines that do not fulfill the specified slugcat conditions
	/// </summary>
	public static string[] ProcessTimelineConditions(string[] lines, SlugcatStats.Timeline slug)
	{
		string remove = "___";

		for (int i = 0; i < lines.Length; i++)
		{
			if (lines[i].Length < 1) continue;
			if (lines[i][0] == '(' && lines[i].Contains(')'))
			{
				string text = lines[i].Substring(1, lines[i].IndexOf(")") - 1);
				lines[i] = !StringMatchesTimeline(text, slug) ? remove : lines[i].Substring(lines[i].IndexOf(")") + 1);
			}
		}

		return lines.Where(x => x != remove).ToArray();
	}

	public static bool StringMatchesTimeline(string text, SlugcatStats.Timeline slug)
	{
		bool include = false;
		bool inverted = false;

		if (text.StartsWith("X-"))
		{
			text = text.Substring(2);
			inverted = true;
		}

		if (slug == null)
		{
			return inverted;
		}

		foreach (string text2 in text.Split(','))
		{
			if (text2.Trim() == slug.ToString())
			{
				include = true;
				break;
			}
		}

		return inverted != include;
	}

	public static bool TryGetElementWithName(this FAtlasManager manager, string elementName, out FAtlasElement? result)
	{
		bool has = manager.DoesContainElementWithName(elementName);
		result = has ? manager.GetElementWithName(elementName) : null;
		return has;
	}

	/// <summary>
	/// Calculates area of a mesh. Assumes the mesh is not self-intersecting!
	/// </summary>
	public static float GetArea(this TriangleMesh mesh)
	{
		float sum = 0f;
		for (int i = 0; i < mesh.vertices.Length - 1; i++)
		{
			Vector2 thisVertex = mesh.vertices[i];
			Vector2 nextVertex = mesh.vertices[i + 1];
			sum += thisVertex.x * nextVertex.y - thisVertex.y * nextVertex.x;
		}
		sum /= 2f;
		return sum;
	}
	public static Vector2 GetCentroid(this TriangleMesh mesh)
	{
		float area = mesh.GetArea();
		Vector2 sum = Vector2.zero;
		for (int i = 0; i < mesh.triangles.Length - 1; i++)
		{
			Vector2 thisVertex = mesh.vertices[i];
			Vector2 nextVertex = mesh.vertices[i + 1];
			float x = (thisVertex.x + nextVertex.x) * (thisVertex.x * nextVertex.y - nextVertex.x * thisVertex.y);
			float y = (thisVertex.y + nextVertex.y) * (thisVertex.x * nextVertex.y - nextVertex.x * thisVertex.y);
			sum += new Vector2(x, y);
		}
		Vector2 result = sum / (6f * area);
		return result;
	}
}
