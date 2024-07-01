using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Unity中不包括的报错类型
/// 由我们自己定义
/// </summary>
public enum ErrorCode
{
    /// <summary>
    /// 下载内容为空
    /// </summary>
    DownloadFileEmpty,
    /// <summary>
    /// 临时文件丢失
    /// </summary>
    TempFileMissing
}

/// <summary>
/// 无参,无返回值的委托
/// 委托的实质,就是声明一种特定返回值,特定参数的函数,但不具体指定某一个
/// 任何符合规则的函数,都可以是某个委托
/// 任何符合规则的函数,都可以委托给某个委托实例(委托变量)来调用
/// 这样就实现了,将某种特定规则的函数,像变量一样传递的功能;
/// 所谓函数的规则,起始就是一个函数由什么类型的返回值,和具体哪几个参数规定
/// </summary>
public delegate void SampleDelegate(string content);

/// <summary>
/// 下载错误时执行
/// </summary>
/// <param name="errorCode"></param>
/// <param name="message"></param>
public delegate void ErrorEventHandler(ErrorCode errorCode,string message);

/// <summary>
/// 下载完成时执行
/// </summary>
/// <param name="message"></param>
public delegate void CompletedEventHnalder(string fileName,string message);

/// <summary>
/// 下载进度更新时执行
/// </summary>
/// <param name="progree"></param>
/// <param name="currentLength"></param>
/// <param name="totalLength"></param>
public delegate void ProgressEventHandler(float progree, long currentLength, long totalLength);
public class DownloadHandler : DownloadHandlerScript
{
    /// <summary>
    /// 下载完成后保存的路径
    /// </summary>
    string SavePath;

    /// <summary>
    /// 临时文件储存路径
    /// </summary>
    string TempPath;

    /// <summary>
    /// 代表临时文件的大小(字节长度)
    /// 也就是本次下载的起始位置
    /// </summary>
    long currentLength = 0;

    /// <summary>
    /// 文件的总大小(字节长度)
    /// </summary>
    long totalLength = 0;

    /// <summary>
    /// 本次需要下载的字节长度
    /// </summary>
    long contentLength = 0;

    /// <summary>
    /// 文件读写流
    /// </summary>
    FileStream fileStream = null;

    /// <summary>
    /// 报错时的回调函数
    /// 委托类型
    /// </summary>
    ErrorEventHandler OnError = null;

    /// <summary>
    /// 下载完成时执行的回调函数
    /// </summary>
    CompletedEventHnalder OnCompleted = null;


    /// <summary>
    /// 下载进度更新时执行的回调函数
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
        //在构造函数中,this关键字代表这次构造 过程中声明的对应类型的实例
        this.SavePath = savePath;

        //原本的文件路径下,额外创建一个.temp文件
        this.TempPath = savePath + ".temp";

        this.OnCompleted = onCompleted;

        this.OnProgress = onProgree;

        this.OnError = onError;

        this.fileStream = new FileStream(this.TempPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        //如果是Create,长度为0
        this.currentLength = this.fileStream.Length;

        //除了下载之外,写入文件也要从已写入最大长度继续往下写
        this.fileStream.Position = this.currentLength;
    }


    /// <summary>
    /// 当设置Header时调用该方法
    /// </summary>
    /// <param name="contentLength"></param>
    protected override void ReceiveContentLengthHeader(ulong contentLength)
    {
        this.contentLength =(long) contentLength;
        //一个文件的总长度=已经下载长度+未下载长度
        this.totalLength = this.contentLength + currentLength;
    }

    /// <summary>
    /// 在每次从服务器上收到消息时会调用
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

        //这里是0和length都指的是datas的位置
        this.fileStream.Write(datas, 0, dataLegnth);

        currentLength += dataLegnth;

        //乘以1.0f是为了隐式转换成float类型
        OnProgress?.Invoke(currentLength * 1.0f/totalLength,currentLength,totalLength);

        return true;
    }

    /// <summary>
    /// 当下载完成时会被调用的函数
    /// </summary>
    protected override void CompleteContent()
    {
        FileStreamClose();

        if (contentLength <= 0)
        {
            OnError.Invoke(ErrorCode.DownloadFileEmpty, "下载内容长度为0");
            return;
        }

        if (!File.Exists(TempPath))
        {
            OnError.Invoke(ErrorCode.TempFileMissing, "临时文件丢失");
            return;
        }

        //将保存地址的文件删除
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }


        //move方法同时也带了重新命名的功能
        //因为path中要求指定具体的文件名称
        File.Move(TempPath, SavePath);

        FileInfo fileInfo = new FileInfo(SavePath);
        OnCompleted.Invoke(fileInfo.Name,"下载完成");
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
