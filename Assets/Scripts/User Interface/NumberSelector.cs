using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NumberSelector : MonoBehaviour
{

    public float min = 0f;
    public float max = 10f;
    public float step = 0.1f;
    public float defaultValue;

    private Button plus, minus;
    [SerializeField] private TextMeshProUGUI numberText;

    private float value;
    public float Value => value;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //TEXT
        numberText.text = defaultValue.ToString("0.0");

        value = defaultValue;

        //BUTTONS UP
        plus = transform.Find("Up").GetComponent<Button>();
        if (plus == null)
        {
            Debug.LogError("NumberSelector: Plus Button component not found!");
            return;
        }
        plus.onClick.AddListener(() =>
        {
            float currentNumber = float.Parse(numberText.text);
            currentNumber += step;
            if (currentNumber > max)
            {
                currentNumber = max;
            }
            value = currentNumber;
            numberText.text = currentNumber.ToString("0.0");
        });

        //BUTTONS DOWN
        minus = transform.Find("Down").GetComponent<Button>();
        if (minus == null)
        {
            Debug.LogError("NumberSelector: Minus Button component not found!");
            return;
        }
        minus.onClick.AddListener(() =>
        {
            float currentNumber = float.Parse(numberText.text);
            currentNumber -= step;
            if (currentNumber < min)
            {
                currentNumber = min;
            }
            value = currentNumber;
            numberText.text = currentNumber.ToString("0.0");
        });
    }
}
