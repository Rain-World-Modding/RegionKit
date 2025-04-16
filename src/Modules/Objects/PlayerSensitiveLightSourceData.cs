using System.Globalization;
using System.Text.RegularExpressions;
using RWCustom;
using UnityEngine;

// Made by Alduris
namespace RegionKit.Modules.Objects
{
    public class PlayerSensitiveLightSourceData : PlacedObject.Data
    {
        public Vector2 radHandlePos;
        public Vector2 detectRadHandlePos;
        public Vector2 panelPos;

        public PlacedObject.LightSourceData.ColorType colorType;
        public float minStrength;
        public float maxStrength;
        public float fadeSpeed;
        public bool flat;

        public float Rad
        {
            get
            {
                return radHandlePos.magnitude;
            }
            set
            {
                radHandlePos = radHandlePos.normalized * value;
            }
        }

        public float DetectRad
        {
            get
            {
                return detectRadHandlePos.magnitude;
            }
            set
            {
                detectRadHandlePos = detectRadHandlePos.normalized * value;
            }
        }

        public PlayerSensitiveLightSourceData(PlacedObject owner) : base(owner)
        {
            radHandlePos = new Vector2(0f, 100f);
            detectRadHandlePos = new Vector2(0f, 160f);
            panelPos = DegToVec(30f) * 100f;
            colorType = PlacedObject.LightSourceData.ColorType.Environment;
            minStrength = 0f;
            maxStrength = 1f;
            fadeSpeed = 0.5f;
            flat = false;
        }

        public override void FromString(string s)
        {
            string[] array = Regex.Split(s, "~");
            int i = 0;
            minStrength = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
            maxStrength = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
            fadeSpeed = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
            colorType = new PlacedObject.LightSourceData.ColorType(array[i++], false);
            radHandlePos.x = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
            radHandlePos.y = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
            detectRadHandlePos.x = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
            detectRadHandlePos.y = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
            panelPos.x = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
            panelPos.y = float.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture);
            flat = int.Parse(array[i++], NumberStyles.Any, CultureInfo.InvariantCulture) > 0;
            unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, i++);
        }

        protected string BaseSaveString()
        {
            return $"{minStrength}~{maxStrength}~{fadeSpeed}~{colorType}~{radHandlePos.x}~{radHandlePos.y}~{detectRadHandlePos.x}~{detectRadHandlePos.y}~{panelPos.x}~{panelPos.y}~{(flat ? 1 : 0)}";
        }

        public override string ToString()
        {
            return SaveUtils.AppendUnrecognizedStringAttrs(SaveState.SetCustomData(this, BaseSaveString()), "~", unrecognizedAttributes);
        }
    }
}
