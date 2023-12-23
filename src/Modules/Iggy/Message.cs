namespace RegionKit.Modules.Iggy;

public class Message
{
	public DateTime expiration;
	public string text;
	public bool Expired => expiration < DateTime.Now;

	public Message(TimeSpan life, string text)
	{
		this.expiration = DateTime.Now + life;
		this.text = text;
	}
}
