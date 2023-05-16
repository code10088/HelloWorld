using System;
using System.Collections.Generic;
using UnityEngine;

namespace xasset.editor
{
    [Serializable]
    public class ChangeRecord
    {
        public string name;
        public string[] changes;
        public ulong size;
        public long timestamp;
    }

    public class Changes : ScriptableObject, ISerializationCallbackReceiver
    {
        public const string Filename = "changes.json";
        public List<ChangeRecord> data = new List<ChangeRecord>();
        private readonly Dictionary<string, ChangeRecord> _data = new Dictionary<string, ChangeRecord>();

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            _data.Clear();
            foreach (var record in data)
            {
                _data[record.name] = record;
            }
        }

        public void Set(string file, string[] changes, ulong size)
        {
            if (!_data.TryGetValue(file, out var value))
            {
                value = new ChangeRecord()
                {
                    name = file,
                    changes = changes,
                    size = size,
                    timestamp = DateTime.Now.ToFileTime(),
                };
                _data[file] = value;
                data.Add(value);
            }
            else
            {
                Logger.W($"Record {file} Exist.");
            }
        }

        public bool TryGetValue(string file, out ChangeRecord value)
        {
            return _data.TryGetValue(file, out value);
        }
    }
}