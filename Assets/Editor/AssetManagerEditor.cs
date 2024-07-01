using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System;

/// <summary>
/// 这个类只用于收集在Editor环境下存在的Package信息
/// 而非打包之后的Package信息
/// </summary>
[Serializable]
public class PackageEditorInfo
{
    /// <summary>
    /// 当前包的名称
    /// 可以由开发者在编辑器窗口中自由定义
    /// </summary>
    public string PackageName;

    /// <summary>
    /// 归属于当前包中的资源列表
    /// 可以由开发者在编辑器窗口中自由定义
    /// </summary>
    public List<UnityEngine.Object> AssetList = new List<UnityEngine.Object>();
}

/// <summary>
/// 代表了Node之间的引用关系
/// 很显然的一个Node可能引用多个Node
/// 也可能被多个Node所引用
/// </summary>
public class AssetBundleEdge
{
    public List<AssetBundleNode> nodes = new List<AssetBundleNode>();
}
public class AssetBundleNode
{
    public string AssetName;

    /// <summary>
    /// 可以用于判断一个资源是否是SourceAsset
    /// 如果为-1说明是DerivedAsset
    /// </summary>
    public int SourceIndex = -1;

    /// <summary>
    /// 当前Node的Index列表
    /// 会沿着自身的OutEdge进行传递
    /// </summary>
    public List<int> SourceIndices = new List<int>();

    /// <summary>
    /// 只有SourceAsset才具有包名
    /// </summary>
    public string PackageName;


    /// <summary>
    /// DerivedAsset的只有PackageNames代表被引用关系
    /// </summary>
    public List<string> PackageNames=new List<string>();

    /// <summary>
    /// 引用当前Node的Nodes
    /// </summary>
    public AssetBundleEdge InEdge;
    /// <summary>
    /// 当前Node所引用的Nodes
    /// </summary>
    public AssetBundleEdge OutEdge;
}

/// <summary>
/// 所有在Editor目录下的C#脚本都不会跟着资源打包到可执行文件包体中
/// </summary>
public class AssetManagerEditor
{



    public static AssetManagerConfigScriptableObject AssetManagerConfig;




    /// <summary>
    /// 本次打包所有AssetBundle的输出路径
    /// 应包含主包包名,以适配增量打包
    /// </summary>
    public static string AssetBundleOutputPath;


    /// <summary>
    /// 代表了整个打包文件输出路径
    /// </summary>
    public static string BuildOutputPath;


    /// <summary>
    /// 通过MenuItem特性，声明Editor顶部菜单栏选项
    /// </summary>
    [MenuItem(nameof(AssetManagerEditor) + "/" + nameof(BuildAssetBundle))]
    static void BuildAssetBundle()
    {

        CheckBuildOutputPath();
        //PathCombine方法可以在几个字符串之间插入斜杠

        if (!Directory.Exists(AssetBundleOutputPath))
        {
            Directory.CreateDirectory(AssetBundleOutputPath);
        }

        //分别使用三种压缩方式,打包三个不同的AssetBundle
        //并输出在三个不同工程外部的文件夹中


        //不同平台之间的AssetBundle不可以通用
        //该方法会打包工程内所有配置了包名的AB包
        //Options为None时使用LZMA压缩
        //UncompressedAssetBundle不进行压缩
        //ChunkBasedCompression进行LZ4 块压缩
        BuildPipeline.BuildAssetBundles(AssetBundleOutputPath, CheckCompressionPattern(), EditorUserBuildSettings.activeBuildTarget);

        Debug.Log("AB包打包已完成");
    }

    public static void AddPacakgeInfoEditor()
    {
        AssetManagerConfig.packageEditorInfos.Add(new PackageEditorInfo());
    }

    public static void RemovePackageInfoEditor(PackageEditorInfo info)
    {
        if (AssetManagerConfig.packageEditorInfos.Contains(info))
        {
            AssetManagerConfig.packageEditorInfos.Remove(info);
        }
    }

    public static void AddAsset(PackageEditorInfo info)
    {
        info.AssetList.Add(null);
    }

    public static void RemoveAsset(PackageEditorInfo info, UnityEngine.Object asset)
    {
        if (info.AssetList.Contains(asset))
        {
            info.AssetList.Remove(asset);
        }
    }

    public static void LoadConfig(AssetManagerEditorWindow window)
    {
        if (AssetManagerConfig == null)
        {
            //使用AssetDataBase加载资源,只需要传入Assets目录下的路径即可
            AssetManagerConfig = AssetDatabase.LoadAssetAtPath<AssetManagerConfigScriptableObject>("Assets/Editor/AssetManagerConfig.asset");
            window.VersionString = AssetManagerConfig.AssetManagerVersion.ToString();

            for (int i = window.VersionString.Length - 1; i >= 1; i--)
            {
                window.VersionString = window.VersionString.Insert(i, ".");
            }



        }
    }

    public static void LoadWindowConfig(AssetManagerEditorWindow window)
    {
        if (window.WindowConfig == null)
        {
            //使用AssetDataBase加载资源,只需要传入Assets目录下的路径即可
            window.WindowConfig = AssetDatabase.LoadAssetAtPath<AssetManagerEditorWindowConfigSO>("Assets/Editor/AssetManagerEditorWindowConfig.asset");
            window.WindowConfig.TitleTextStyle = new GUIStyle();
            window.WindowConfig.TitleTextStyle.fontSize = 26;
            window.WindowConfig.TitleTextStyle.normal.textColor = Color.red;
            window.WindowConfig.TitleTextStyle.alignment = TextAnchor.MiddleCenter;

            window.WindowConfig.VersionTextStyle = new GUIStyle();
            window.WindowConfig.VersionTextStyle.fontSize = 20;
            window.WindowConfig.VersionTextStyle.normal.textColor = Color.cyan;
            window.WindowConfig.VersionTextStyle.alignment = TextAnchor.MiddleRight;


            window.WindowConfig.LogoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/a.png");
            window.WindowConfig.LogoTextureStyle = new GUIStyle();
            window.WindowConfig.LogoTextureStyle.alignment = TextAnchor.MiddleCenter;
        }
    }

    public static void LoadConfigFromJson()
    {
        string configPath = Path.Combine(Application.dataPath, "Editor/AssetManagerConfig.amc");

        string configString = File.ReadAllText(configPath);


        JsonUtility.FromJsonOverwrite(configString, AssetManagerConfig);

    }

    public static void SaveConfigToJson()
    {
        if (AssetManagerConfig != null)
        {
            string configString = JsonUtility.ToJson(AssetManagerConfig);
            string outputPath = Path.Combine(Application.dataPath, "Editor/AssetManagerConfig.amc");

            File.WriteAllText(outputPath, configString);


            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    [MenuItem(nameof(AssetManagerEditor) + "/" + nameof(CreateConfig))]
    static void CreateConfig()
    {
        //声明ScriptableObject类型的实例
        //ScriptableObject类型的声明,就类似于JSON中将某个类的实例序列化的过程
        AssetManagerConfigScriptableObject config = ScriptableObject.CreateInstance<AssetManagerConfigScriptableObject>();

        AssetDatabase.CreateAsset(config, "Assets/Editor/AssetManagerConfig.asset");

        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 返回由一个包中所有Asset的GUID列表经过MD5算法加密后得到的哈希码字符串
    /// 如果GUID列表不发生变化,以及加密算法和参数没有发生变化
    /// 那么总是能够得到相同的字符串
    /// </summary>
    /// <param name="assetNames"></param>
    /// <returns></returns>
    static string ComputeAssetSetSignature(IEnumerable<string> assetNames)
    {
        var assetGUIDs = assetNames.Select(AssetDatabase.AssetPathToGUID);

        MD5 currentMD5 = MD5.Create();

        foreach (var assetGUID in assetGUIDs.OrderBy(x => x))
        {
            byte[] bytes = Encoding.ASCII.GetBytes(assetGUID);

            //使用MD5算法加密字节数组
            currentMD5.TransformBlock(bytes, 0, bytes.Length, null, 0);
        }

        currentMD5.TransformFinalBlock(new byte[0], 0, 0);

        return BytesToHexString(currentMD5.Hash);
    }


    /// <summary>
    /// byte转16进制字符串
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    static string BytesToHexString(byte[] bytes)
    {
        StringBuilder stringBuilder = new StringBuilder();

        foreach (var aByte in bytes)
        {
            stringBuilder.Append(aByte.ToString("x2"));
        }

        return stringBuilder.ToString();
    }

    static string[] BuildAssetBundleHashTable(AssetBundleBuild[] assetBundleBuilds,string versionPath)
    {
        //表的长度与AssetBundle的数量保持一致
        string[] assetBundleHashs = new string[assetBundleBuilds.Length];

        for (int i = 0; i < assetBundleBuilds.Length; i++)
        {
            string assetBundlePath = Path.Combine(AssetBundleOutputPath, assetBundleBuilds[i].assetBundleName);

            FileInfo info = new FileInfo(assetBundlePath);

            //表中记录的,是一个AssetBundle文件的长度,以及其内容的MD5哈希值
            assetBundleHashs[i] = $"{info.Length}_{assetBundleBuilds[i].assetBundleName}";

        }

        string hashString = JsonConvert.SerializeObject(assetBundleHashs);
        string hashFilePath = Path.Combine(AssetBundleOutputPath, "AssetBundleHashs");
        string hashFileVersionPath = Path.Combine(versionPath, "AssetBundleHashs");
        File.WriteAllText(hashFilePath, hashString);
        File.WriteAllText(hashFileVersionPath, hashString);
        return assetBundleHashs;


    }
    public static void BuildAssetBundleFromDirectedGraph()
    {
        CheckBuildOutputPath();

        List<AssetBundleNode> allNodes = new List<AssetBundleNode>();

        int sourceIndex = 0;

        Dictionary<string, PackageBuildInfo> packageInfoDic = new Dictionary<string, PackageBuildInfo>();

        #region 有向图构建
        for (int i = 0; i < AssetManagerConfig.packageEditorInfos.Count; i++)
        {
            PackageBuildInfo packageBuildInfo = new PackageBuildInfo();
            packageBuildInfo.PackageName = AssetManagerConfig.packageEditorInfos[i].PackageName;

            packageBuildInfo.IsSourcePackage = true;

            packageInfoDic.Add(packageBuildInfo.PackageName, packageBuildInfo);

            //当前所选中的资源,就是SourceAsset
            //所以首先添加SourceAsset的Node
            foreach (UnityEngine.Object asset in AssetManagerConfig.packageEditorInfos[i].AssetList)
            {
                AssetBundleNode currentNode = null;
                //以资源的具体路径,作为资源名称
                string assetNamePath = AssetDatabase.GetAssetPath(asset);

                foreach (AssetBundleNode node in allNodes)
                {
                    if (node.AssetName == assetNamePath)
                    {
                        currentNode = node;
                        currentNode.PackageName = packageBuildInfo.PackageName;
                        break;
                    }
                }

                if (currentNode == null)
                {
                    currentNode = new AssetBundleNode();
                    currentNode.AssetName = assetNamePath;

                    currentNode.SourceIndex = sourceIndex;
                    currentNode.SourceIndices = new List<int>() { sourceIndex };

                    currentNode.PackageName = packageBuildInfo.PackageName;
                    currentNode.PackageNames.Add(currentNode.PackageName);

                    currentNode.InEdge = new AssetBundleEdge();
                    allNodes.Add(currentNode);
                }


                GetNodesFromDependencies(currentNode, allNodes);

                sourceIndex++;
            }
        }
        #endregion


        #region 有向图区分打包集合
        Dictionary<List<int>, List<AssetBundleNode>> assetBundleNodeDic = new Dictionary<List<int>, List<AssetBundleNode>>();

        foreach (AssetBundleNode node in allNodes)
        {
            StringBuilder packageNameString = new StringBuilder();

            //包名不为空或无,则代表是一个SourceAsset,其包名已经在编辑器窗口中添加了
            if (string.IsNullOrEmpty(node.PackageName))
            {
                for (int i = 0; i < node.PackageNames.Count; i++)
                {
                    packageNameString.Append(node.PackageNames[i]);
                    if (i < node.PackageNames.Count - 1)
                    {
                        packageNameString.Append("_");
                    }
                }

                string packageName = packageNameString.ToString();
                node.PackageName = packageName;

                //此时只添加了对应的包以及包名
                //而没有具体添加包中对应的Asset
                //因为Asset添加时需要具有AssetBundleName
                //所以只能在生成AssetBundleBuild的地方添加Asset
                if (!packageInfoDic.ContainsKey(packageName))
                {
                    PackageBuildInfo packageBuildInfo = new PackageBuildInfo();
                    packageBuildInfo.PackageName = packageName;
                    packageBuildInfo.IsSourcePackage = false;
                    packageInfoDic.Add(packageBuildInfo.PackageName, packageBuildInfo);
                }
            }


            bool isEquals = false;
            List<int> keyList = new List<int>();
            //遍历所有key
            //通过这样的方式就能确保,不同List之间,内容是否一致
            foreach (List<int> key in assetBundleNodeDic.Keys)
            {
                //判断key的长度是否和当前node的SourceIndices长度相等
                isEquals = node.SourceIndices.Count == key.Count && node.SourceIndices.All(p => key.Any(k => k.Equals(p)));

                if (isEquals)
                {
                    keyList = key;
                    break;
                }
            }
            if (!isEquals)
            {
                keyList = node.SourceIndices;
                assetBundleNodeDic.Add(node.SourceIndices, new List<AssetBundleNode>());
            }

            assetBundleNodeDic[keyList].Add(node);
        }
        #endregion


        AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[assetBundleNodeDic.Count];
        int buildIndex = 0;

        foreach (List<int> key in assetBundleNodeDic.Keys)
        {

            List<string> assetNames = new List<string>();
            //这一层循环,都是从同一个键值对中获取node
            //也就是从SourceIndices相同的集合中,获取对应的Node所代表的Asset
            foreach (AssetBundleNode node in assetBundleNodeDic[key])
            {
                assetNames.Add(node.AssetName);

                //如果是一个SourceAsset,则他的PackageName只会具有自己
                foreach(string packageName in node.PackageNames)
                {
                    if (packageInfoDic.ContainsKey(packageName))
                    {
                        if(!packageInfoDic[packageName].PackageDependecies.Contains(node.PackageName) && !string.Equals(node.PackageName, packageInfoDic[packageName].PackageName))
                        {
                            packageInfoDic[packageName].PackageDependecies.Add(node.PackageName);
                        }
                    }
                }
            }
            string[] assetNamesArray = assetNames.ToArray();

            assetBundleBuilds[buildIndex].assetBundleName = ComputeAssetSetSignature(assetNamesArray);

            assetBundleBuilds[buildIndex].assetNames = assetNamesArray;

            foreach (AssetBundleNode node in assetBundleNodeDic[key])
            {
                //因为区分了的DerivedPackage
                //所以此处可以确保,每一个Node都具有一个包名
                AssetBuildInfo assetBuildInfo = new AssetBuildInfo();

                assetBuildInfo.AssetName = node.AssetName;
                assetBuildInfo.AssetBundleName = assetBundleBuilds[buildIndex].assetBundleName;

                packageInfoDic[node.PackageName].AssetInfos.Add(assetBuildInfo);

            }
            buildIndex++;
        }


        BuildPipeline.BuildAssetBundles(AssetBundleOutputPath, assetBundleBuilds, CheckIncrementalBuildMode(), BuildTarget.StandaloneWindows);

        string buildVersionFilePath = Path.Combine(BuildOutputPath, "BuildVersion.version");
        File.WriteAllText(buildVersionFilePath, AssetManagerConfig.CurrentBuildVersion.ToString());

        //创建版本路径
        string versionPath = Path.Combine(BuildOutputPath, AssetManagerConfig.CurrentBuildVersion.ToString());

        if (!Directory.Exists(versionPath))
        {
            Directory.CreateDirectory(versionPath);
        }


        BuildAssetBundleHashTable(assetBundleBuilds,versionPath);

        CopyAssetBundleToVersionFolder(versionPath) ;


        BuildPackageTable(packageInfoDic,versionPath);

        CreateBuildInfo(versionPath);

        AssetManagerConfig.CurrentBuildVersion++;
        AssetDatabase.Refresh();
    }

    public static void CreateBuildInfo(string versionPath)
    {
        BuildInfo currentBuildInfo = new BuildInfo();
        currentBuildInfo.BuildVersion = AssetManagerConfig.CurrentBuildVersion;

        //获取AB包输出路径的文件夹信息
        DirectoryInfo directoryInfo = new DirectoryInfo(versionPath);
        //获取该文件夹下所有的文件信息
        FileInfo[] fileInfos = directoryInfo.GetFiles();

        //遍历该文件夹下所有文件,并收集所有文件的长度
        foreach(FileInfo fileInfo in fileInfos)
        {
            currentBuildInfo.FileNames.Add(fileInfo.Name, (ulong)fileInfo.Length);
            currentBuildInfo.FizeTotalSize += (ulong)fileInfo.Length;
        }

        string buildInfoSavePath = Path.Combine(versionPath, "BuildInfo");
        string buildInfoString = JsonConvert.SerializeObject(currentBuildInfo);

        File.WriteAllText(buildInfoSavePath, buildInfoString);
        //File.WriteAllTextAsync(buildInfoSavePath, buildInfoString);
    }

    public static string PackageTableName = "AllPackages";
    /// <summary>
    /// 
    /// </summary>
    /// <param name="packages">Package字典,key为包名</param>
    /// <param name="outputPath"></param>
    static void BuildPackageTable(Dictionary<string, PackageBuildInfo> packages,string versionPath)
    {

        string packagesPath = Path.Combine(AssetBundleOutputPath, PackageTableName);
        string packagesVersionPath = Path.Combine(versionPath, PackageTableName);

        string packagesJSON = JsonConvert.SerializeObject(packages.Keys);

        File.WriteAllText(packagesPath, packagesJSON);
        File.WriteAllText(packagesVersionPath, packagesJSON);

        foreach (PackageBuildInfo package in packages.Values)
        {
            packagesPath = Path.Combine(AssetBundleOutputPath, package.PackageName);
            packagesJSON = JsonConvert.SerializeObject(package);
            packagesVersionPath = Path.Combine(versionPath, package.PackageName);

            File.WriteAllText(packagesPath, packagesJSON);
            File.WriteAllText(packagesVersionPath, packagesJSON);
        }
    }

    static void CopyAssetBundleToVersionFolder(string versionPath)
    {

        //从AssetBundle输出路径下读取包列表
        string[] assetNames = ReadAssetBundleHashTable(AssetBundleOutputPath);


        //复制主包
        string mainBundleOringinPath = Path.Combine(AssetBundleOutputPath, OutputBundleName);
        string mainBundleVersionPath = Path.Combine(versionPath, OutputBundleName);
        File.Copy(mainBundleOringinPath, mainBundleVersionPath,true);


        foreach (var assetName in assetNames)
        {
            string assetHashName = assetName.Substring(assetName.IndexOf("_") + 1);

            string assetOriginPath = Path.Combine(AssetBundleOutputPath, assetHashName);

            //fileinfo.Name是包含了拓展名的文件名
            string assetVersionPath = Path.Combine(versionPath, assetHashName);
            //fileinfo.FullName是包含了目录和文件名的文件完整路径
            File.Copy(assetOriginPath, assetVersionPath, true);
        }

    }

    static BuildAssetBundleOptions CheckIncrementalBuildMode()
    {
        BuildAssetBundleOptions option = BuildAssetBundleOptions.None;

        switch (AssetManagerConfig._IncrementalBuildMode)
        {
            case IncrementalBuildMode.None:
                option = BuildAssetBundleOptions.None;
                break;

            case IncrementalBuildMode.IncrementalBuild:
                option = BuildAssetBundleOptions.DeterministicAssetBundle;
                break;

            case IncrementalBuildMode.ForceRebuild:
                option = BuildAssetBundleOptions.ForceRebuildAssetBundle;
                break;
        }
        return option;
    }

    static string[] ReadAssetBundleHashTable(string outputPath)
    {

        string VersionHashTablePath = Path.Combine(outputPath, "AssetBundleHashs");

        string VersionHashString = File.ReadAllText(VersionHashTablePath);

        string[] VersionAssetHashs = JsonConvert.DeserializeObject<string[]>(VersionHashString);

        return VersionAssetHashs;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="lastNode"></param>调用该函数的Node,本次创建的所有Node都为该Node的OutEdge
    /// <param name="allNodes"></param>当前所有的Node,可以用成员变量代替
    public static void GetNodesFromDependencies(AssetBundleNode lastNode, List<AssetBundleNode> allNodes)
    {
        //因为有向图是一层层建立依赖关系,所以不能直接获取当前资源的全部依赖
        //所以这里只获取当前资源的直接依赖
        string[] assetNames = AssetDatabase.GetDependencies(lastNode.AssetName, false);

        if (assetNames.Length == 0)
        {
            //有向图到了终点
            return;
        }

        if (lastNode.OutEdge == null)
        {
            lastNode.OutEdge = new AssetBundleEdge();
        }

        foreach (string assetName in assetNames)
        {
            if (!isValidExtentionName(assetName))
            {
                continue;
            }

            AssetBundleNode currentNode = null;
            foreach (AssetBundleNode existingNode in allNodes)
            {
                //如果当前的资源名称已经被某个Node所使用
                //那么判断为相同的资源,直接使用已存在的Node
                if (existingNode.AssetName == assetName)
                {
                    currentNode = existingNode;
                    break;
                }
            }

            if (currentNode == null)
            {
                currentNode = new AssetBundleNode();
                currentNode.AssetName = assetName;
                currentNode.InEdge = new AssetBundleEdge();
                allNodes.Add(currentNode);
            }


            currentNode.InEdge.nodes.Add(lastNode);
            lastNode.OutEdge.nodes.Add(currentNode);

            //包名以及包对资源的引用,同样也通过有向图进行传递
            if (!string.IsNullOrEmpty(lastNode.PackageName))
            {
                if (!currentNode.PackageNames.Contains(lastNode.PackageName))
                {
                    currentNode.PackageNames.Add(lastNode.PackageName);
                }
            }
            else //否则是DerivedAsset,直接获取lastNode的SourceIndices即可
            {
                foreach (string packageNames in lastNode.PackageNames)
                {
                    if (!currentNode.PackageNames.Contains(packageNames))
                    {
                        currentNode.PackageNames.Add(packageNames);
                    }
                }
            }

            //如果lastNode是SourceAsset,则直接为当前Node添加lastNode的Index
            //因为List是一个引用类型,所以SoureAsset的SourceIndices哪怕内容和Derived一样,也视为一个新的List
            if (lastNode.SourceIndex >= 0)
            {
                if (!currentNode.SourceIndices.Contains(lastNode.SourceIndex))
                {
                    currentNode.SourceIndices.Add(lastNode.SourceIndex);
                }
            }
            else //否则是DerivedAsset,直接获取lastNode的SourceIndices即可
            {
                foreach (int index in lastNode.SourceIndices)
                {
                    if (!currentNode.SourceIndices.Contains(index))
                    {
                        currentNode.SourceIndices.Add(index);
                    }
                }
            }

            GetNodesFromDependencies(currentNode, allNodes);
        }
    }



    public static List<string> GetSelectedAssetsDependencies()
    {
        List<string> dependencies = new List<string>();

        List<string> selectedAssets = new List<string>();

        for (int i = 0; i < selectedAssets.Count; i++)
        {
            //所有通过该方法获取到的数组,可以视为集合列表L中的一个元素
            string[] deps = AssetDatabase.GetDependencies(selectedAssets[i], true);

            foreach (string depName in deps)
            {
                Debug.Log(depName);
            }
        }

        return dependencies;
    }


    static BuildAssetBundleOptions CheckCompressionPattern()
    {
        BuildAssetBundleOptions option = new BuildAssetBundleOptions();
        switch (AssetManagerConfig.CompressionPattern)
        {
            case AssetBundleCompresionPattern.LZMA:
                option = BuildAssetBundleOptions.None;
                break;
            case AssetBundleCompresionPattern.LZ4:
                option = BuildAssetBundleOptions.ChunkBasedCompression;
                break;
            case AssetBundleCompresionPattern.None:
                option = BuildAssetBundleOptions.UncompressedAssetBundle;
                break;
        }
        return option;
    }


    [MenuItem(nameof(AssetManagerEditor) + "/" + nameof(OpenAssetManagerWindow))]
    static void OpenAssetManagerWindow()
    {

        AssetManagerEditorWindow window = (AssetManagerEditorWindow)EditorWindow.GetWindow(typeof(AssetManagerEditorWindow));
    }

    public static string OutputBundleName = "LocalAssets";
    static void CheckBuildOutputPath()
    {

        switch (AssetManagerConfig.BuildingPattern)
        {
            case AssetBundlePattern.EditorSimulation:
                break;
            case AssetBundlePattern.Local:
                BuildOutputPath = Path.Combine(Application.streamingAssetsPath, "BuildOutput");
                break;

            case AssetBundlePattern.Remote:
                BuildOutputPath = Path.Combine(Application.persistentDataPath, "BuildOutput");
                break;
        }

        if (!Directory.Exists(BuildOutputPath))
        {
            Directory.CreateDirectory(BuildOutputPath);
        }
        

        AssetBundleOutputPath = Path.Combine(BuildOutputPath, OutputBundleName);

        if (!Directory.Exists(AssetBundleOutputPath))
        {
            Directory.CreateDirectory(AssetBundleOutputPath);
        }

    }
    /// <summary>
    /// 因为List是引用类型,所以在方法中对于参数的修改,会反应到传入参数的变量上
    /// 因为本质上,参数只是引用了变量的指针,所以最终修改的是同一个对象的值
    /// </summary>
    /// <param name="setsA"></param>
    /// <param name="setsB"></param>
    /// <returns></returns>
    public static List<GUID> ContrastDependenciesFromGUID(List<GUID> setsA, List<GUID> setsB)
    {
        List<GUID> newDependencies = new List<GUID>();

        //取交集
        foreach (var assetGUID in setsA)
        {
            if (setsB.Contains(assetGUID))
            {
                newDependencies.Add(assetGUID);
            }
        }

        //取差集
        foreach (var assetGUID in newDependencies)
        {
            if (setsA.Contains(assetGUID))
            {
                setsA.Remove(assetGUID);
            }
            if (setsB.Contains(assetGUID))
            {
                setsB.Remove(assetGUID);
            }
        }

        //返回集合Snew
        return newDependencies;
    }

    public static void BuildAssetBundleFromSets()
    {
        CheckBuildOutputPath();
        //被选中将要被打包的资源列表,即列表A
        List<string> selectedAssets = new List<string>();

        //集合列表L
        List<List<GUID>> selectedAssetsDependencies = new List<List<GUID>>();

        //遍历所有选择的SourceAsset以及依赖,获得集合列表L
        foreach (string selectedAsset in selectedAssets)
        {
            //获取SourceAsset的DerivedAsset,其中已经包含了SourceAsset
            string[] assetDeps = AssetDatabase.GetDependencies(selectedAsset, true);

            List<GUID> assetGUIDs = new List<GUID>();

            foreach (string assetdep in assetDeps)
            {
                GUID assetGUID;
                GUID.TryParse(assetdep, out assetGUID);
                assetGUIDs.Add(assetGUID);
            }

            //将包含了SourceAsset以及DrivedAsset的集合添加到集合L中
            selectedAssetsDependencies.Add(assetGUIDs);
        }

        for (int i = 0; i < selectedAssetsDependencies.Count; i++)
        {
            int nextIndex = i + 1;
            if (nextIndex >= selectedAssetsDependencies.Count)
            {
                break;
            }

            Debug.Log($"对比之前{selectedAssetsDependencies[i].Count}");
            Debug.Log($"对比之前{selectedAssetsDependencies[nextIndex].Count}");

            for (int j = 0; j <= i; j++)
            {
                List<GUID> newDependencies = ContrastDependenciesFromGUID(selectedAssetsDependencies[j], selectedAssetsDependencies[nextIndex]);
                //将Snew集合添加到集合列表L中
                if (newDependencies != null && newDependencies.Count > 0)
                {
                    selectedAssetsDependencies.Add(newDependencies);
                }
            }
        }

        AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[selectedAssetsDependencies.Count];

        for (int i = 0; i < assetBundleBuilds.Length; i++)
        {

            assetBundleBuilds[i].assetBundleName = i.ToString();

            string[] assetNames = new string[selectedAssetsDependencies[i].Count];

            List<GUID> assetGUIDs = selectedAssetsDependencies[i];


            for (int j = 0; j < assetNames.Length; j++)
            {
                string assetName = AssetDatabase.GUIDToAssetPath(assetGUIDs[j].ToString());
                if (assetName.Contains(".cs"))
                {
                    continue;
                }
                assetNames[j] = assetName;
            }

            assetBundleBuilds[i].assetNames = assetNames;
        }
        BuildPipeline.BuildAssetBundles(AssetBundleOutputPath, assetBundleBuilds, CheckCompressionPattern(), BuildTarget.StandaloneWindows);

        AssetDatabase.Refresh();
    }

    public static void BuildAssetBundleFromEditorWindow()
    {
        CheckBuildOutputPath();


        //被选中将要被打包的资源列表
        List<string> selectedAssets = new List<string>();

        //选中了多少资源,则打包多少个ab包
        AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[selectedAssets.Count];


        //string directoryPath = AssetDatabase.GetAssetPath(AssetManagerConfig.AssetBundleDirectory);

        for (int i = 0; i < assetBundleBuilds.Length; i++)
        {
            //string bundleName = selectedAssets[i].Replace($@"{directoryPath}\", string.Empty);

            //Unity在导入.prefab文件时,会默认使用预制体导入器导入,而assetBundle不是预制体,所以会导致报错
            //bundleName = bundleName.Replace(".prefab", string.Empty);


            //assetBundleBuilds[i].assetBundleName = bundleName;

            assetBundleBuilds[i].assetNames = new string[] { selectedAssets[i] };
        }


        BuildPipeline.BuildAssetBundles(AssetBundleOutputPath, assetBundleBuilds, CheckCompressionPattern(), BuildTarget.StandaloneWindows);

        Debug.Log(AssetBundleOutputPath);

        //刷新Project界面，如果不是打包到工程内则不需要执行
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 打包指定文件夹下所有资源为AssetBundle
    /// </summary>
    public static void BuildAssetBundleFromDirectory()
    {
        CheckBuildOutputPath();


        AssetBundleBuild[] assetBundleBuilds = new AssetBundleBuild[1];

        //将要打包的具体包名，而非主包名
        assetBundleBuilds[0].assetBundleName = "Local";

        //这里虽然名为Name，实际上需要资源在工程下的路径
        //assetBundleBuilds[0].assetNames = AssetManagerConfig.CurrentAllAssets.ToArray();


        //Unity中Inspector面板配置的AssetBundle信息，其实就是一个AssetBundleBuild结构体
        //Unity只不过是遍历了所有的文件夹，并把我们在文件夹中配置的AssetBundleBuild收集起来并打包
        BuildPipeline.BuildAssetBundles(AssetBundleOutputPath, assetBundleBuilds, CheckCompressionPattern(), BuildTarget.StandaloneWindows);

        Debug.Log(AssetBundleOutputPath);

        //刷新Project界面，如果不是打包到工程内则不需要执行
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// 传入包含拓展名的文件名,用于和无效拓展名数组进行对比
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static bool isValidExtentionName(string fileName)
    {
        bool isValid = true;
        foreach (string invalidName in AssetManagerConfig.InvalidExtensionNames)
        {
            if (fileName.Contains(invalidName))
            {
                isValid = false;
                return isValid;
            }
        }
        return isValid;
    }

    public static List<string> FindAllAssetNameFromDirectory(string directoryPath)
    {
        List<string> assetPaths = new List<string>();

        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            Debug.Log("文件夹路径不存在");
            return null;
        }

        //System.IO命名空间下的类,也就是Windows自带的对文件夹进行操作的类
        //System.IO下的类，只能在Widnows或者PC平台上读写文件，在移动端不适用
        DirectoryInfo directoryInfo = new DirectoryInfo(directoryPath);

        //获取该目录下所有文件信息
        //Directory文件夹不属于File类型，所以这里不会获取子文件夹
        FileInfo[] fileInfos = directoryInfo.GetFiles();

        //所有非元数据文件路径都添加到列表中用于打包这些文件
        foreach (FileInfo info in fileInfos)
        {
            if (!isValidExtentionName(info.Extension))
            {
                continue;
            }
            //AssetBundle打包只需要文件名
            string assetPath = Path.Combine(directoryPath, info.Name);
            assetPaths.Add(assetPath);
            Debug.Log(assetPath);
        }

        return assetPaths;
    }
}
