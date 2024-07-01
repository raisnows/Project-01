using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;



public class DownloadInfo
{
    public List<string> DownloadedFileNames = new List<string>();
}
public class Downloader 
{
    /// <summary>
    /// 文件服务器地址
    /// </summary>
    string URL = null;

    /// <summary>
    /// 文件保存路径
    /// </summary>
    string SavePath = null;

    /// <summary>
    /// 具体的下载实例
    /// </summary>
    UnityWebRequest request=null;

    /// <summary>
    /// 由我们自己实现的下载处理类
    /// </summary>
    DownloadHandler downloadHandler = null;

    ErrorEventHandler OnError;

    ProgressEventHandler OnProgress;

    CompletedEventHnalder OnCompleted;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="savePath"></param>
    /// <param name="onCompleted"></param>
    /// <param name="onProgress"></param>
    /// <param name="onError"></param>
    public Downloader(string url,string savePath,CompletedEventHnalder onCompleted,ProgressEventHandler onProgress,ErrorEventHandler onError)
    {
        this.URL = url;
        this.SavePath = savePath;
        this.OnCompleted = onCompleted;
        this.OnProgress = onProgress;
        this.OnError = onError;
    }


    public async UniTask StartDownload()
    {
        request = UnityWebRequest.Get(URL);
        if (!string.IsNullOrEmpty(SavePath))
        {
            request.timeout = 2;

            request.disposeDownloadHandlerOnDispose = true;

            downloadHandler = new DownloadHandler(SavePath, OnCompleted, OnProgress, OnError);

            //因为currentLength会在实例化以及写入临时文件时更新,所以始终可以表达临时文件的长度
            request.SetRequestHeader("range", $"bytes={downloadHandler.CurrentLength}-");


            request.downloadHandler = downloadHandler;
        }
        request.SendWebRequest();


        while (!request.isDone)
        {
            Debug.Log(request.downloadProgress);
            await UniTask.Yield(PlayerLoopTiming.LastUpdate);
        }

    }


    public void Dispose()
    {
        OnError = null;
        OnCompleted = null;
        OnProgress = null;
        if (request != null)
        {
            if (!request.isDone)
            {
                //放弃本次请求
                request.Abort();
            }
            request.Dispose();
            request = null;
        }
    }
}
