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
}