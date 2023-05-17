using System;
using System.Collections.Generic;
using System.Linq;

namespace xasset
{
    public class GetDownloadSizeRequest : Request
    {
        private readonly List<ManifestBundle> _bundles = new List<ManifestBundle>();
        private readonly List<DownloadContent> _contents = new List<DownloadContent>();
        private int _max;

        public Versions versions { get; set; }
        public ulong downloadSize { get; private set; }
        public string[] exclude { get; set; }

        public DownloadRequestBase DownloadAsync()
        {
            var request = DownloadRequestBatch.Create();
            foreach (var content in _contents)
                if (content.status != DownloadContent.Status.Downloaded)
                    request.AddContent(content);
            request.SendRequest();
            return request;
        }

        protected override void OnStart()
        {
            if (!Assets.Updatable)
            {
                SetResult(Result.Success);
                return;
            }

            foreach (var version in versions.data)
            {
                if (exclude == null)
                {
                    var bundles = version.manifest.bundles;
                    _bundles.AddRange(bundles);
                }
                else
                {
                    List<int> result = new List<int>();
                    var assets = version.manifest.assets;
                    for (int i = 0; i < assets.Length; i++)
                    {
                        if (exclude.Contains(assets[i].name)) Exclude(i, assets, result);
                    }
                    for (int i = 0; i < result.Count; i++)
                    {
                        _bundles.Add(version.manifest.bundles[result[i]]);
                    }
                }
            }

            _max = _bundles.Count;
        }
        private void Exclude(int id, ManifestAsset[] asset, List<int> result)
        {
            if (result.Contains(asset[id].bundle)) return;
            result.Add(asset[id].bundle);
            foreach (var item in asset[id].deps) Exclude(item, asset, result);
        }

        protected override void OnUpdated()
        {
            progress = (_max - _bundles.Count) * 1f / _max;
            while (_bundles.Count > 0)
            {
                AddContent(_bundles[0]);
                _bundles.RemoveAt(0);
                if (Scheduler.Busy) return;
            }

            SetResult(Result.Success);
        }

        private void AddContent(ManifestBundle bundle)
        {
            var url = Assets.GetDownloadURL(bundle.file);
            var savePath = Assets.GetDownloadDataPath(bundle.file);
            var content = DownloadContent.Get(url, savePath, bundle.hash, bundle.size);
            content.status = DownloadContent.Status.Default;
            _contents.Add(content);
            if (!Assets.IsPlayerAsset(bundle.hash) && !Assets.IsDownloaded(bundle))
                downloadSize += content.downloadSize;
            else
                content.status = DownloadContent.Status.Downloaded;
        }
    }
}