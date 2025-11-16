using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.Networking;

namespace Chris.Gameplay.Mod
{
    public class ModDownloader : IDisposable, IProgress<float>
    {
        public Subject<float> OnProgress { get; }= new();

        public Subject<Result> OnComplete { get; } = new();

        private readonly CancellationToken _cancellationToken;
        
        public ModDownloader(CancellationToken cancellationToken = default)
        {
            _cancellationToken = cancellationToken;
        }
        
        public async UniTask DownloadModAsync(string url, string downloadPath)
        {
            Result result = new();
            using UnityWebRequest request = UnityWebRequest.Get(new Uri(url).AbsoluteUri);
            request.downloadHandler = new DownloadHandlerFile(downloadPath);
            await request.SendWebRequest().ToUniTask(this, cancellationToken: _cancellationToken);
            string unzipFolder = Path.GetDirectoryName(downloadPath);
            if (!ZipWrapper.UnzipFile(downloadPath, unzipFolder))
            {
                result.ErrorInfo = $"Can't unzip mod: {downloadPath}!";
                File.Delete(downloadPath);
                result.DownloadPath = downloadPath[..4];
                OnComplete.OnNext(result);
                return;
            }
            result.Succeed = true;
            OnComplete.OnNext(result);
        }
        
        void IProgress<float>.Report(float value)
        {
            OnProgress.OnNext(value);
        }
        
        public void Dispose()
        {
            OnProgress.Dispose();
            OnComplete.Dispose();
        }
        
        public struct Result
        {
            public string ErrorInfo;
            
            public bool Succeed;
            
            public string DownloadPath;
        }
    }
}