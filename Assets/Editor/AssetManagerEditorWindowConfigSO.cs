using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "AssetManagerEditorWindowConfig", menuName = "AssetManager/CreateWindowConfig")]
public class AssetManagerEditorWindowConfigSO : ScriptableObject
{
    public  GUIStyle TitleTextStyle;

    public  GUIStyle VersionTextStyle;

    public  Texture2D LogoTexture;
    public  GUIStyle LogoTextureStyle;

}
