
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
/// ��ΪAssets�µ������ű��ᱻ���뵽AssetmblyCharp.dll��
/// �����Ű�������ȥ����APK�������Բ�����ʹ������UnityEditor�����ռ��µķ���
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
        //���������ɵ��ļ������ͷ������ļ��������
        //������������
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
        Debug.Log($"���ؽ���:{progress * 100}%,��ǰ���س���:{currentLength * 1.0f / 1024 / 1024}M,�ļ��ܳ���:{totalLength * 1.0f / 1024 / 1024}M");

    }

    void OnError(ErrorCode errorCode, string message)
    {
        Debug.LogError(message);
    }

    void CreateAssetBundleDownloadList()
    {
        
        string assetBundleHashsPath = Path.Combine(DownloadVersionPath, "AssetBundleHashs");
        string assetBundleHashsString = File.ReadAllText(assetBundleHashsPath);

        //Զ�˰��б�
        string[] remoteAssetBundleHashs = JsonConvert.DeserializeObject<string[]>(assetBundleHashsString);


        //���ر��ȡ
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
            Debug.Log("���ر��ȡʧ��,ֱ������Զ�˱�");
            downloadAssetNames = remoteAssetBundleHashs.ToList();
        }
        else
        {
            AssetBundleVersionDiffrence diffrence = ContrastAssetBundleVersion(localAssetBundleHash, remoteAssetBundleHashs);
            downloadAssetNames = diffrence.AdditionAssetBundles;
        }

        //�����������
        downloadAssetNames.Add("LocalAssets");

        Downloader downloader = null;
        foreach (string assetBundleName in downloadAssetNames)
        {
            string fileName = assetBundleName;
            if (assetBundleName.Contains("_"))
            {
                //�»��ߺ�һλ����AssetBundleName
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
                OnCompleted(fileName, "�����Ѵ���");
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
                OnCompleted(packageName, "�����Ѵ���");
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
            OnCompleted("AssetBundleHashs", "�����Ѵ���");
        }
    }

    BuildInfo RemoteBuildInfo;
    IEnumerator GetRemoteVersion()
    {
        #region ��ȡԶ�˰汾��
        string remoteVersionFilePath = Path.Combine(HTTPAddress, "BuildOutput", "BuildVersion.version");

        UnityWebRequest request = UnityWebRequest.Get(remoteVersionFilePath);

        request.SendWebRequest();

        while (!request.isDone)
        {
            //����null����ȴ�һ֡
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
        //ʹ�ñ�������Զ�˰汾
        AssetManagerRuntime.Instance.RemoteAssetVersion = version;

        #endregion
        RemoteVersionPath = Path.Combine(HTTPAddress, "BuildOutput", AssetManagerRuntime.Instance.RemoteAssetVersion.ToString());
        DownloadVersionPath = Path.Combine(AssetManagerRuntime.Instance.DownloadPath, AssetManagerRuntime.Instance.RemoteAssetVersion.ToString());

        if (!Directory.Exists(DownloadVersionPath))
        {
            Directory.CreateDirectory(DownloadVersionPath);
        }
        Debug.Log(DownloadVersionPath);
        Debug.Log($"Զ����Դ�汾Ϊ{version}");

        #region ��ȡԶ��BuildInfo
        string remoteBuildInfoPath = Path.Combine(HTTPAddress, "BuildOutput", version.ToString(), "BuildInfo");

        request = UnityWebRequest.Get(remoteBuildInfoPath);

        request.SendWebRequest();

        while (!request.isDone)
        {
            //����null����ȴ�һ֡
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
        //���ȶ�ȡ���ص������б�
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

        //���Ȼ���Ҫ����AllPackages�Լ�Packages
        //������Ҫ�����ж�AllPackages�Ƿ��Ѿ�����;
        if (CurrentDownloadInfo.DownloadedFileNames.Contains("AllPackages"))
        {
            OnCompleted("AllPackages", "�����Ѵ���");
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
