using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Unity�в������ı�������
/// �������Լ�����
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// ��������Ϊ��
    /// </summary>
    DownloadFileEmpty,
    /// <summary>
    /// ��ʱ�ļ���ʧ
    /// </summary>
    TempFileMissing
}

/// <summary>
/// �޲�,�޷���ֵ��ί��
/// ί�е�ʵ��,��������һ���ض�����ֵ,�ض������ĺ���,��������ָ��ĳһ��
/// �κη��Ϲ���ĺ���,��������ĳ��ί��
/// �κη��Ϲ���ĺ���,������ί�и�ĳ��ί��ʵ��(ί�б���)������
/// ������ʵ����,��ĳ���ض�����ĺ���,�����һ�����ݵĹ���;
/// ��ν�����Ĺ���,��ʼ����һ��������ʲô���͵ķ���ֵ,�;����ļ��������涨
/// </summary>
public delegate void SampleDelegate(string content);

/// <summary>
/// ���ش���ʱִ��
/// </summary>
/// <param name="errorCode"></param>
/// <param name="message"></param>
public delegate void ErrorEventHandler(ErrorCode errorCode,string message);

/// <summary>
/// �������ʱִ��
/// </summary>
/// <param name="message"></param>
public delegate void CompletedEventHnalder(string fileName,string message);

/// <summary>
/// ���ؽ��ȸ���ʱִ��
/// </summary>
/// <param name="progree"></param>
/// <param name="currentLength"></param>
/// <param name="totalLength"></param>
public delegate void ProgressEventHandler(float progree, long currentLength, long totalLength);
public class DownloadHandler : DownloadHandlerScript
{
    /// <summary>
    /// ������ɺ󱣴��·��
    /// </summary>
    string SavePath;

    /// <summary>
    /// ��ʱ�ļ�����·��
    /// </summary>
    string TempPath;

    /// <summary>
    /// ������ʱ�ļ��Ĵ�С(�ֽڳ���)
    /// Ҳ���Ǳ������ص���ʼλ��
    /// </summary>
    long currentLength = 0;

    /// <summary>
    /// �ļ����ܴ�С(�ֽڳ���)
    /// </summary>
    long totalLength = 0;

    /// <summary>
    /// ������Ҫ���ص��ֽڳ���
    /// </summary>
    long contentLength = 0;

    /// <summary>
    /// �ļ���д��
    /// </summary>
    FileStream fileStream = null;

    /// <summary>
    /// ����ʱ�Ļص�����
    /// ί������
    /// </summary>
    ErrorEventHandler OnError = null;

    /// <summary>
    /// �������ʱִ�еĻص�����
    /// </summary>
    CompletedEventHnalder OnCompleted = null;


    /// <summary>
    /// ���ؽ��ȸ���ʱִ�еĻص�����
    /// </summary>
    ProgressEventHandler OnProgress = null;


    public long CurrentLength { 
        get { return currentLength;} 
    }

    public long TotalLength {
        get { return totalLength; }  
    }



    public DownloadHandler(string savePath,CompletedEventHnalder onCompleted,ProgressEventHandler onProgree,ErrorEventHandler onError) : base(new byte[1024*1024])
    {
        //�ڹ��캯����,this�ؼ��ִ�����ι��� �����������Ķ�Ӧ���͵�ʵ��
        this.SavePath = savePath;

        //ԭ�����ļ�·����,���ⴴ��һ��.temp�ļ�
        this.TempPath = savePath + ".temp";

        this.OnCompleted = onCompleted;

        this.OnProgress = onProgree;

        this.OnError = onError;

        this.fileStream = new FileStream(this.TempPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        //�����Create,����Ϊ0
        this.currentLength = this.fileStream.Length;

        //��������֮��,д���ļ�ҲҪ����д����󳤶ȼ�������д
        this.fileStream.Position = this.currentLength;
    }


    /// <summary>
    /// ������Headerʱ���ø÷���
    /// </summary>
    /// <param name="contentLength"></param>
    protected override void ReceiveContentLengthHeader(ulong contentLength)
    {
        this.contentLength =(long) contentLength;
        //һ���ļ����ܳ���=�Ѿ����س���+δ���س���
        this.totalLength = this.contentLength + currentLength;
    }

    /// <summary>
    /// ��ÿ�δӷ��������յ���Ϣʱ�����
    /// </summary>
    /// <param name="datas"></param>
    /// <param name="dataLegnth"></param>
    /// <returns></returns>
    protected override bool ReceiveData(byte[] datas,int dataLegnth)
    {
        if(contentLength<=0 || datas == null || datas.Length <= 0)
        {
            return false;
        }

        //������0��length��ָ����datas��λ��
        this.fileStream.Write(datas, 0, dataLegnth);

        currentLength += dataLegnth;

        //����1.0f��Ϊ����ʽת����float����
        OnProgress?.Invoke(currentLength * 1.0f/totalLength,currentLength,totalLength);

        return true;
    }

    /// <summary>
    /// ���������ʱ�ᱻ���õĺ���
    /// </summary>
    protected override void CompleteContent()
    {
        FileStreamClose();

        if (contentLength <= 0)
        {
            OnError.Invoke(ErrorCode.DownloadFileEmpty, "�������ݳ���Ϊ0");
            return;
        }

        if (!File.Exists(TempPath))
        {
            OnError.Invoke(ErrorCode.TempFileMissing, "��ʱ�ļ���ʧ");
            return;
        }

        //�������ַ���ļ�ɾ��
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }


        //move����ͬʱҲ�������������Ĺ���
        //��Ϊpath��Ҫ��ָ��������ļ�����
        File.Move(TempPath, SavePath);

        FileInfo fileInfo = new FileInfo(SavePath);
        OnCompleted.Invoke(fileInfo.Name,"�������");
    }

    public  void dispose()
    {
        base.Dispose();
        FileStreamClose();
    }


    void FileStreamClose()
    {
        if (fileStream == null)
        {
            return;
        }

        fileStream.Close();
        fileStream.Dispose();
        fileStream = null;

    }
}
