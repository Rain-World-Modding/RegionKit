using System.Runtime.CompilerServices;

namespace RegionKit.Modules.ShelterBehaviors
{
	// I realized halfway through writing this that it probably wouldn't be the best way of writing it but Oh Well!
	internal static class ShelterEventHandler
	{
		private static readonly RKConditionalWeakTable<Room, List<WeakReference<IReactToShelterEvents>>> reactCWT = new();

		internal static void SubscribeObject(Room room, IReactToShelterEvents subscriber)
		{
			if (!reactCWT.TryGetValue(room, out List<WeakReference<IReactToShelterEvents>> list))
			{
				list = [];
				reactCWT.Add(room, list);
			}
			list.Add(new(subscriber));
		}

		internal static void FireShelterEvent(Room room, float newFactor, float closeSpeed)
		{
			if (reactCWT.TryGetValue(room, out var list))
			{
				// Remove expired subscribers
				list.RemoveAll(x => !x.TryGetTarget(out var subscriber) || (subscriber is UpdatableAndDeletable { slatedForDeletetion: true }));

				// Notify the others
				foreach (WeakReference<IReactToShelterEvents> item in list)
				{
					if (item.TryGetTarget(out IReactToShelterEvents subscriber))
					{
						subscriber.ShelterEvent(newFactor, closeSpeed);
					}
				}
			}
		}
	}
}
