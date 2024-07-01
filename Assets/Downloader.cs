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
    /// �ļ���������ַ
    /// </summary>
    string URL = null;

    /// <summary>
    /// �ļ�����·��
    /// </summary>
    string SavePath = null;

    /// <summary>
    /// ���������ʵ��
    /// </summary>
    UnityWebRequest request=null;

    /// <summary>
    /// �������Լ�ʵ�ֵ����ش�����
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

            //��ΪcurrentLength����ʵ�����Լ�д����ʱ�ļ�ʱ����,����ʼ�տ��Ա����ʱ�ļ��ĳ���
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
                //������������
                request.Abort();
            }
            request.Dispose();
            request = null;
        }
    }
}
