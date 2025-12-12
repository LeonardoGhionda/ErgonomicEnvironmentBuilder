using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;

// Enum and Struct definitions remain the same...
enum TransformType { LT, LR, LS, GT, GR }

struct TransformBox
{
    public TransformType type;
    public TMP_InputField[] fields;
}

public class TransformationBox : MonoBehaviour
{
    [Header("Local")]
    [SerializeField] private RectTransform LTranslateBox;
    [SerializeField] private RectTransform LRotateBox;
    [SerializeField] private RectTransform LScaleBox;
    [Header("Global")]
    [SerializeField] private RectTransform GTranslateBox;
    [SerializeField] private RectTransform GRotateBox;

    TransformBox[] transformBoxes;
    private readonly int fieldCount = 5;

    private Transform selected;
    public Transform Selected
    {
        get { return selected; }
        set
        {
            selected = value;
            UpdateUiFromTransform(); 
        }
    }

    private void Awake()
    {
        transformBoxes = new TransformBox[fieldCount];

        // LOCAL
        transformBoxes[0] = CreateBox(LTranslateBox, TransformType.LT);
        transformBoxes[1] = CreateBox(LRotateBox, TransformType.LR);
        transformBoxes[2] = CreateBox(LScaleBox, TransformType.LS);
        // GLOBAL
        transformBoxes[3] = CreateBox(GTranslateBox, TransformType.GT);
        transformBoxes[4] = CreateBox(GRotateBox, TransformType.GR);

        // Culture Setup ( italy use comma instead of period to separete devimal :P )
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        CultureInfo.CurrentUICulture = new CultureInfo("en-US");

        // Event Listeners
        foreach (var b in transformBoxes)
        {
            var currentBox = b;

            foreach (var field in b.fields)
            {
                field.contentType = TMP_InputField.ContentType.DecimalNumber;
                field.ForceLabelUpdate();
                //field.onEndEdit.AddListener((val) => OnBoxValueChanged(currentBox, val));
            }
        }
    }

    // Helper to cleanup Awake and reduce boilerplate
    private TransformBox CreateBox(RectTransform parent, TransformType type)
    {
        var unorderedFields = parent.GetComponentsInChildren<TMP_InputField>().ToList();
        var x = unorderedFields.Find(a => a.name.Contains("x"));
        var y = unorderedFields.Find(a => a.name.Contains("y"));
        var z = unorderedFields.Find(a => a.name.Contains("z"));
        return new TransformBox { type = type, fields = new TMP_InputField[] { x, y, z } };
    }

    /// <summary>
    /// Reads the actual Transform values and updates all Text Inputs
    /// </summary>
    private void UpdateUiFromTransform()
    {
        if (selected == null) return;

        foreach (var b in transformBoxes)
        {
            Vector3 val = Vector3.zero;
            switch (b.type)
            {
                case TransformType.LT: val = selected.localPosition; break;
                case TransformType.LR: val = selected.localEulerAngles; break;
                case TransformType.LS: val = selected.localScale; break;
                case TransformType.GT: val = selected.position; break;
                case TransformType.GR: val = selected.eulerAngles; break;
            }

            // Update text without triggering onEndEdit events
            b.fields[0].SetTextWithoutNotify(val.x.ToString("F1"));
            b.fields[1].SetTextWithoutNotify(val.y.ToString("F1"));
            b.fields[2].SetTextWithoutNotify(val.z.ToString("F1"));
        }
    }

    /// <summary>
    /// Only updates the specific property that was changed
    /// </summary>
    private void OnBoxValueChanged(GizmoManager gizmoManager, TransformBox box, string _)
    {
        if (selected == null) return;

        Vector3 values = Vector3.zero;
        float f;
        if (float.TryParse(box.fields[0].text, out f)) values.x = f;
        if (float.TryParse(box.fields[1].text, out f)) values.y = f;
        if (float.TryParse(box.fields[2].text, out f)) values.z = f;

        switch (box.type)
        {
            case TransformType.LT:
                selected.localPosition = values;
                break;
            case TransformType.LR:
                selected.localEulerAngles = values;
                break;
            case TransformType.LS:
                selected.localScale = values;
                break;
            case TransformType.GT:
                selected.position = values;
                break;
            case TransformType.GR:
                selected.eulerAngles = values;
                break;
        }

        UpdateUiFromTransform();

        // Update Gizmos
        gizmoManager.onSelectionExternallyMoved(selected.transform);
    }
}