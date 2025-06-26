// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class GPUIHelpBox : VisualElement
    { 
        public static readonly string ussClassName = "unity-help-box";
        public static readonly string labelUssClassName = ussClassName + "__label";
        public static readonly string iconUssClassName = ussClassName + "__icon";
        public static readonly string iconInfoUssClassName = iconUssClassName + "--info";
        public static readonly string iconWarningUssClassName = iconUssClassName + "--warning";
        public static readonly string iconErrorUssClassName = iconUssClassName + "--error";

        private HelpBoxMessageType _helpBoxMessageType;
        private VisualElement _iconVE;
        private string _iconClass;
        private Label _helpLabel;

        private VisualElement _buttonsVE;
        private Button _wwwButton;
        private Button _selectButton;
        private Button _fixButton;
        private string _wwwAddress;
        private UnityEngine.Object _targetObject;
        private UnityEngine.Events.UnityEvent _fixEvent;
        private UnityEngine.Events.UnityAction _fixAction;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public string helpText
        {
            get
            {
                return _helpLabel.text;
            }
            set
            {
                _helpLabel.text = value;
            }
        }

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public HelpBoxMessageType messageType
        {
            get
            {
                return _helpBoxMessageType;
            }
            set
            {
                if (value != _helpBoxMessageType)
                {
                    _helpBoxMessageType = value;
                    UpdateIcon(value);
                }
            }
        }

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public string wwwAddress
        {
            get
            {
                return _wwwAddress;
            }
            set
            {
                if (value != _wwwAddress)
                {
                    _wwwAddress = value;
                    UpdateButtons();
                }
            }
        }


        public GPUIHelpBox() : this(string.Empty, HelpBoxMessageType.Info)
        {
        }

        public GPUIHelpBox(string text, HelpBoxMessageType messageType, string wwwAddress = null, UnityEngine.Object targetObject = null, UnityEngine.Events.UnityAction fixAction = null, string fixButtonText = null, Color? fixButtonColor = null)
        {
            _helpBoxMessageType = messageType;
            _wwwAddress = wwwAddress;
            _targetObject = targetObject;
            _fixAction = fixAction;

            AddToClassList(ussClassName);
            AddToClassList("gpui-help-box");

            _helpLabel = new Label();
            _helpLabel.name = "HelpLabel";
            _helpLabel.text = text;
            _helpLabel.AddToClassList(labelUssClassName);
            _helpLabel.style.marginRight = 4;
            Add(_helpLabel);
            _iconVE = new VisualElement();
            _iconVE.name = "HelpIcon";
            _iconVE.AddToClassList(iconUssClassName);
            UpdateIcon(messageType);

            _fixEvent = new UnityEngine.Events.UnityEvent();
            _fixEvent.AddListener(_fixAction);

            _buttonsVE = new VisualElement();
            _buttonsVE.name = "HelpButtons";
            _buttonsVE.AddToClassList("gpui-help-box-button-list");
            Add(_buttonsVE);

            _wwwButton = new Button();
            _wwwButton.name = "WWWButton";
            _wwwButton.text = "Docs";
            _wwwButton.style.backgroundColor = GPUIEditorConstants.Colors.blue;
            _wwwButton.AddToClassList("gpui-help-box-button");
            _wwwButton.RegisterCallback<MouseUpEvent>(ev => Application.OpenURL(_wwwAddress));
            _wwwButton.focusable = false;
            _buttonsVE.Add(_wwwButton);

            _selectButton = new Button();
            _selectButton.name = "SelectButton";
            _selectButton.text = "Select";
            _selectButton.style.backgroundColor = GPUIEditorConstants.Colors.green;
            _selectButton.AddToClassList("gpui-help-box-button");
            _selectButton.RegisterCallback<MouseUpEvent>(ev => Selection.activeObject = _targetObject);
            _selectButton.focusable = false;
            _buttonsVE.Add(_selectButton);

            _fixButton = new Button();
            _fixButton.name = "FixButton";
            _fixButton.text = string.IsNullOrEmpty(fixButtonText) ? "Fix" : fixButtonText;
            _fixButton.style.backgroundColor = fixButtonColor == null ? GPUIEditorConstants.Colors.lightRed : fixButtonColor.Value;
            _fixButton.AddToClassList("gpui-help-box-button");
            _fixButton.RegisterCallback<MouseUpEvent>(ev => _fixEvent.Invoke());
            _fixButton.SetEnabled(!Application.isPlaying);
            _fixButton.focusable = false;
            _buttonsVE.Add(_fixButton);

            UpdateButtons();
        }

        private string GetIconClass(HelpBoxMessageType messageType)
        {
            return messageType switch
            {
                HelpBoxMessageType.Info => iconInfoUssClassName,
                HelpBoxMessageType.Warning => iconWarningUssClassName,
                HelpBoxMessageType.Error => iconErrorUssClassName,
                _ => null,
            };
        }

        private void UpdateIcon(HelpBoxMessageType messageType)
        {
            if (!string.IsNullOrEmpty(_iconClass))
            {
                _iconVE.RemoveFromClassList(_iconClass);
            }

            _iconClass = GetIconClass(messageType);
            if (_iconClass == null)
            {
                _iconVE.RemoveFromHierarchy();
                return;
            }

            _iconVE.AddToClassList(_iconClass);
            if (_iconVE.parent == null)
            {
                Insert(0, _iconVE);
            }
        }

        private void UpdateButtons()
        {
            bool hasWWW = !string.IsNullOrEmpty(_wwwAddress);
            bool hasTargetObject = _targetObject != null;
            bool hasFixEvent = _fixAction != null;

            _helpLabel.style.marginRight = 4;
            _buttonsVE.SetVisible(false);
            if (hasWWW || hasTargetObject || hasFixEvent)
            {
                _helpLabel.style.marginRight = 64;
                _wwwButton.SetVisible(hasWWW);
                _selectButton.SetVisible(hasTargetObject);
                _fixButton.SetVisible(hasFixEvent && !Application.isPlaying);
                _buttonsVE.SetVisible(true);
            }
        }

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<GPUIHelpBox, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription _helpText = new UxmlStringAttributeDescription
            {
                name = "help-text", defaultValue = "Help Text"
            };

            private UxmlEnumAttributeDescription<HelpBoxMessageType> _messageType = new UxmlEnumAttributeDescription<HelpBoxMessageType>
            {
                name = "message-type",
                defaultValue = HelpBoxMessageType.Info
            };

            private UxmlStringAttributeDescription _wwwAddress = new UxmlStringAttributeDescription
            {
                name = "www-address"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                GPUIHelpBox helpBox = ve as GPUIHelpBox;
                helpBox.helpText = _helpText.GetValueFromBag(bag, cc);
                helpBox.messageType = _messageType.GetValueFromBag(bag, cc);
                helpBox.wwwAddress = _wwwAddress.GetValueFromBag(bag, cc);
            }
        }
#endif
    }
}