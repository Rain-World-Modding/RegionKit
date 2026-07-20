using EffExt;

namespace RegionKit.Modules.Effects
{
	internal static class PaletteEffectColorBuilder
	{

		internal static void __RegisterBuilder()
		{
			try
			{
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("PaletteEffectColorA");
				builder
					.SetCategory(_Enums.RegionKit_Decoration.value)
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff PaletteEffectColorA init {ex}");
			}

			try
			{
				EffectDefinitionBuilder builder = new EffectDefinitionBuilder("PaletteEffectColorB");
				builder
					.SetCategory(_Enums.RegionKit_Decoration.value)
					.Register();
			}
			catch (Exception ex)
			{
				LogWarning($"Error on eff PaletteEffectColorB init {ex}");
			}
		}
	}
}
