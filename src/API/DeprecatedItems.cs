using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.API
{
	public static class DeprecatedItems
	{
		private static readonly HashSet<string> DeprecatedEffects = [];
		private static readonly HashSet<string> DeprecatedObjects = [];

		public static void RegisterDeprecatedEffect(string type)
		{
			DeprecatedEffects.Add(type);
		}

		public static void RegisterDeprecatedObject(string type)
		{
			DeprecatedObjects.Add(type);
		}

		internal static void Enable()
		{
			try
			{
				IL.DevInterface.RoomSettingsPage.AssembleEffectsPages += RemoveDeprecatedEffects;
				IL.DevInterface.ObjectsPage.AssembleObjectPages += RemoveDeprecatedObjects;
			}
			catch (Exception ex)
			{
				LogError(ex);
			}
		}

		internal static void Disable()
		{
			try
			{
				IL.DevInterface.RoomSettingsPage.AssembleEffectsPages -= RemoveDeprecatedEffects;
				IL.DevInterface.ObjectsPage.AssembleObjectPages -= RemoveDeprecatedObjects;
			}
			catch (Exception ex)
			{
				LogError(ex);
			}
		}

		private static void RemoveDeprecatedEffects(ILContext il)
		{
			// Prevents objects from being added to the pane without removing them from being registered to begin with because this is an easy solution I think
			var c = new ILCursor(il);

			try
			{
				c.GotoNext(MoveType.After, x => x.MatchStloc(1));

				c.Emit(OpCodes.Ldloc, 0);
				c.EmitDelegate((Dictionary<RoomSettingsPage.DevEffectsCategories, List<RoomSettings.RoomEffect.Type>> categoryDict) =>
				{
					foreach (List<RoomSettings.RoomEffect.Type> list in categoryDict.Values)
					{
						list.RemoveAll(x => DeprecatedEffects.Contains(x.value));
					}
				});
			}
			catch (Exception ex)
			{
				LogError("RemoveDeprecatedEffects IL hook failed!");
				LogError(ex);
			}
		}

		private static void RemoveDeprecatedObjects(ILContext il)
		{
			// Prevents objects from being added to the pane without removing them from being registered to begin with because this is an easy solution I think
			var c = new ILCursor(il);

			try
			{
				c.GotoNext(MoveType.After, x => x.MatchStloc(1));

				c.Emit(OpCodes.Ldloc, 0);
				c.EmitDelegate((Dictionary<ObjectsPage.DevObjectCategories, List<PlacedObject.Type>> categoryDict) =>
				{
					foreach (List<PlacedObject.Type> list in categoryDict.Values)
					{
						list.RemoveAll(x => DeprecatedObjects.Contains(x.value));
					}
				});
			}
			catch (Exception ex)
			{
				LogError("RemoveDeprecatedObjects IL hook failed!");
				LogError(ex);
			}
		}
	}
}
