using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


[CreateAssetMenu(fileName = "AssetManagerConfig",menuName = "AssetManager/CreateManagerConfig")]
public class AssetManagerConfigScriptableObject : ScriptableObject
{
    /// <summary>
    /// 编辑器模拟下,不进行打包
    /// 本地模式,打包到StreamingAssets
    /// 远端模式,打包到任意远端路径,在该示例中为persistentDataPath
    /// </summary>
    public AssetBundlePattern BuildingPattern;

    /// <summary>
    /// 是否应用增量打包
    /// </summary>
    public IncrementalBuildMode _IncrementalBuildMode;

    /// <summary>
    /// AssetBundle压缩格式
    /// </summary>
    public AssetBundleCompresionPattern CompressionPattern;

    /// <summary>
    /// 资源管理器工具的版本
    /// </summary>
    public int AssetManagerVersion = 100;

    /// <summary>
    /// 资源打包的版本
    /// </summary>
    public int CurrentBuildVersion = 100;



    public List<PackageEditorInfo> packageEditorInfos=new List<PackageEditorInfo>();

    /// <summary>
    /// 需要排除的Asset拓展名
    /// </summary>
    public string[] InvalidExtensionNames = new string[] { ".meta", ".cs" };

}
