// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class GPUIObjectSelectorElement : VisualElement
    { 
        public static readonly string ussClassName = "gpui-prefab-selector-element";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string iconUssClassName = ussClassName + "__icon";
        public static readonly string countUssClassName = ussClassName + "__count";

        private VisualElement _iconVE;
        private Label _objectNameLabel;
        private Label _countLabel;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public string objectName
        {
            get
            {
                return _objectNameLabel.text;
            }
            set
            {
                _objectNameLabel.text = value;
            }
        }

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public string countText
        {
            get
            {
                return _countLabel.text;
            }
            set
            {
                _countLabel.text = value;
                if (value != null && int.TryParse(value, out int result))
                    _countLabel.SetVisible(result > 0);
            }
        }

        public Texture2D icon
        {
            set
            {
                _iconVE.style.backgroundImage = value;
            }
        }

        public GPUIObjectSelectorElement() : this(string.Empty, null, 0)
        {
        }

        public GPUIObjectSelectorElement(string text, Texture2D icon, int count)
        {
            AddToClassList(ussClassName);

            _iconVE = new VisualElement();
            _iconVE.name = "PrefabIcon";
            _iconVE.AddToClassList(iconUssClassName);
            if (icon != null)
                _iconVE.style.backgroundImage = icon;
            Add(_iconVE);

            _objectNameLabel = new Label();
            _objectNameLabel.name = "PrefabNameLabel";
            _objectNameLabel.text = text;
            _objectNameLabel.AddToClassList(labelUssClassName);
            _objectNameLabel.style.marginRight = 4;
            Add(_objectNameLabel);

            _countLabel = new Label();
            _countLabel.name = "PrefabCountLabel";
            _countLabel.text = count.ToString();
            _countLabel.AddToClassList(countUssClassName);
            _countLabel.style.marginRight = 4;
            Add(_countLabel);
        }

        public void SetObject(UnityEngine.Object o, string countText = null)
        {
            _iconVE.style.backgroundImage = AssetPreview.GetMiniThumbnail(o);
            _objectNameLabel.text = o.name;
            this.countText = countText;
        }

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<GPUIObjectSelectorElement, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription _objectName = new UxmlStringAttributeDescription
            {
                name = "object-name", defaultValue = "Object Name"
            };

            private UxmlStringAttributeDescription _count = new UxmlStringAttributeDescription
            {
                name = "count"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                GPUIObjectSelectorElement pse = ve as GPUIObjectSelectorElement;
                pse.objectName = _objectName.GetValueFromBag(bag, cc);
                pse.countText = _count.GetValueFromBag(bag, cc);
            }
        }
#endif
    }
}