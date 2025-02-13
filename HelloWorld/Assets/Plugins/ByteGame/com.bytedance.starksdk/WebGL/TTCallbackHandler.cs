using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TTSDK
{
    struct TTCallbackKeyPair
    {
        public string success;
        public string fail;
    }
    
    class TTCallbackKeyPairDictionary<T>
    {
        private readonly Dictionary<T, T> k2v = new Dictionary<T, T>();
        private readonly Dictionary<T, T> v2k = new Dictionary<T, T>();

        public void Add(T key, T value)
        {
            if (k2v.ContainsKey(key) || v2k.ContainsKey(key))
            {
                throw new ArgumentException("Duplicate key or value.");
            }
            
            k2v.Add(key, value);
            v2k.Add(value, key);
        }

        public bool Remove(T key)
        {
            // Due to old compiler version, cannot simplify it.
            if (k2v.TryGetValue(key, out var value))
            {
                k2v.Remove(key);
                v2k.Remove(value);
                return true;
            }

            if (v2k.TryGetValue(key, out value))
            {
                v2k.Remove(key);
                k2v.Remove(value);
                return true;
            }

            return false;
        }

        public bool TryGetValue(T key, out T value)
        {
            return k2v.TryGetValue(key, out value) || v2k.TryGetValue(key, out value);
        }
    }
    
    class TTCallbackHandler
    {
        private static readonly Hashtable responseHT = new Hashtable();

        private static readonly TTCallbackKeyPairDictionary<string> keyPairs = new TTCallbackKeyPairDictionary<string>();

        private static int htCounter = 0;

        private static int GenarateCallbackId()
        {
            if (htCounter > 1000000)
            {
                htCounter = 0;
            }

            htCounter++;
            return htCounter;
        }
        
        public static TTCallbackKeyPair AddPair<T>(Action<T> success, Action<T> fail)
            where T : TTBaseResponse
        {
            var res = new TTCallbackKeyPair
            {
                success = Add(success),
                fail = Add(fail)
            };
            
            keyPairs.Add(res.success, res.fail);
            return res;
        }

        public static string Add<T>(Action<T> callback) where T : TTBaseResponse
        {
            if (callback == null)
            {
                return "";
            }
            var key = MakeKey();
            responseHT.Add(key, callback);
            return key;
        }

        public static string MakeKey()
        {
            int id = GenarateCallbackId();
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var timestamp = Convert.ToInt64(ts.TotalSeconds);
            var key = timestamp.ToString() + '-' + id;
            return key;
        }

        public static void InvokeResponseCallback<T>(string str) where T : TTBaseResponse
        {
            if (!string.IsNullOrEmpty(str))
            {
                T res = JsonUtility.FromJson<T>(str);
                var id = res.callbackId;

                Callback(id, res);
            }
        }

        public static void Callback<T>(string id, T res)
        {
            if (responseHT.ContainsKey(id))
            {
                var callback = (Action<T>) responseHT[id];
                callback(res);
                responseHT.Remove(id);
            }
            else
            {
                Debug.LogError($"callback id not found, id: {id}");
            }

            if (keyPairs.TryGetValue(id, out var pairId))
            {
                keyPairs.Remove(id);
                responseHT.Remove(pairId);
            }
        }
    }
}