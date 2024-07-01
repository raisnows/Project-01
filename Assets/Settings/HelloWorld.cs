
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

public static class PlayerData
{
    public static int MoveSpeed;
    public static int RotateSpeed;
}

/// <summary>
/// 因为Assets下的其他脚本会被编译到AssetmblyCharp.dll中
/// 跟随着包体打包出去（如APK），所以不允许使用来自UnityEditor命名空间下的方法
/// </summary>
public class HelloWorld : MonoBehaviour
{
    public AssetBundlePattern LoadPattern;

    AssetBundle CubeBundle;
    AssetBundle SphereBundle;
    GameObject SampleObejct;

    public Button LoadAssetBundleButton;
    public Button LoadAssetButton;
    public Button UnloadFalseButton;
    public Button UnloadTrueButton;


    public string HTTPAddress = "http://192.168.203.61:8080/";


    string RemoteVersionPath;
    string DownloadVersionPath;
    // Start is called before the first frame update
    void Start()
    {
        AssetManagerRuntime.AssetManagerInit(LoadPattern);
        if (LoadPattern == AssetBundlePattern.Remote)
        {
            StartCoroutine(GetRemoteVersion());
        }
        else
        {
            LoadAsset();
        }
    }

    void LoadAsset()
    {
        AssetPackage package = AssetManagerRuntime.Instance.LoadPackage("A");
        GameObject obj = package.LoadAsset<GameObject>("Assets/Resources/Capsule.prefab");

        Instantiate(obj);
    }

    DownloadInfo CurrentDownloadInfo;
    void OnCompleted(string fileName,string message)
    {
        if (!CurrentDownloadInfo.DownloadedFileNames.Contains(fileName))
        {
            CurrentDownloadInfo.DownloadedFileNames.Add(fileName);
            string downloadInfoString = JsonConvert.SerializeObject(CurrentDownloadInfo);
            string downloadSavePath = Path.Combine(AssetManagerRuntime.Instance.DownloadPath, "TempDownloadInfo");
            File.WriteAllText(downloadSavePath, downloadInfoString);
        }
        switch (fileName)
        {
            case "AllPackages":
                CreatePackagesDownloadList();
                break;
            case "AssetBundleHashs":
                CreateAssetBundleDownloadList();
                break;
        }
        //如果下载完成的文件数量和服务器文件数量相等
        //则代表下载完成
        if (CurrentDownloadInfo.DownloadedFileNames.Count == RemoteBuildInfo.FileNames.Count)
        {
            CopyDownloadAssetsToLocalPath();
            AssetManagerRuntime.Instance.UpdateLocalAssetVersion();
            LoadAsset();
        }
        Debug.Log($"{fileName}:{message}");
    }

    void OnProgress(float progress, long currentLength, long totalLength)
    {
        Debug.Log($"下载进度:{progress * 100}%,当前下载长度:{currentLength * 1.0f / 1024 / 1024}M,文件总长度:{totalLength * 1.0f / 1024 / 1024}M");

    }

    void OnError(ErrorCode errorCode, string message)
    {
        Debug.LogError(message);
    }

    void CreateAssetBundleDownloadList()
    {
        
        string assetBundleHashsPath = Path.Combine(DownloadVersionPath, "AssetBundleHashs");
        string assetBundleHashsString = File.ReadAllText(assetBundleHashsPath);

        //远端包列表
        string[] remoteAssetBundleHashs = JsonConvert.DeserializeObject<string[]>(assetBundleHashsString);


        //本地表读取
        string localAssetBundleHashPath = Path.Combine(AssetManagerRuntime.Instance.AssetBundleLoadPath, "AssetBundleHashs");
        string assetBundleHashString = "";

        string[] localAssetBundleHash = null;

        if (File.Exists(localAssetBundleHashPath))
        {
            assetBundleHashString = File.ReadAllText(localAssetBundleHashPath);

            localAssetBundleHash = JsonConvert.DeserializeObject<string[]>(assetBundleHashString);
        }

        List<string> downloadAssetNames = null;
        if (localAssetBundleHash == null)
        {
            Debug.Log("本地表读取失败,直接下载远端表");
            downloadAssetNames = remoteAssetBundleHashs.ToList();
        }
        else
        {
            AssetBundleVersionDiffrence diffrence = ContrastAssetBundleVersion(localAssetBundleHash, remoteAssetBundleHashs);
            downloadAssetNames = diffrence.AdditionAssetBundles;
        }

        //添加主包包名
        downloadAssetNames.Add("LocalAssets");

        Downloader downloader = null;
        foreach (string assetBundleName in downloadAssetNames)
        {
            string fileName = assetBundleName;
            if (assetBundleName.Contains("_"))
            {
                //下划线后一位才是AssetBundleName
                int startIndex = assetBundleName.IndexOf("_") + 1;
                fileName = assetBundleName.Substring(startIndex);
            }

            if (!CurrentDownloadInfo.DownloadedFileNames.Contains(fileName))
            {
                string fileURL = Path.Combine(RemoteVersionPath, fileName);
                string fileSavePath = Path.Combine(DownloadVersionPath, fileName);
                downloader = new Downloader(fileURL, fileSavePath, OnCompleted, OnProgress, OnError);
                downloader.StartDownload();
            }
            else
            {
                OnCompleted(fileName, "本地已存在");
            }
        }
    }
    void CreatePackagesDownloadList()
    {
        string allPackagesPath = Path.Combine(DownloadVersionPath, "AllPackages");
        string allPackagesString = File.ReadAllText(allPackagesPath);
        List<string> allPackages = JsonConvert.DeserializeObject<List<string>>(allPackagesString);

        Downloader downloader = null;

        foreach(string packageName in allPackages)
        {
            if (!CurrentDownloadInfo.DownloadedFileNames.Contains(packageName))
            {
                string remotePakcagePath = Path.Combine(RemoteVersionPath, packageName);
                string remotePakcageSavePath = Path.Combine(DownloadVersionPath, packageName);
                downloader = new Downloader(remotePakcagePath, remotePakcageSavePath, OnCompleted, OnProgress, OnError);
                downloader.StartDownload();
            }
            else
            {
                OnCompleted(packageName, "本地已存在");
            }
        }

        if (!CurrentDownloadInfo.DownloadedFileNames.Contains("AssetBundleHashs"))
        {
            string remoteFilePath = Path.Combine(RemoteVersionPath, "AssetBundleHashs");
            string remoteFileSavePath = Path.Combine(DownloadVersionPath, "AssetBundleHashs");
            downloader = new Downloader(remoteFilePath, remoteFileSavePath, OnCompleted, OnProgress, OnError);
            downloader.StartDownload();
        }
        else
        {
            OnCompleted("AssetBundleHashs", "本地已存在");
        }
    }

    BuildInfo RemoteBuildInfo;
    IEnumerator GetRemoteVersion()
    {
        #region 获取远端版本号
        string remoteVersionFilePath = Path.Combine(HTTPAddress, "BuildOutput", "BuildVersion.version");

        UnityWebRequest request = UnityWebRequest.Get(remoteVersionFilePath);

        request.SendWebRequest();

        while (!request.isDone)
        {
            //返回null代表等待一帧
            yield return null;
        }

        if (!string.IsNullOrEmpty(request.error))
        {
            Debug.LogError(request.error);
            yield break;
        }

        int version = int.Parse(request.downloadHandler.text);
        if (AssetManagerRuntime.Instance.LocalAssetVersion == version)
        {
            LoadAsset();
            yield break;
        }
        //使用变量保存远端版本
        AssetManagerRuntime.Instance.RemoteAssetVersion = version;

        #endregion
        RemoteVersionPath = Path.Combine(HTTPAddress, "BuildOutput", AssetManagerRuntime.Instance.RemoteAssetVersion.ToString());
        DownloadVersionPath = Path.Combine(AssetManagerRuntime.Instance.DownloadPath, AssetManagerRuntime.Instance.RemoteAssetVersion.ToString());

        if (!Directory.Exists(DownloadVersionPath))
        {
            Directory.CreateDirectory(DownloadVersionPath);
        }
        Debug.Log(DownloadVersionPath);
        Debug.Log($"远端资源版本为{version}");

        #region 获取远端BuildInfo
        string remoteBuildInfoPath = Path.Combine(HTTPAddress, "BuildOutput", version.ToString(), "BuildInfo");

        request = UnityWebRequest.Get(remoteBuildInfoPath);

        request.SendWebRequest();

        while (!request.isDone)
        {
            //返回null代表等待一帧
            yield return null;
        }

        if (!string.IsNullOrEmpty(request.error))
        {
            Debug.LogError(request.error);
            yield break;
        }

        string buildInfoString = request.downloadHandler.text;
        RemoteBuildInfo = JsonConvert.DeserializeObject<BuildInfo>(buildInfoString);

        if (RemoteBuildInfo == null || RemoteBuildInfo.FizeTotalSize <= 0)
        {
            yield break;
        }
        #endregion
        CreateDownloadList();
    }

   

    void CreateDownloadList()
    {
        //首先读取本地的下载列表
        string downloadInfoPath = Path.Combine(AssetManagerRuntime.Instance.DownloadPath, "TempDownloadInfo");
        if (File.Exists(downloadInfoPath))
        {
            string downloadInfoString = File.ReadAllText(downloadInfoPath);
            CurrentDownloadInfo = JsonConvert.DeserializeObject<DownloadInfo>(downloadInfoString);
        }
        else
        {
            CurrentDownloadInfo = new DownloadInfo();
        }

        //首先还是要下载AllPackages以及Packages
        //所以需要首先判断AllPackages是否已经下载;
        if (CurrentDownloadInfo.DownloadedFileNames.Contains("AllPackages"))
        {
            OnCompleted("AllPackages", "本地已存在");
        }
        else
        {
            string filePath = Path.Combine(RemoteVersionPath,"AllPackages");
            string savePath = Path.Combine(DownloadVersionPath, "AllPackages");
            Downloader downloader = new Downloader(filePath, savePath, OnCompleted, OnProgress, OnError);
            downloader.StartDownload();
        }
    }
    void CopyDownloadAssetsToLocalPath()
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(DownloadVersionPath);

        string localVersionPath = Path.Combine(AssetManagerRuntime.Instance.LocalAssetPath, AssetManagerRuntime.Instance.RemoteAssetVersion.ToString());

        directoryInfo.MoveTo(localVersionPath);

        string downloadInfoPath = Path.Combine(AssetManagerRuntime.Instance.DownloadPath, "TempDownloadInfo");
        File.Delete(downloadInfoPath);
    }
   
    AssetBundleVersionDiffrence ContrastAssetBundleVersion(string[] oldVersionAssets, string[] newVersionAssets)
    {
        AssetBundleVersionDiffrence diffrence = new AssetBundleVersionDiffrence();

        foreach (var assetName in oldVersionAssets)
        {
            if (!newVersionAssets.Contains(assetName))
            {
                diffrence.ReducedAssetBundles.Add(assetName);
            }
        }

        foreach (var assetName in newVersionAssets)
        {
            if (!oldVersionAssets.Contains(assetName))
            {
                diffrence.AdditionAssetBundles.Add(assetName);
            }
        }

        return diffrence;
    }
 
}
