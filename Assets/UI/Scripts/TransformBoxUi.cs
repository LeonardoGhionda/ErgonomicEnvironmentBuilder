using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

enum TransformType
{
    T,
    R,
    S
}

struct TransformBox
{
    public TransformType type;
    public TMP_InputField[] fields;
}

/// <summary>
/// make transform box value and selected model transform match
/// </summary>
public class TransformBoxUi : MonoBehaviour
{
    [SerializeField] private RectTransform translateBox;
    [SerializeField] private RectTransform rotateBox;
    [SerializeField] private RectTransform scaleBox;

    TransformBox[] transformBoxes;

    private readonly int fieldCount = 3;

    private Transform selected;
    public Transform Selected
    {
        get { return selected; }
        set
        {
            selected = value;

            //write in ui selected transform values
            foreach (var b in transformBoxes)
            {
                switch (b.type)
                {
                    case TransformType.T:
                        b.fields[0].text = selected.transform.localPosition.x.ToString("F1");
                        b.fields[1].text = selected.transform.localPosition.y.ToString("F1");
                        b.fields[2].text = selected.transform.localPosition.z.ToString("F1");
                        break;
                    case TransformType.R:
                        b.fields[0].text = selected.transform.eulerAngles.x.ToString("F1");
                        b.fields[1].text = selected.transform.eulerAngles.y.ToString("F1");
                        b.fields[2].text = selected.transform.eulerAngles.z.ToString("F1");
                        break;
                    case TransformType.S:
                        b.fields[0].text = selected.transform.localScale.x.ToString("F1");
                        b.fields[1].text = selected.transform.localScale.y.ToString("F1");
                        b.fields[2].text = selected.transform.localScale.z.ToString("F1");
                        break;
                }
            }
        }
    }

    private void Awake()
    {
        //initialize array 
        transformBoxes = new TransformBox[fieldCount];

        //TRANSLATE
        //--------------------
        {
            var unorderedFields = translateBox.GetComponentsInChildren<TMP_InputField>().ToList();
            var x = unorderedFields.Find(a => a.name.Contains("x"));
            var y = unorderedFields.Find(a => a.name.Contains("y"));
            var z = unorderedFields.Find(a => a.name.Contains("z"));

            TMP_InputField[] fields = { x, y, z };

            transformBoxes[0] = new TransformBox
            {
                type = TransformType.T,
                fields = fields,
            };
        }

        //ROTATE
        //--------------------
        {
            var unorderedFields = rotateBox.GetComponentsInChildren<TMP_InputField>().ToList();
            TMP_InputField x = unorderedFields.Find(a => a.name.Contains("x"));
            TMP_InputField y = unorderedFields.Find(a => a.name.Contains("y"));
            TMP_InputField z = unorderedFields.Find(a => a.name.Contains("z"));

            TMP_InputField[] fields = { x, y, z };
            transformBoxes[1] = new TransformBox
            {
                type = TransformType.R,
                fields = fields,
            };
        }

        //SCALE
        //--------------------
        {
            var unorderedFields = scaleBox.GetComponentsInChildren<TMP_InputField>().ToList();
            TMP_InputField x = unorderedFields.Find(a => a.name.Contains("x"));
            TMP_InputField y = unorderedFields.Find(a => a.name.Contains("y"));
            TMP_InputField z = unorderedFields.Find(a => a.name.Contains("z"));

            TMP_InputField[] fields = { x, y, z };

            transformBoxes[2] = new TransformBox
            {
                type = TransformType.S,
                fields = fields,
            };
        }

        //change to a culture that separate decimal with period instead of commas 
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");

        foreach (var b in transformBoxes)
        {
            foreach (var field in b.fields)
            {
                field.contentType = TMP_InputField.ContentType.DecimalNumber;
                field.ForceLabelUpdate();
                field.onEndEdit.AddListener(OnValueChanged);
            }
        }
    }

    /// <summary>
    /// Set inputted values in the selected object
    /// </summary>
    /// <param name="_">only there to make the function usable</param>
    private void OnValueChanged(string _)
    {
        if (selected == null) return;

        foreach (var b in transformBoxes)
        {
            if (selected == null)
            {
                Debug.LogError("Selected is null");
                return;
            }

            Vector3 values = Vector3.zero;
            if (float.TryParse(b.fields[0].text, out float f))
                values.x = f;
            if (float.TryParse(b.fields[1].text, out f))
                values.y = f;
            if (float.TryParse(b.fields[2].text, out f))
                values.z = f;

            switch (b.type)
            {
                case TransformType.T:
                    selected.localPosition = values;
                    break;
                case TransformType.R:
                    selected.eulerAngles = values;
                    break;
                case TransformType.S:
                    selected.localScale = values;
                    break;
            }
        }
        selected.GetComponent<RuntimeGizmoTransform>().ResetHandles();
    }
}
