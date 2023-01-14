namespace RegionKit;

public static class TheRitual
{
	public static void Commence()
	{

		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (asm.FullName.Contains("Partiality"))
			{
				//your sins do not go unnoticed
				throw new Joar();
			}
		}
	}
}
