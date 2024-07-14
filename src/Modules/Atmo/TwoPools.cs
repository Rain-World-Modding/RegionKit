using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
/// <summary>
/// Represents a bipartite graph. Built for memory efficiency in case value types are used, but is slow to add/remove items to, since every time you do so it has to re-sort internal lists and re-generate indice bindings.
/// </summary>
/// <typeparam name="TLeft"></typeparam>
/// <typeparam name="TRight"></typeparam>
public sealed class TwoPools<TLeft, TRight>
	where TRight : IEquatable<TRight>, IComparable<TRight>
	where TLeft : IEquatable<TLeft>, IComparable<TLeft>
{
	/// <summary>
	/// Creates a new instance, optionally filling either side with items.
	/// </summary>
	/// <param name="leftSet"></param>
	/// <param name="rightSet"></param>
	public TwoPools(IEnumerable<TLeft>? leftSet = null, IEnumerable<TRight>? rightSet = null)
	{
		if (leftSet is not null)
		{
			left.AddRange(leftSet);
			left.Sort();
		}
		if (rightSet is not null)
		{
			right.AddRange(rightSet);
			right.Sort();
		}
		GenerateLinks();
	}
	#region fields
	private readonly List<TLeft> left = new();
	private readonly List<TRight> right = new();
	/// <summary>
	/// Index on the left, associated indices on the right
	/// </summary>
	private readonly Dictionary<int, List<int>> bindFromLeft = new();
	/// <summary>
	/// Index on the right, associated indices on the left
	/// </summary>
	private readonly Dictionary<int, List<int>> bindFromRight = new();
	#endregion
	#region public methods
	/// <summary>
	/// Adds an item to the left pool
	/// </summary>
	/// <param name="item"></param>
	public void InsertLeft(TLeft item)
	{
		if (LeftContains(item)) return;
		List<TwoPools<TLeft, TRight>.Link>? oldlinks = ExtractLinks();
		left.Add(item);
		left.Sort();
		GenerateLinks(oldlinks);
	}
	/// <summary>
	/// Adds several items to the left pool.
	/// </summary>
	/// <param name="items"></param>
	public void InsertRangeLeft(IEnumerable<TLeft> items)
	{
		List<TwoPools<TLeft, TRight>.Link>? oldlinks = ExtractLinks();
		foreach (TLeft? item in items)
		{
			if (left.Contains(item)) continue;
			left.Add(item);
		}
		//left.AddRange(items);
		left.Sort();
		GenerateLinks(oldlinks);
	}
	/// <summary>
	/// Removes an item from the left pool
	/// </summary>
	/// <param name="item"></param>
	public void RemoveLeft(TLeft item)
	{
		List<TwoPools<TLeft, TRight>.Link>? oldlinks = ExtractLinks();
		left.Remove(item);
		left.Sort();
		GenerateLinks(oldlinks);
	}
	/// <summary>
	/// Adds an item to the right pool
	/// </summary>
	/// <param name="item"></param>
	public void InsertRight(TRight item)
	{
		if (RightContains(item)) return;
		List<TwoPools<TLeft, TRight>.Link>? oldLinks = ExtractLinks();
		right.Add(item);
		right.Sort();
		GenerateLinks(oldLinks);
	}
	/// <summary>
	/// Adds several items to the right pool
	/// </summary>
	/// <param name="items"></param>
	public void InsertRangeRight(IEnumerable<TRight> items)
	{
		List<TwoPools<TLeft, TRight>.Link>? oldLinks = ExtractLinks();
		foreach (TRight? item in items)
		{
			if (right.Contains(item)) continue;
			right.Add(item);
		}
		//right.AddRange(items);
		right.Sort();
		GenerateLinks(oldLinks);
	}
	/// <summary>
	/// Removes an item from the right pool
	/// </summary>
	/// <param name="item"></param>
	public void RemoveRight(TRight item)
	{
		List<TwoPools<TLeft, TRight>.Link>? oldLinks = ExtractLinks();
		right.Remove(item);
		right.Sort();
		GenerateLinks(oldLinks);
	}
	/// <summary>
	/// Clears both pools.
	/// </summary>
	public void Clear()
	{
		left.Clear();
		right.Clear();
		GenerateLinks();
	}
	/// <summary>
	/// Adds a link between two items.
	/// </summary>
	/// <param name="itemL">Item on the left</param>
	/// <param name="itemR">Item on the right</param>
	/// <returns>true if a link was successfully added; otherwise false.</returns>
	public bool AddLink(TLeft itemL, TRight itemR)
	{
		int rIndex = right.BinarySearch(itemR);
		int lIndex = left.BinarySearch(itemL);
		if (rIndex < 0 || lIndex < 0) return false;//One of the items was not found in the pools
		bindFromLeft[lIndex].Add(rIndex);
		bindFromLeft[lIndex].TrimExcess();
		bindFromRight[rIndex].Add(lIndex);
		bindFromRight[rIndex].TrimExcess();
		return true;
	}
	/// <summary>
	/// Establishes multiple links between pools.
	/// </summary>
	/// <param name="pairs"></param>
	public void AddLinksBulk(IEnumerable<KeyValuePair<TLeft, TRight>> pairs)
	{
		foreach (KeyValuePair<TLeft, TRight> pair in pairs) AddLink(pair.Key, pair.Value);
	}
	/// <summary>
	/// Removes a link between given items.
	/// </summary>
	/// <param name="itemL">Item in the left column</param>
	/// <param name="itemR">Item in the right column</param>
	/// <returns>true if something was removed</returns>
	public bool RemoveLink(TLeft itemL, TRight itemR)
	{
		int rIndex = right.BinarySearch(itemR);
		int lIndex = left.BinarySearch(itemL);
		if (rIndex < 0 || lIndex < 0) return false;//One of the items was not found in the pools
		bool successL = bindFromLeft[lIndex].Remove(rIndex);
		bindFromLeft[lIndex].TrimExcess();
		bool successR = bindFromRight[rIndex].Remove(lIndex);
		bindFromRight[lIndex].TrimExcess();
		return successL || successR;
	}
	/// <summary>
	/// Removes multiple links between pools.
	/// </summary>
	/// <param name="pairs"></param>
	public void RemoveLinksBulk(IEnumerable<KeyValuePair<TLeft, TRight>> pairs)
	{
		foreach (KeyValuePair<TLeft, TRight> pair in pairs) RemoveLink(pair.Key, pair.Value);
	}
	/// <summary>
	/// Yields all items on the right associated with a given item on the left.
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	/// <exception cref="KeyNotFoundException"></exception>
	public IEnumerable<TRight> IndexFromLeft(TLeft item)
	{
		int selected = left.BinarySearch(item);
		if (selected < 0) throw new KeyNotFoundException("Item not found in left pool");
		foreach (int indexR in bindFromLeft[selected])
		{
			yield return right[indexR];
		}
	}
	/// <summary>
	/// Yields all items on the left associated with a given item on the right. 
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	/// <exception cref="KeyNotFoundException"></exception>
	public IEnumerable<TLeft> IndexFromRight(TRight item)
	{
		int selected = right.BinarySearch(item);
		if (selected < 0) throw new KeyNotFoundException("Item not found in right pool");
		foreach (int indexL in bindFromRight[selected])
		{
			yield return left[indexL];
		}
	}
	/// <summary>
	/// Checks if left pool contains a given item.
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool LeftContains(TLeft item)
	{
		return left.BinarySearch(item) >= 0;
	}

	/// <summary>
	/// Checks if right pool contains a given item.
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public bool RightContains(TRight item)
	{
		return right.BinarySearch(item) >= 0;
	}

	/// <summary>
	/// Returns a collection containing everything in the left pool.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<TLeft> EnumerateLeft()
	{
		return left.AsEnumerable();
	}

	/// <summary>
	/// Returns a collection containing everything in the right pool.
	/// </summary>
	/// <returns></returns>
	public IEnumerable<TRight> EnumerateRight()
	{
		return right.AsEnumerable();
	}

	/// <summary>
	/// Concatenates two similar pools, retaining links. WARNING: SLOW
	/// </summary>
	/// <param name="p1"></param>
	/// <param name="p2"></param>
	/// <returns></returns>
	public static TwoPools<TLeft, TRight> Stitch(TwoPools<TLeft, TRight> p1, TwoPools<TLeft, TRight> p2)
	{
		TwoPools<TLeft, TRight> res = new();
		List<TwoPools<TLeft, TRight>.Link>
			links1 = p1.ExtractLinks(),
			links2 = p2.ExtractLinks();
		res.InsertRangeLeft(p1.EnumerateLeft());
		res.InsertRangeLeft(p2.EnumerateLeft());
		res.InsertRangeRight(p1.EnumerateRight());
		res.InsertRangeRight(p2.EnumerateRight());
		res.AddLinksBulk(
			links1.AsEnumerable().Concat(links2).Select(
				(x) => new KeyValuePair<TLeft, TRight>(x.ileft, x.iright))
			);
		return res;
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		var sb = new StringBuilder();
		for (int i = 0; i < left.Count; i++)
		{

			bool leftExists = i >= left.Count;
			sb.Append($"{i}.\t");
			sb.Append(leftExists ? "[ ]" : left[i]);
			sb.Append('{');
			sb.Append(StitchSeq(IndexFromLeft(left[i]).Select(x => x.ToString())));
			//if (leftExists)
			//{
			//    bindFromLeft.TryGetValue(i, out var supposedLinks);
			//    if (supposedLinks != null) sb.Append(supposedLinks.Select(x => x.ToString()).Aggregate((x, y) => $"{x}, {y}"));
			//}
			////sb.Append('\t');
			sb.Append('}');
			//sb.Append(i >= right.Count ? "[ ]" : right[i]);
			sb.Append('\n');
			//sb.AppendFormat("{0}\t{1}", (i > left.Count ? "[ ]" : left[i]), (i > right.Count ? "[ ]" : right[i]));
		}
		return sb.ToString();
	}
	#endregion
	#region internals
	/// <summary>
	/// Extracts a list of links to be used in relinking later (used when inserting or removing items).
	/// </summary>
	/// <returns></returns>
	private List<Link> ExtractLinks()
	{
		List<Link> res = new();
		foreach (KeyValuePair<int, List<int>> kvp in bindFromLeft)
		{
			foreach (int rightside in kvp.Value)
			{
				res.Add(new Link(left[kvp.Key], right[rightside]));
			}
		}
		return res;
	}
	/// <summary>
	/// Generates link dictionary contents, optionally inheriting from a previous set
	/// </summary>
	/// <param name="links"></param>
	/// <exception cref="NotImplementedException"></exception>
	private void GenerateLinks(List<Link>? links = null)
	{
		bindFromLeft.Clear();
		bindFromRight.Clear();
		for (int i = 0; i < left.Count; i++)
		{
			bindFromLeft.Add(i, new());
		}
		for (int j = 0; j < right.Count; j++)
		{
			bindFromRight.Add(j, new());
		}
		if (links is null) return;
		foreach (Link link in links)
		{
			AddLink(link.ileft, link.iright);
		}
	}
	#endregion
	/// <summary>
	/// Stitches a collection by commas
	/// </summary>
	/// <param name="coll"></param>
	/// <returns></returns>
	public static string StitchSeq(IEnumerable<string> coll)
	{
		return coll is null || coll.Count() is 0 ? string.Empty : coll.Aggregate((x, y) => $"{x}, {y}");
	}

	private struct Link : IEquatable<Link>
	{
		internal TLeft ileft;
		internal TRight iright;

		public Link(TLeft ileft, TRight iright)
		{
			this.ileft = ileft;
			this.iright = iright;
		}

		public bool Equals(TwoPools<TLeft, TRight>.Link other)
		{
			return ileft.Equals(other.ileft) && iright.Equals(other.iright);
		}
	}
}

