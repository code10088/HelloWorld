using Luban;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIText), true), CanEditMultipleObjects]
public class UITextEditor : TMP_EditorPanelUI
{
    static Dictionary<int, string> languageDic = new Dictionary<int, string>();
    SerializedProperty m_i18nKeyProperty;

    protected override void OnEnable()
    {
        base.OnEnable();
        m_i18nKeyProperty = serializedObject.FindProperty("i18nKey");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();

        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(m_i18nKeyProperty);
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            if (languageDic.TryGetValue(((UIText)target).i18nKey, out var val))
            {
                foreach (var t in targets)
                {
                    ((UIText)t).text = val;
                    EditorUtility.SetDirty(t);
                }
            }
        }
    }

    [InitializeOnLoadMethod]
    public static void ChangeLanguage()
    {
        ChangeLanguage((LanguageType)EditorPrefs.GetInt(PlayerPrefsConst.Language, (int)LanguageType.CN));
    }
    public static void ChangeLanguage(LanguageType type)
    {
        var path = $"Assets/ZRes/DataConfig/tblanguage{type.ToString().ToLower()}.bytes";
        var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (ta == null) return;
        ByteBuf buf = new ByteBuf(ta.bytes);
        int n = buf.ReadSize();
        for (int i = n; i > 0; --i)
        {
            int key = buf.ReadInt();
            string value = buf.ReadString();
            languageDic[key] = value;
        }
    }
}