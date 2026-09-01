namespace RegionKit.Modules.MoonStuff
{
	/// <summary>
	/// Specifies an object can interact with <see cref="LightSourceFlicker"/>
	/// </summary>
	public interface IFlickerable
	{
		/// <summary>The position to check for whether the object is in range</summary>
		public Vector2 CheckPosition { get; }

		/// <summary>The sunlight type to count as. Use <c>All</c> to count as any.</summary>
		public LightSourceFlickerType.LightSourceFlickerData.SunlightType SunlightType { get; }

		/// <summary>The light source type to count as. Use <c>All</c> to count as any.</summary>
		public LightSourceFlickerType.LightSourceFlickerData.LightSourceType LightSourceType { get; }
	}
}
