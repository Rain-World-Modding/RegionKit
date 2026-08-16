using Random = UnityEngine.Random;

namespace RegionKit.Modules.MoonStuff
{
	internal static class _Functions
	{
		/// <summary>
		/// Same as <see cref="Custom.ClosestPointOnLine(Vector2, Vector2, Vector2)"/> but clamps the values to always stay on the line
		/// </summary>
		public static Vector2 ClosestPointOnLineClamped(Vector2 A, Vector2 B, Vector2 P)
		{
			Vector2 vector = Custom.ClosestPointOnLine(A, B, P);

			if (A.x > B.x)
			{
				vector.x = Mathf.Clamp(vector.x, B.x, A.x);
			}
			else
			{
				vector.x = Mathf.Clamp(vector.x, A.x, B.x);
			}

			if (A.y > B.y)
			{
				vector.y = Mathf.Clamp(vector.y, B.y, A.y);
			}
			else
			{
				vector.y = Mathf.Clamp(vector.y, A.y, B.y);
			}

			return vector;
		}

		public static Color RandomColor(int seed)
		{
			Random.State state = Random.state;
			Random.InitState(seed);
			Color col = new Color(Random.value, Random.value, Random.value);
			Random.state = state;
			return col;
		}

		public static Color RandomColor()
		{
			return new Color(Random.value, Random.value, Random.value);
		}

		/// <summary>
		/// Takes a given amount of variables with weights and sizes them down so that all of them added up equals 1
		/// </summary>
		public static float[] Percent(params float[] values)
		{
			float num = 0;
			float[] output = values;

			for (int i = 0; i < values.Length; i++)
			{
				num += values[i];
			}

			for (int i = 0; i < output.Length; i++)
			{
				output[i] /= num;
			}

			return output;
		}
	}
}
