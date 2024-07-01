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
    /// ÿ�����̷����޸�ʱ,����ø÷���
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
    /// �����������ÿ����Ⱦ֡���ã����Կ���������ȾUI����
    /// ��Ϊ�÷���������Editor���У�����ˢ��Ƶ��ȡ����Editor������֡��
    /// </summary>
    private void OnGUI()
    {
        //Ĭ��������Ǵ�ֱ�Ű�
        //GUI���մ���˳�������Ⱦ
        GUILayout.Space(20);

        if (WindowConfig.LogoTexture != null)
        {
            GUILayout.Label(WindowConfig.LogoTexture, WindowConfig.LogoTextureStyle);
        }

        #region Title��������
        GUILayout.Space(20);
        GUILayout.Label(nameof(AssetManagerEditor), WindowConfig.TitleTextStyle);


        #endregion
        GUILayout.Space(20);
        GUILayout.Label(VersionString, WindowConfig.VersionTextStyle);


        GUILayout.Space(20);
        AssetManagerEditor.AssetManagerConfig.BuildingPattern = (AssetBundlePattern)EditorGUILayout.EnumPopup("���ģʽ", AssetManagerEditor.AssetManagerConfig.BuildingPattern);


        GUILayout.Space(20);
        AssetManagerEditor.AssetManagerConfig.CompressionPattern = (AssetBundleCompresionPattern)EditorGUILayout.EnumPopup("ѹ����ʽ", AssetManagerEditor.AssetManagerConfig.CompressionPattern);

        GUILayout.Space(20);
        AssetManagerEditor.AssetManagerConfig._IncrementalBuildMode = (IncrementalBuildMode)EditorGUILayout.EnumPopup("�������", AssetManagerEditor.AssetManagerConfig._IncrementalBuildMode);

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
            if (GUILayout.Button("����Asset"))
            {
                AssetManagerEditor.AddAsset(packageInfo);
            }
            GUILayout.EndVertical();
            GUILayout.Space(20);
        }
        GUILayout.Space(10);

        if (GUILayout.Button("����Package"))
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
        if (GUILayout.Button("���AssetBundle"))
        {
            AssetManagerEditor.BuildAssetBundleFromDirectedGraph();
            Debug.Log("EditorButton����");
        }

        GUILayout.Space(20);
        if (GUILayout.Button("����Config�ļ�"))
        {
            AssetManagerEditor.SaveConfigToJson();
        }

        GUILayout.Space(20);
        if (GUILayout.Button("��ȡConfigJson�ļ�"))
        {
            AssetManagerEditor.LoadConfigFromJson();
        }
    }
}
