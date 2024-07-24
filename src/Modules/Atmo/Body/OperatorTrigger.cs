namespace RegionKit.Modules.Atmo.Body;

internal class OperatorTrigger : HappenTrigger
{
	HappenTrigger left; HappenTrigger right;
	private Func<bool, bool, bool> getter;

	public OperatorTrigger(HappenTrigger left, HappenTrigger right, Func<bool, bool, bool> getter)
	{
		this.left = left;
		this.right = right;
		this.getter = getter;
	}

	public override bool Active()
	{
		bool left = false;
		bool right = false;
		try { left = this.left.Active(); }
		catch (Exception e)
		{
			this.left = new EventfulTrigger(owner, null);
		}

		try { right = this.right.Active(); }
		catch (Exception e)
		{
			this.right = new EventfulTrigger(owner, null);
		}


		return getter(left, right);
	}

	public override void Update()
	{
		try { left.Update(); }
		catch (Exception e)
		{
			left = new EventfulTrigger(owner, null);
		}

		try { right.Update(); }
		catch (Exception e)
		{
			right = new EventfulTrigger(owner, null);
		}
	}

	public static Dictionary<string, Func<HappenTrigger, HappenTrigger, HappenTrigger>> args = new()
	{
		{ "AND", AND },
		{ "&", AND },
		{ "OR", OR },
		{ "|", OR },
		{ "XOR", XOR },
		{ "^", XOR },
		{ "NOT", NOT },
		{ "!", NOT },
		{ "EQUALS", EQUALS },
		{ "=", EQUALS },
	};

	public static HappenTrigger EQUALS(HappenTrigger left, HappenTrigger right)
	{
		return new OperatorTrigger(left, right, (left, right) => left == right);
	}
	public static HappenTrigger AND(HappenTrigger left, HappenTrigger right)
	{
		return new OperatorTrigger(left, right, (left, right) => left && right);
	}
	public static HappenTrigger OR(HappenTrigger left, HappenTrigger right)
	{
		return new OperatorTrigger(left, right, (left, right) => left || right);
	}
	public static HappenTrigger XOR(HappenTrigger left, HappenTrigger right)
	{
		return new OperatorTrigger(left, right, (left, right) => left ^ right);
	}
	public static HappenTrigger NOT(HappenTrigger left, HappenTrigger right)
	{
		left = new EventfulTrigger(left.owner, null);
		return new OperatorTrigger(left, right, (left, right) => !right);
	}
}
