using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine.Networking;

public class HttpManager : Singletion<HttpManager>
{
    private List<HttpItem> all = new List<HttpItem>();
    private int count = 0;

    public void Post(string url, byte[] send, Action<byte[]> receive, int timeout = 10)
    {
        all.Add(new HttpItem(url, send, receive, timeout));
        CheckHttpQueue();
    }
    public void CheckHttpQueue()
    {
        if (count < GameSetting.httpLimit && all.Count > 0)
        {
            HttpItem item = all[0];
            all.RemoveAt(0);
            item.Start();
            count++;
        }
    }

    public class HttpItem
    {
        private string url;
        private int timeout;
        private byte[] send;
        private Action<byte[]> receive;
        private byte[] result;
        private int retry = 0;

        public HttpItem(string _url, byte[] _send, Action<byte[]> _receive, int _timeout)
        {
            url = _url;
            timeout = _timeout;
            send = _send;
            receive = _receive;
        }
        public void Start()
        {
#if UNITY_WEBGL
            Driver.Instance.StartCoroutine(Request());
#else
            Driver.Instance.StartTask(Request, Finish, priority: 0);
#endif
        }
#if UNITY_WEBGL
        private IEnumerator Request()
        {
            var uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            uwr.timeout = timeout * 1000;
            uwr.uploadHandler = new UploadHandlerRaw(send);
            yield return uwr.SendWebRequest();
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                result = uwr.downloadHandler.data;
                uwr.Dispose();
                Finish();
            }
            else
            {
                uwr.Dispose();
                result = null;
                if (++retry > GameSetting.retryTime) yield break;
                Start();
            }
        }
#else
        private void Request(object o)
        {
            var hwr = WebRequest.Create(url) as HttpWebRequest;
            hwr.Timeout = timeout * 1000;
            hwr.Method = WebRequestMethods.Http.Post;
            using (Stream stream = hwr.GetRequestStream())
            {
                stream.Write(send, 0, send.Length);
            }
            var hwb = hwr.GetResponse() as HttpWebResponse;
            if (hwb.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = hwb.GetResponseStream();
                result = new byte[hwb.ContentLength];
                stream.Read(result, 0, result.Length);
                hwb.Dispose();
            }
            else
            {
                hwb.Dispose();
                result = null;
                if (++retry > GameSetting.retryTime) return;
                Request(null);
            }
        }
#endif
        private void Finish()
        {
            send = null;
            Instance.count--;
            receive?.Invoke(result);
            Instance.CheckHttpQueue();
        }
    }
}
