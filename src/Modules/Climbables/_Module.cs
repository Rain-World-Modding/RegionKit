namespace RegionKit.Modules.Climbables;


[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Climbables")]
public static class _Module
{
	internal const string CLIMBABLES_POM_CATEGORY = Objects._Module.GAMEPLAY_POM_CATEGORY;
	internal static void Setup()
	{
		RegisterFullyManagedObjectType([new IntVector2Field("vector", new IntVector2(), IntVector2Field.IntVectorReprType.rect)], typeof(ClimbablePoleH), "ClimbablePoleH", CLIMBABLES_POM_CATEGORY);
		RegisterFullyManagedObjectType([new IntVector2Field("vector", new IntVector2(), IntVector2Field.IntVectorReprType.rect)], typeof(ClimbablePoleV), "ClimbablePoleV", CLIMBABLES_POM_CATEGORY);
		RegisterFullyManagedObjectType([new Vector2Field("vector", new Vector2(), Vector2Field.VectorReprType.line)], typeof(ClimbableRope), "ClimbableRope", CLIMBABLES_POM_CATEGORY);
		RegisterManagedObject<ClimbableArc, BezierObjectData, BezierObjectRepresentation>("ClimbableArc", CLIMBABLES_POM_CATEGORY);
	}

	internal static void Enable()
	{
		On.ClimbableVinesSystem.VineSwitch += ClimbableVinesSystem_VineSwitch_hk;
	}
	internal static void Disable()
	{
		On.ClimbableVinesSystem.VineSwitch -= ClimbableVinesSystem_VineSwitch_hk;
	}


	private static ClimbableVinesSystem.VinePosition? ClimbableVinesSystem_VineSwitch_hk(On.ClimbableVinesSystem.orig_VineSwitch orig, ClimbableVinesSystem self, ClimbableVinesSystem.VinePosition vPos, UnityEngine.Vector2 goalPos, float rad)
	{
		ClimbableVinesSystem.VinePosition newPos = orig(self, vPos, goalPos, rad);

		if (vPos.vine is ClimbableArc && newPos == null && (vPos.floatPos == 0f || vPos.floatPos == 1f))
		{
			// Copypaste from orig but bypassing the dotprod check
			int num = self.PrevSegAtFloat(vPos.vine, vPos.floatPos);
			int num2 = Custom.IntClamp(num + 1, 0, vPos.vine.TotalPositions() - 1);
			float t = Mathf.InverseLerp(self.FloatAtSegment(vPos.vine, num), self.FloatAtSegment(vPos.vine, num2), vPos.floatPos);
			Vector2 vector = Vector2.Lerp(vPos.vine.Pos(num), vPos.vine.Pos(num2), t);
			goalPos = vector + (vector - goalPos).normalized * 0.1f; // shorten that range a tiny bit.
			float f = Vector2.Dot((vPos.vine.Pos(num) - vPos.vine.Pos(num2)).normalized, (vector - goalPos).normalized);
			if (Mathf.Abs(f) > 0.5f)
			{
				float num3 = float.MaxValue;

				for (int j = 0; j < vPos.vine.TotalPositions(); j++)
				{
					if (self.OverlappingSegment(vPos.vine.Pos(j), vPos.vine.Rad(j), vPos.vine.Pos(j + 1), vPos.vine.Rad(j + 1), vector, rad))
					{
						Vector2 vector2 = self.ClosestPointOnSegment(vPos.vine.Pos(j), vPos.vine.Pos(j + 1), vector);
						float num4 = Vector2.Distance(vector2, goalPos);
						num4 *= 1f - 0.25f * Mathf.Abs(Vector2.Dot((vPos.vine.Pos(j) - vPos.vine.Pos(j + 1)).normalized, (vector - goalPos).normalized));
						float num5 = Mathf.Lerp(self.FloatAtSegment(vPos.vine, j), self.FloatAtSegment(vPos.vine, j + 1), Mathf.InverseLerp(0f, Vector2.Distance(vPos.vine.Pos(j), vPos.vine.Pos(j + 1)), Vector2.Distance(vPos.vine.Pos(j), vector2))) * self.TotalLength(vPos.vine);
						if (Mathf.Abs(vPos.floatPos * self.TotalLength(vPos.vine) - num5) < 100f)
						{
							num4 = float.MaxValue;
						}
						if (num4 < num3)
						{
							num3 = num4;
							float t2 = Mathf.InverseLerp(0f, Vector2.Distance(vPos.vine.Pos(j), vPos.vine.Pos(j + 1)), Vector2.Distance(vPos.vine.Pos(j), vector2));
							newPos = new ClimbableVinesSystem.VinePosition(vPos.vine, Mathf.Lerp(self.FloatAtSegment(vPos.vine, j), self.FloatAtSegment(vPos.vine, j + 1), t2));
						}
					}
				}
			}
		}

		return newPos;
	}

}
