using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityExtensions.Editor;
#endif

namespace UnityExtensions.Tween
{
    [Serializable, TweenAnimation("Rendering/Material Property", "Material Property")]
    public class TweenMaterialProperty : TweenFromTo<Vector4>
    {
        public enum Type
        {
            Color = 0,
            Vector = 1,
            Float = 2,
            Range = 3
        }

        public Renderer target;

        [SerializeField] int _materialMask = ~0;
        [SerializeField] string _propertyName = default;
        [SerializeField] Type _propertyType = default;

        int _propertyID = -1;

        public string propertyName => _propertyName;
        public Type propertyType => _propertyType;
        public bool allMaterialSelected => _materialMask == ~0;
        public bool noneMaterialSelected => _materialMask == 0;
        public void SelectAllMaterials() => _materialMask = ~0;
        public void DeselectAllMaterials() => _materialMask = 0;
        public bool IsMaterialSelected(int materialIndex) => _materialMask.GetBit(materialIndex);
        public void SetMaterialSelected(int materialIndex, bool selected) => _materialMask.SetBit(materialIndex, selected);

        public int propertyID
        {
            get
            {
                if (_propertyID == -1 && !string.IsNullOrEmpty(_propertyName))
                {
                    _propertyID = Shader.PropertyToID(_propertyName);
                }
                return _propertyID;
            }
        }

        public void SetProperty(string name, Type type)
        {
            _propertyName = name;
            _propertyType = type;
            _propertyID = -1;
        }

        public override void Interpolate(float factor)
        {
            if (propertyID != -1 && target)
            {
                var value = Vector4.LerpUnclamped(from, to, factor);

                using (var properties = RuntimeUtilities.materialPropertyBlockPool.GetTemp())
                {
                    if (allMaterialSelected)
                    {
                        target.GetPropertyBlock(properties);
                        SetPropertyBlockValue();
                        target.SetPropertyBlock(properties);
                    }
                    else
                    {
                        int materialCount = GetMaterials();

                        for (int i = 0; i < materialCount; i++)
                        {
                            if (IsMaterialSelected(i))
                            {
                                target.GetPropertyBlock(properties, i);
                                SetPropertyBlockValue();
                                target.SetPropertyBlock(properties, i);
                            }
                        }
                    }

                    void SetPropertyBlockValue()
                    {
                        switch (_propertyType)
                        {
                            case Type.Color:
                            case Type.Vector:
                                properties.item.SetVector(propertyID, value);
                                return;

                            case Type.Float:
                            case Type.Range:
                                properties.item.SetFloat(propertyID, value.x);
                                return;
                        }
                    }
                }
            }
        }

        static List<Material> _materials = new List<Material>(8);

        int GetMaterials()
        {
            _materials.Clear();
            if (target) target.GetSharedMaterials(_materials);
            return Mathf.Min(_materials.Count, 32);
        }

#if UNITY_EDITOR

        Renderer _originalTarget;
        List<MaterialPropertyBlock> _tempBlocks;
        static StringBuilder _builder = new StringBuilder();

        public override void Reset(TweenPlayer player)
        {
            base.Reset(player);
            player.TryGetComponent(out target);
            _materialMask = ~0;
            _propertyName = null;
            _propertyType = Type.Color;
            _propertyID = -1;
            from = Color.white;
            to = Color.white;
        }

        public override void OnValidate(TweenPlayer player)
        {
            _propertyID = -1;
        }

        public override void RecordState()
        {
            _originalTarget = target;
            if (target)
            {
                if (_tempBlocks == null) _tempBlocks = new List<MaterialPropertyBlock>();

                int materialCount = GetMaterials();
                for (int i = 0; i < materialCount; i++)
                {
                    _tempBlocks.Add(RuntimeUtilities.materialPropertyBlockPool.Spawn());
                    target.GetPropertyBlock(_tempBlocks[i], i);
                }

                _tempBlocks.Add(RuntimeUtilities.materialPropertyBlockPool.Spawn());
                target.GetPropertyBlock(_tempBlocks[_tempBlocks.Count - 1]);
            }
        }

        public override void RestoreState()
        {
            if (_originalTarget)
            {
                var currentTarget = target;
                target = _originalTarget;

                target.SetPropertyBlock(_tempBlocks[_tempBlocks.Count - 1].isEmpty ? null : _tempBlocks[_tempBlocks.Count - 1]);

                int materialCount = GetMaterials();
                int count = Mathf.Min(_tempBlocks.Count - 1, materialCount);
                for (int i = 0; i < count; i++)
                {
                    target.SetPropertyBlock(_tempBlocks[i].isEmpty ? null : _tempBlocks[i], i);
                }

                target = currentTarget;
            }

            foreach (var item in _tempBlocks)
            {
                RuntimeUtilities.materialPropertyBlockPool.Despawn(item);
            }
            _tempBlocks.Clear();
        }

        struct Property : IEquatable<Property>
        {
            public string name;
            public ShaderUtil.ShaderPropertyType type;

            public bool Equals(Property other) => type == other.type && name == other.name;
            public override bool Equals(object obj) => throw new Exception("What are you doing?");
            public static bool operator ==(Property a, Property b) => a.type == b.type && a.name == b.name;
            public static bool operator !=(Property a, Property b) => a.type != b.type || a.name != b.name;
            public override int GetHashCode() => name.GetHashCode() & (~(int)type);
        }

        void DrawMaterialMask(int materialCount, TweenPlayer player)
        {
            int count = 0;

            if (allMaterialSelected) _builder.Append("All (Apply to Renderer)");
            else if (noneMaterialSelected) _builder.Append("None");
            else
            {
                for (int i = 0; i < materialCount; i++)
                {
                    if (IsMaterialSelected(i))
                    {
                        count++;

                        if (count > 1)
                        {
                            _builder.Append(", ");
                            if (count == 4)
                            {
                                _builder.Append("...");
                                break;
                            }
                        }

                        _builder.Append(i);
                        _builder.Append(": ");
                        _builder.Append(_materials[i] ? _materials[i].name : "(None)");
                    }
                }
            }

            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, EditorGUIUtilities.TempContent("Materials"));

            if (GUI.Button(rect, _builder.ToString(), EditorStyles.layerMaskField))
            {
                GenericMenu menu = new GenericMenu();

                menu.AddItem(new GUIContent("All (Apply to Renderer)"), allMaterialSelected, () =>
                {
                    Undo.RecordObject(player, "Select Material");
                    SelectAllMaterials();
                });

                menu.AddItem(new GUIContent("None"), noneMaterialSelected, () =>
                {
                    Undo.RecordObject(player, "Select Material");
                    DeselectAllMaterials();
                });

                if (materialCount > 0) menu.AddSeparator(string.Empty);

                for (int i = 0; i < materialCount; i++)
                {
                    int index = i;

                    menu.AddItem(new GUIContent(index + ": " + (_materials[index] ? _materials[index].name : "(None)")),
                        IsMaterialSelected(index),
                        () =>
                        {
                            Undo.RecordObject(player, "Select Material");
                            SetMaterialSelected(index, !IsMaterialSelected(index));
                        });
                }

                menu.DropDown(rect);
            }

            _builder.Clear();
        }

        void DrawProperty(int materialCount, TweenPlayer player)
        {
            var rect = EditorGUILayout.GetControlRect();
            rect = EditorGUI.PrefixLabel(rect, EditorGUIUtilities.TempContent("Property"));

            if (!string.IsNullOrEmpty(propertyName))
            {
                _builder.Append(propertyName);
                _builder.Append(" (");
                _builder.Append(propertyType);
                _builder.Append(')');
            }

            if (GUI.Button(rect, _builder.ToString(), EditorStyles.layerMaskField))
            {
                var properties = new HashSet<Property>();
                var menu = new GenericMenu();

                for (int i = 0; i < materialCount; i++)
                {
                    if (IsMaterialSelected(i) && _materials[i] && _materials[i].shader)
                    {
                        var shader = _materials[i].shader;
                        int count = ShaderUtil.GetPropertyCount(shader);

                        for (int idx = 0; idx < count; idx++)
                        {
                            if (!ShaderUtil.IsShaderPropertyHidden(shader, idx))
                            {
                                var prop = new Property
                                {
                                    name = ShaderUtil.GetPropertyName(shader, idx),
                                    type = ShaderUtil.GetPropertyType(shader, idx)
                                };

                                if (properties.Contains(prop)) continue;
                                properties.Add(prop);

                                string description = ShaderUtil.GetPropertyDescription(shader, idx);

                                if (prop.type == ShaderUtil.ShaderPropertyType.TexEnv)
                                {
                                    prop.name += "_ST";
                                    prop.type = ShaderUtil.ShaderPropertyType.Vector;
                                    description += " Scale and Offest";
                                }

                                _builder.Clear();
                                _builder.Append(prop.name);
                                _builder.Append(" (\"");
                                _builder.Append(description);
                                _builder.Append("\", ");
                                _builder.Append(prop.type);
                                _builder.Append(')');

                                menu.AddItem(new GUIContent(_builder.ToString()),
                                    _propertyName == prop.name && _propertyType == (Type)(int)prop.type,
                                    () =>
                                    {
                                        Undo.RecordObject(player, "Select Property");
                                        Type oldType = propertyType;
                                        SetProperty(prop.name, (Type)(int)prop.type);

                                        if (oldType != propertyType)
                                        {
                                            if (propertyType == Type.Color)
                                                from = to = Color.white;

                                            if (propertyType == Type.Float || propertyType == Type.Range)
                                                from.x = to.x = 1f;

                                            if (propertyType == Type.Vector)
                                            {
                                                if (prop.name.EndsWith("_ST"))
                                                    from = to = new Vector4(1, 1, 0, 0);
                                                else
                                                    from = to = new Vector4(1, 1, 1, 1);
                                            }
                                        }
                                    });

                                _builder.Clear();
                            }
                        }
                    }
                }

                if (properties.Count == 0) menu.AddItem(new GUIContent("(No Valid Property)"), false, () => { });

                menu.DropDown(rect);
            }

            _builder.Clear();
        }

        protected override void OnPropertiesGUI(TweenPlayer player, SerializedProperty property)
        {
            using (DisabledScope.New(player.playing))
            {
                EditorGUILayout.PropertyField(property.FindPropertyRelative(nameof(target)));
            }

            int materialCount = GetMaterials();
            DrawMaterialMask(materialCount, player);
            DrawProperty(materialCount, player);

            var (fromProp, toProp) = GetFromToProperties(property);

            switch (propertyType)
            {
                case Type.Float:
                case Type.Range:
                    FromToFieldLayout("Value", fromProp.FindPropertyRelative(nameof(Vector4.x)), toProp.FindPropertyRelative(nameof(Vector4.x)));
                    break;

                case Type.Vector:
                    FromToFieldLayout("X", fromProp.FindPropertyRelative(nameof(Vector4.x)), toProp.FindPropertyRelative(nameof(Vector4.x)));
                    FromToFieldLayout("Y", fromProp.FindPropertyRelative(nameof(Vector4.y)), toProp.FindPropertyRelative(nameof(Vector4.y)));
                    FromToFieldLayout("Z", fromProp.FindPropertyRelative(nameof(Vector4.z)), toProp.FindPropertyRelative(nameof(Vector4.z)));
                    FromToFieldLayout("W", fromProp.FindPropertyRelative(nameof(Vector4.w)), toProp.FindPropertyRelative(nameof(Vector4.w)));
                    break;

                case Type.Color:
                    var rect = EditorGUILayout.GetControlRect();
                    float labelWidth = EditorGUIUtility.labelWidth;

                    var fromRect = new Rect(rect.x + labelWidth, rect.y, (rect.width - labelWidth - 8) / 2, rect.height);
                    var toRect = new Rect(rect.xMax - fromRect.width, fromRect.y, fromRect.width, fromRect.height);
                    rect.width = labelWidth - 8;

                    EditorGUI.LabelField(rect, "Color");

                    using (LabelWidthScope.New(12))
                    {
                        fromProp.vector4Value = EditorGUI.ColorField(fromRect, EditorGUIUtilities.TempContent("F"), fromProp.vector4Value, false, true, true);
                        toProp.vector4Value = EditorGUI.ColorField(toRect, EditorGUIUtilities.TempContent("T"), toProp.vector4Value, false, true, true);
                    }
                    break;
            }
        }

#endif // UNITY_EDITOR

    } // class TweenMaterialProperty

} // namespace UnityExtensions.Tween