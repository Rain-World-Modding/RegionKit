namespace RegionKit.Modules.BackgroundBuilder;

public enum BackgroundBuilderError
{
    InvalidVanillaBgElement,
    WrongVanillaBgScene
}

public class BackgroundBuilderException : Exception
{
	public BackgroundBuilderException(BackgroundBuilderError err)
	{
		error = err;
	}
	public BackgroundBuilderError error { get; private set; }

	public override string Message => error.ToString();
}