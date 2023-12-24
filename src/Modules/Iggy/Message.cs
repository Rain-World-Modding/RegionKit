namespace RegionKit.Modules.Iggy;

public record Message(DateTime expiration, string text, bool uninterruptable)
{
	public bool Expired => expiration < DateTime.Now;

	public Message(TimeSpan length, string text, bool uninterruptable) : this(DateTime.Now + length, text, uninterruptable) { }
}
