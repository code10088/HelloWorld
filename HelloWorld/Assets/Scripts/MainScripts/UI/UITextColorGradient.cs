using TMPro;
using UnityEngine;

public class UITextColorGradient : MonoBehaviour
{
    private TMP_Text text;
    private Color topLeftFrom;
    private Color topRightFrom;
    private Color bottomLeftFrom;
    private Color bottomRightFrom;
    private Color topLeftTo;
    private Color topRightTo;
    private Color bottomLeftTo;
    private Color bottomRightTo;
    public float interval = 1;
    private float timer = 0;

    void Awake()
    {
        text = GetComponent<TMP_Text>();
        topLeftFrom = ColorRandom();
        topRightFrom = ColorRandom();
        bottomLeftFrom = ColorRandom();
        bottomRightFrom = ColorRandom();
        topLeftTo = ColorRandom();
        topRightTo = ColorRandom();
        bottomLeftTo = ColorRandom();
        bottomRightTo = ColorRandom();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < interval)
        {
            float f = timer / interval;
            var gradient = text.colorGradient;
            gradient.topLeft = Color.Lerp(topLeftFrom, topLeftTo, f);
            gradient.topRight = Color.Lerp(topRightFrom, topRightTo, f);
            gradient.bottomLeft = Color.Lerp(bottomLeftFrom, bottomLeftTo, f);
            gradient.bottomRight = Color.Lerp(bottomRightFrom, bottomRightTo, f);
            text.colorGradient = gradient;
        }
        else
        {
            timer = 0;
            topLeftFrom = topLeftTo;
            topRightFrom = topRightTo;
            bottomLeftFrom = bottomLeftTo;
            bottomRightFrom = bottomRightTo;
            topLeftTo = ColorRandom();
            topRightTo = ColorRandom();
            bottomLeftTo = ColorRandom();
            bottomRightTo = ColorRandom();
        }
    }

    Color ColorRandom()
    {
        return Random.ColorHSV(0, 1, 1, 1, 1, 1, 1, 1);
    }
}
