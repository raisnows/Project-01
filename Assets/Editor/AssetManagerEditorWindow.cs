using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetManagerEditorWindow : EditorWindow
{
    public string VersionString;

    public AssetManagerEditorWindowConfigSO WindowConfig;
    public void Awake()
    {
        AssetManagerEditor.LoadConfig(this);
        AssetManagerEditor.LoadWindowConfig(this);
    }

    /// <summary>
    /// 每当工程发生修改时,会调用该方法
    /// </summary>
    private void OnValidate()
    {
        AssetManagerEditor.LoadConfig(this);
        AssetManagerEditor.LoadWindowConfig(this);
    }

    private void OnInspectorUpdate()
    {
        AssetManagerEditor.LoadConfig(this);
        AssetManagerEditor.LoadWindowConfig(this);
    }

    private void OnEnable()
    {
        
    }

    /// <summary>
    /// 这个方法会在每个渲染帧调用，所以可以用来渲染UI界面
    /// 因为该方法运行在Editor类中，所以刷新频率取决于Editor的运行帧率
    /// </summary>
    private void OnGUI()
    {
        //默认情况下是垂直排版
        //GUI按照代码顺序进行渲染
        GUILayout.Space(20);

        if (WindowConfig.LogoTexture != null)
        {
            GUILayout.Label(WindowConfig.LogoTexture, WindowConfig.LogoTextureStyle);
        }

        #region Title文字内容
        GUILayout.Space(20);
        GUILayout.Label(nameof(AssetManagerEditor), WindowConfig.TitleTextStyle);


        #endregion
        GUILayout.Space(20);
        GUILayout.Label(VersionString, WindowConfig.VersionTextStyle);


        GUILayout.Space(20);
        AssetManagerEditor.AssetManagerConfig.BuildingPattern = (AssetBundlePattern)EditorGUILayout.EnumPopup("打包模式", AssetManagerEditor.AssetManagerConfig.BuildingPattern);


        GUILayout.Space(20);
        AssetManagerEditor.AssetManagerConfig.CompressionPattern = (AssetBundleCompresionPattern)EditorGUILayout.EnumPopup("压缩格式", AssetManagerEditor.AssetManagerConfig.CompressionPattern);

        GUILayout.Space(20);
        AssetManagerEditor.AssetManagerConfig._IncrementalBuildMode = (IncrementalBuildMode)EditorGUILayout.EnumPopup("增量打包", AssetManagerEditor.AssetManagerConfig._IncrementalBuildMode);

        GUILayout.Space(20);


        GUILayout.BeginVertical("frameBox");
        GUILayout.Space(10);
        for(int i=0; i< AssetManagerEditor.AssetManagerConfig.packageEditorInfos.Count; i++)
        {
            PackageEditorInfo packageInfo = AssetManagerEditor.AssetManagerConfig.packageEditorInfos[i];
            GUILayout.BeginVertical("frameBox");

            GUILayout.BeginHorizontal();
            packageInfo.PackageName = EditorGUILayout.TextField("PackageName", packageInfo.PackageName);

            if (GUILayout.Button("Remove"))
            {
                AssetManagerEditor.RemovePackageInfoEditor(packageInfo);
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            for (int j = 0; j < packageInfo.AssetList.Count; j++)
            {
                GUILayout.BeginHorizontal();
                packageInfo.AssetList[j] = EditorGUILayout.ObjectField(packageInfo.AssetList[j], typeof(GameObject)) as GameObject;

                if (GUILayout.Button("Remove"))
                {
                    AssetManagerEditor.RemoveAsset(packageInfo, packageInfo.AssetList[j]);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }
            GUILayout.Space(5);
            if (GUILayout.Button("新增Asset"))
            {
                AssetManagerEditor.AddAsset(packageInfo);
            }
            GUILayout.EndVertical();
            GUILayout.Space(20);
        }
        GUILayout.Space(10);

        if (GUILayout.Button("新增Package"))
        {
            AssetManagerEditor.AddPacakgeInfoEditor();
        }

        GUILayout.EndVertical();

        //if(AssetManagerEditor.AssetManagerConfig.CurrentAllAssets !=null && AssetManagerEditor.AssetManagerConfig.CurrentAllAssets.Count > 0)
        //{
        //    for(int i = 0; i < AssetManagerEditor.AssetManagerConfig.CurrentAllAssets.Count; i++)
        //    {
        //        AssetManagerEditor.AssetManagerConfig.CurrentSelectedAssets[i] = EditorGUILayout.ToggleLeft(AssetManagerEditor.AssetManagerConfig.CurrentAllAssets[i], AssetManagerEditor.AssetManagerConfig.CurrentSelectedAssets[i]);
        //    }
        //}

        GUILayout.Space(20);
        if (GUILayout.Button("打包AssetBundle"))
        {
            AssetManagerEditor.BuildAssetBundleFromDirectedGraph();
            Debug.Log("EditorButton按下");
        }

        GUILayout.Space(20);
        if (GUILayout.Button("保存Config文件"))
        {
            AssetManagerEditor.SaveConfigToJson();
        }

        GUILayout.Space(20);
        if (GUILayout.Button("读取ConfigJson文件"))
        {
            AssetManagerEditor.LoadConfigFromJson();
        }
    }
}
