namespace RegionKit;

public class InvalidPOMDataException : Exception
{
	private readonly Type uadType;
	private readonly Type[] dataTypes;
	private readonly string? _addMessage;

	public InvalidPOMDataException(
		Type uadType,
		Type[] dataTypes,
		string? addMessage = null)
	{
		this.uadType = uadType;
		this.dataTypes = dataTypes;
		this._addMessage = addMessage;
	}
	public override string Message => $"{uadType.FullName} requires ManagedData of one of the followuing types: {dataTypes.Select(t => t.FullName).Stitch()}" + _addMessage is not null ? $"({_addMessage})" : "";
}

public sealed class InvalidPOMDataException<TUAD, TData> : InvalidPOMDataException
	where TUAD : UpdatableAndDeletable
	where TData : ManagedData
{
	private readonly string? _addMessage;

	public InvalidPOMDataException(string? addMessage = null) : base(
		typeof(TUAD),
		new[] { typeof(TData) },
		addMessage)
	{
		this._addMessage = addMessage;
	}
}