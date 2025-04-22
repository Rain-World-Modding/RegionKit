using DevInterface;

namespace RegionKit.Modules.Climbables;

internal class BezierObjectRepresentation : ManagedRepresentation
{
	protected ManagedData data => (ManagedData)placedObject.data;
	private Vector2[] _Quad
	{
		get
		{
			var vecs = data.GetValue<Vector2[]>("vectors")!;
			return new[]
			{
				vecs[1],
				vecs[2],
				vecs[3]
			};
		}
	}

	PlacedObject placedObject;
	Handle handleA => this;
	Handle handleB;
	Handle handleC;
	Handle handleD;

	GameObject? lineObject;
	LineRenderer? lineRenderer;
	FGameObjectNode? lineNode;
	public BezierObjectRepresentation(PlacedObject.Type placedType, ObjectsPage objPage, PlacedObject pObj) : base(placedType, objPage, pObj)
	{
		placedObject = pObj;
		handleB = new Handle(owner, "Rect_Handle", this, new Vector2(0f, 40f));
		this.subNodes.Add(handleB);
		handleB.pos = _Quad[0];
		handleC = new Handle(owner, "Rect_Handle", this, new Vector2(40f, 40f));
		this.subNodes.Add(handleC);
		handleC.pos = _Quad[1];
		handleD = new Handle(owner, "Rect_Handle", handleC, new Vector2(40f, 0f));
		handleC.subNodes.Add(handleD);
		handleD.pos = _Quad[2] - handleC.pos;
		for (int i = 0; i < 2; i++)
		{
			this.fSprites.Add(new FSprite("pixel", true));
			owner.placedObjectsContainer.AddChild(this.fSprites[1 + i]);
			this.fSprites[1 + i].anchorY = 0f;
		}

		lineObject = new GameObject();
		lineRenderer = (LineRenderer)(lineObject.AddComponent(typeof(LineRenderer)));
		lineRenderer.material = new Material(FShader.defaultShader.shader);

		UpdateLineSegments();

		lineNode = new FGameObjectNode(lineObject, false, false, false);
		owner.placedObjectsContainer.AddChild(lineNode);

		lineRenderer.startColor = Color.white;
		lineRenderer.endColor = Color.white;
	}

	protected override void MakeControls()
	{
		//please don't make the representations
	}

	protected void UpdateLineSegments()
	{
		float heuristicDistance = handleB.pos.magnitude;
		heuristicDistance += handleD.pos.magnitude;
		heuristicDistance += (handleB.pos - (handleC.pos + handleD.pos)).magnitude;

		int nsegments = Mathf.CeilToInt(heuristicDistance / 10f);

		Vector2 posA = handleA.absPos;
		Vector2 posB = handleB.absPos;
		Vector2 posC = handleC.absPos;
		Vector2 posD = handleD.absPos;

		if (lineRenderer is null) {
			return;
		};
		lineRenderer.positionCount = nsegments;
		float step = 1f / nsegments;
		for (int i = 0; i < nsegments; i++)
		{
			float t = step * i;
			float num = 1f - t;
			Vector2 pt = num * num * num * posA + 3f * num * num * t * posB + 3f * num * t * t * posD + t * t * t * posC;
			lineRenderer.SetPosition(i, pt);
		}
	}


	public override void ClearSprites()
	{
		base.ClearSprites();
		lineObject = null;
		lineRenderer = null;
		lineNode?.RemoveFromContainer();
		lineNode = null;
	}

	public override void SetColor(Color col)
	{
		base.SetColor(col);
		if (lineRenderer != null)
		{
			lineRenderer.startColor = col;
			lineRenderer.endColor = col;
		}
	}

	public override void Refresh()
	{
		base.Refresh();
		Vector2[] vectors = new Vector2[]
		{
			new Vector2(0f, 0f),
			handleB.pos,
			handleC.pos,
			handleD.pos + handleC.pos
		};
		data.SetValue<Vector2[]>("vectors", vectors);
		base.MoveSprite(1, this.absPos);
		this.fSprites[1].scaleY = handleB.pos.magnitude;
		this.fSprites[1].rotation = RWCustom.Custom.VecToDeg(handleB.pos);
		base.MoveSprite(2, this.absPos + handleC.pos);
		this.fSprites[2].scaleY = handleD.pos.magnitude;
		this.fSprites[2].rotation = RWCustom.Custom.VecToDeg(handleD.pos);

		UpdateLineSegments();
	}

}
public class BezierObjectData : ManagedData
{
	// Token: 0x06003C30 RID: 15408 RVA: 0x0044740C File Offset: 0x0044560C
	public BezierObjectData(PlacedObject owner) : base(owner, new ManagedField[] {
					new Vector2ArrayField("vectors", 4, true, Vector2ArrayField.Vector2ArrayRepresentationType.Polygon, Vector2.zero, Vector2.up * 40f, (Vector2.right + Vector2.up) * 40f, Vector2.right * 40f)})
	{
	}
}
