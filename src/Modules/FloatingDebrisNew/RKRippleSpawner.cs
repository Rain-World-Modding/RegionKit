using Watcher;

namespace RegionKit.Modules.FloatingDebrisNew
{
	public class RKRippleSpawner(string maskShader, bool watcher) : FloatingDebris.Floater.IFloaterSpawner
	{
		public virtual FloatingDebris.UIText GetUIText()
		{
			return FloatingDebris.Ripple.RippleSpawner.uiText;
		}

		public virtual FloatingDebris.Floater Spawn(FloatingDebris.FloaterData data)
		{
			return new FloatingDebris.Ripple(data)
			{
				isGameplay = watcher,
				maskShader = maskShader
			};
		}
	}
}
