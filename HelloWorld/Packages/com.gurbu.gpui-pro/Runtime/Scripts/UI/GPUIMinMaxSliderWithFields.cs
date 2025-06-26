using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
    public class GPUIMinMaxSliderWithFields : VisualElement
    {
        private MinMaxSlider minMaxSlider;
        private FloatField minField;
        private FloatField maxField;

        private const int floatFieldWidth = 42;

        public GPUIMinMaxSliderWithFields(string label, float minLimit, float maxLimit, float initialMin, float initialMax, Action<Vector2> valueChangedCallback)
        {
            // Create the MinMaxSlider
            minMaxSlider = new MinMaxSlider(initialMin, initialMax, minLimit, maxLimit);
            minMaxSlider.label = label;
            minMaxSlider.labelElement.style.width = 170;
            minMaxSlider.style.flexGrow = 1;
            minMaxSlider.style.paddingRight = 5;
            minMaxSlider.style.marginRight = 2;
            Add(minMaxSlider);

            // Create FloatFields for min and max values
            minField = new FloatField { value = initialMin };
            minField.style.flexGrow = 0;
            minField.style.minWidth = floatFieldWidth;
            minField.style.maxWidth = floatFieldWidth;
            maxField = new FloatField { value = initialMax };
            maxField.style.flexGrow = 0;
            maxField.style.minWidth = floatFieldWidth;
            maxField.style.maxWidth = floatFieldWidth;

            // Arrange elements horizontally
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.Add(minMaxSlider);
            container.Add(minField);
            container.Add(maxField);
            Add(container);

            // Sync MinMaxSlider and FloatFields
            minField.RegisterValueChangedCallback(evt =>
            {
                var clampedMin = Mathf.Clamp(evt.newValue, minLimit, maxField.value);
                minField.value = clampedMin;
                minMaxSlider.value = new Vector2(clampedMin, minMaxSlider.value.y);
                valueChangedCallback?.Invoke(minMaxSlider.value);
            });

            maxField.RegisterValueChangedCallback(evt =>
            {
                var clampedMax = Mathf.Clamp(evt.newValue, minField.value, maxLimit);
                maxField.value = clampedMax;
                minMaxSlider.value = new Vector2(minMaxSlider.value.x, clampedMax);
                valueChangedCallback?.Invoke(minMaxSlider.value);
            });

            minMaxSlider.RegisterValueChangedCallback(evt =>
            {
                minField.value = evt.newValue.x;
                maxField.value = evt.newValue.y;
                valueChangedCallback?.Invoke(evt.newValue);
            });
        }

        // Set the Min and Max values directly
        public void SetValues(float min, float max)
        {
            minField.value = min;
            maxField.value = max;
            minMaxSlider.value = new Vector2(min, max);
        }
    }
}
