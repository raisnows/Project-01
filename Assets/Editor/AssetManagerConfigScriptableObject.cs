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
    /// �༭��ģ����,�����д��
    /// ����ģʽ,�����StreamingAssets
    /// Զ��ģʽ,���������Զ��·��,�ڸ�ʾ����ΪpersistentDataPath
    /// </summary>
    public AssetBundlePattern BuildingPattern;

    /// <summary>
    /// �Ƿ�Ӧ���������
    /// </summary>
    public IncrementalBuildMode _IncrementalBuildMode;

    /// <summary>
    /// AssetBundleѹ����ʽ
    /// </summary>
    public AssetBundleCompresionPattern CompressionPattern;

    /// <summary>
    /// ��Դ���������ߵİ汾
    /// </summary>
    public int AssetManagerVersion = 100;

    /// <summary>
    /// ��Դ����İ汾
    /// </summary>
    public int CurrentBuildVersion = 100;



    public List<PackageEditorInfo> packageEditorInfos=new List<PackageEditorInfo>();

    /// <summary>
    /// ��Ҫ�ų���Asset��չ��
    /// </summary>
    public string[] InvalidExtensionNames = new string[] { ".meta", ".cs" };

}
