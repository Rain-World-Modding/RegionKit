using Random = UnityEngine.Random;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;

namespace RegionKit.Modules.Objects;

public class FanLightData : PlacedObject.Data
{
	public Vector2 panelPos = DegToVec(120f) * 20f;
	public int randomSeed = Random.Range(0, 101);
	public float colorR = 1f;
	public float colorG;
	public float colorB;
	public int speed = 50;
	public Vector2 handlePos = new(0f, 100f);
	public int inverseSpeed;
	public string imageName = "FanLightMask1";
    public bool submersible;

	public FanLightData(PlacedObject owner) : base(owner) { }

	public virtual Color Color => new(colorR, colorG, colorB);

	public virtual Color Color2 => new(colorR, colorG + .05f, colorB + .05f);

	public virtual float Rad => handlePos.magnitude;

    public override void FromString(string s)
	{
		var ar = Regex.Split(s, "~");
        if (ar.Length >= 11) 
        {
            float.TryParse(ar[0], NumberStyles.Any, CultureInfo.InvariantCulture, out panelPos.x);
            float.TryParse(ar[1], NumberStyles.Any, CultureInfo.InvariantCulture, out panelPos.y);
			int.TryParse(ar[2], NumberStyles.Any, CultureInfo.InvariantCulture, out randomSeed);
            float.TryParse(ar[3], NumberStyles.Any, CultureInfo.InvariantCulture, out colorR);
            float.TryParse(ar[4], NumberStyles.Any, CultureInfo.InvariantCulture, out colorG);
            float.TryParse(ar[5], NumberStyles.Any, CultureInfo.InvariantCulture, out colorB);
            int.TryParse(ar[6], NumberStyles.Any, CultureInfo.InvariantCulture, out speed);
            imageName = ar[7] switch
            {
                "1" or "" or null => "FanLightMask1",
                "2" => "FanLightMask2",
                "3" => "FanLightMask3",
                _ => ar[7]
            };
            float.TryParse(ar[8], NumberStyles.Any, CultureInfo.InvariantCulture, out handlePos.x);
            float.TryParse(ar[9], NumberStyles.Any, CultureInfo.InvariantCulture, out handlePos.y);
            int.TryParse(ar[10], NumberStyles.Any, CultureInfo.InvariantCulture, out inverseSpeed);
            if (ar.Length >= 12)
            {
                submersible = ar[11] == "1";
                unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(ar, 12);
            }
            else
                unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(ar, 11);
        }
    }

    protected virtual string BaseSaveString() 
    {
		return new StringBuilder()
			.Append(panelPos.x.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(panelPos.y.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(randomSeed.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(colorR.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(colorG.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(colorB.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(speed.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(imageName)
			.Append('~')
			.Append(handlePos.x.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(handlePos.y.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(inverseSpeed.ToString(CultureInfo.InvariantCulture))
			.Append('~')
			.Append(submersible ? '1' : '0')
			.ToString();
    }

    public override string ToString() => SaveUtils.AppendUnrecognizedStringAttrs(BaseSaveString(), "~", unrecognizedAttributes);
}
