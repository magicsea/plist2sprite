﻿using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
//using Data;
public class TPFrameData
{
    public string name;
    public Rect frame;
    public Vector2 offset;
    //public string rotated;
    public Rect sourceColorRect;
    public Vector2 sourceSize;
    public bool rotated;

    public void LoadX(string sname,PList plist,int format)
    {
        name = sname;
        //frame = TPAtlas.StrToRect(plist["frame"] as string);
        //offset = TPAtlas.StrToVec2(plist["offset"] as string);
        //sourceColorRect = TPAtlas.StrToRect(plist["sourceColorRect"] as string);
        //sourceSize = TPAtlas.StrToVec2(plist["sourceSize"] as string);
        if (format==2)
        {
            object varCheck;
            if (plist.TryGetValue("frame", out varCheck))
            {
                frame = TPAtlas.StrToRect(plist["frame"] as string);
                offset = TPAtlas.StrToVec2(plist["offset"] as string);
                //sourceColorRect = TPAtlas.StrToRect(plist["sourceColorRect"] as string);
                sourceSize = TPAtlas.StrToVec2(plist["sourceSize"] as string);
                rotated = TPAtlas.ToBool(plist["rotated"]);
            }
            else
            {
                frame = TPAtlas.StrToRect(plist["textureRect"] as string);
                offset = TPAtlas.StrToVec2(plist["spriteOffset"] as string);
                //sourceColorRect = TPAtlas.StrToRect(plist["sourceColorRect"] as string);
                sourceSize = TPAtlas.StrToVec2(plist["spriteSourceSize"] as string);
                rotated = TPAtlas.ToBool(plist["rotated"]);
            }
        }
        else
        {
            //0
            var x = float.Parse(plist["x"].ToString());
            var y = float.Parse(plist["y"].ToString());
            var w = float.Parse(plist["width"].ToString());
            var h = float.Parse(plist["height"].ToString());
            var ow = float.Parse(plist["originalWidth"].ToString());
            var oh = float.Parse(plist["originalHeight"].ToString());
            var offx = float.Parse(plist["offsetX"].ToString());
            var offy = float.Parse(plist["offsetY"].ToString());
            frame = new Rect(x, y, w, h);
            offset = new Vector2(offx, offy);
            //sourceColorRect = new Rect(0, 0, ow, oh);
            sourceSize = new Vector2(ow, oh);
            rotated = TPAtlas.ToBool(plist["rotated"]);
        }
  
    }
}

public class TPAtlas
{
    public string realTextureFileName;
    public Vector2 size;
    public List<TPFrameData> sheets = new List<TPFrameData>();

    public void LoadX(PList plist)
    {
        //read metadata
        PList meta = plist["metadata"] as PList;
        object varCheck;
        if (meta.TryGetValue("realTextureFileName", out varCheck))
        {
            realTextureFileName = meta["realTextureFileName"] as string;
        }
        else
        {
            PList ptarget = meta["target"] as PList;
            realTextureFileName = ptarget["name"] as string;
        }

        size = StrToVec2(meta["size"] as string);
        int? format = meta["format"] as int?;
        if (format.HasValue)
        {
            Debug.Log("format:" + format.Value);
        }
        //read frames
        PList frames = plist["frames"] as PList;
        foreach (var kv in frames)
        {
            string name = kv.Key;
            PList framedata = kv.Value as PList;
            TPFrameData frame = new TPFrameData();
            frame.LoadX(name, framedata, format.Value);
            sheets.Add(frame);
        }
    }

    public static Vector2 StrToVec2(string str)
    {

        str = str.Replace("{","");
        str = str.Replace("}", "");
        string[] vs = str.Split(',');

        Vector2 v = new Vector2();
        v.x = float.Parse(vs[0]);
        v.y = float.Parse(vs[1]);
        return v;
    }
    public static Rect StrToRect(string str)
    {
        str = str.Replace("{", "");
        str = str.Replace("}", "");
        string[] vs = str.Split(',');

        Rect v = new Rect(float.Parse(vs[0]), float.Parse(vs[1]), float.Parse(vs[2]), float.Parse(vs[3]));
        return v;
    }

    public static bool ToBool(object obj)
    {
        if (obj == null) return false;
        var str = obj.ToString();
        str = str.Replace(" ", "");
        if(str=="true"||str=="True")
        {
            return true;
        }
        
        return false;
    }

}
public class SpriteSheetConvert : ScriptableObject
{
    public static string GetUTF8String(byte[] bt)
    {
        string val = System.Text.Encoding.UTF8.GetString(bt);
        return val;
    }

    [MenuItem("Assets/Plist2Sprite/PLis拆分Sprite(选择PList)")]
    static void ConvertSprite()
    {
        //EditorUtility.DisplayDialog("MyTool", "Do It in C# !", "OK", "");
        Object selobj = Selection.activeObject;
        string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!selectionPath.EndsWith(".plist")&&!selectionPath.EndsWith(".txt"))
        {
            EditorUtility.DisplayDialog("Error", "Please select a plist file!", "OK", "");
            return;
        }

        Debug.LogWarning("#PLisToSprites start:" + selectionPath);
        string fileContent = string.Empty;
        using (FileStream file = new FileStream(selectionPath, FileMode.Open))
        {
            byte[] str = new byte[(int)file.Length];
            file.Read(str, 0, str.Length);
            fileContent = GetUTF8String(str);
            Debug.Log(fileContent);
            file.Close();
            file.Dispose();
        }
        //去掉<!DOCTYPE>,不然异常
        int delStart = fileContent.IndexOf("<!DOCTYPE");
        if(delStart>=0)
        {
            int delEnd = fileContent.IndexOf("\n", delStart);
            fileContent = fileContent.Remove(delStart, delEnd - delStart);
        }

        Debug.Log(fileContent);
        //解析文件
        PList plist = new PList();
        plist.LoadText(fileContent);//Load(selectionPath);
        TPAtlas at = new TPAtlas();
        at.LoadX(plist);

        //重写meta
        string texPath = Path.GetDirectoryName(selectionPath) + "/" + at.realTextureFileName;
        Texture2D selTex = AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D)) as Texture2D;
        Debug.Log("texture:" + texPath);
        Debug.Log("write texture:" + selTex.name+ "  size:"+selTex.texelSize);
        TextureImporter textureImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (textureImporter.textureType != TextureImporterType.Sprite && textureImporter.textureType != TextureImporterType.Default)
        {
            EditorUtility.DisplayDialog("Error", "Texture'type must be sprite or Advanced!", "OK", "");
            return;
        }
        if (textureImporter.spriteImportMode!=SpriteImportMode.Multiple)
        {
            EditorUtility.DisplayDialog("Error", "spriteImportMode must be Multiple!", "OK", "");
            return;
        }
        SpriteMetaData[] sheetMetas = new SpriteMetaData[at.sheets.Count];
        for (int i = 0; i < at.sheets.Count; i++)
        {
            var frameData = at.sheets[i];
            sheetMetas[i].alignment = 0;
            sheetMetas[i].border = new Vector4(0, 0, 0, 0);
            sheetMetas[i].name = frameData.name;
            sheetMetas[i].pivot = new Vector2(0.5f, 0.5f);
            if(frameData.rotated)
            {
                var w = frameData.frame.height;
                var h = frameData.frame.width;
                sheetMetas[i].rect = new Rect(frameData.frame.x, at.size.y - frameData.frame.y - h,w, h);//这里原点在左下角，y相反,h,w相反
            }
            else
            {
                sheetMetas[i].rect = new Rect(frameData.frame.x, at.size.y - frameData.frame.y - frameData.frame.height,
    frameData.frame.width, frameData.frame.height);//这里原点在左下角，y相反
            }

            //Debug.Log("do sprite:" + frameData.name);
        }
        //textureImporter.spriteImportMode = SpriteImportMode.Multiple;
        textureImporter.spritesheet = sheetMetas;

        //save
        textureImporter.textureType = TextureImporterType.Sprite;       //bug?
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;   //不加这两句会导致无法保存meta
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

        Debug.LogWarning("#PLisToSprites end:" + texPath);
    }

    [MenuItem("Assets/Plist2Sprite/Sprite导出散图(选择图片)")]
    static void OutputSprite()
    {
        string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        Texture2D selTex = Selection.activeObject as Texture2D;
        string rootPath = Path.GetDirectoryName(selectionPath) +"/"+ Selection.activeObject.name;
        TextureImporter textureImporter = AssetImporter.GetAtPath(selectionPath) as TextureImporter;
        //textureImporter.textureType = TextureImporterType.Advanced;
        //textureImporter.isReadable = true;
        Object[] selected = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);
        if (!Directory.Exists(rootPath))
            Directory.CreateDirectory(rootPath);
        Debug.Log("output dir :" + rootPath);
        //foreach (var item in selected)
        //{
        //    Sprite sprite = item as Sprite;
        //    if (sprite == null)
        //        return;
        //    string path = rootPath + "/" + sprite.name + ".png";
        //    Debug.Log("output :" + path);
        //    SaveSpriteToPNG(sprite, path);
        //}

        int i = 0;
        //EditorUtility.DisplayProgressBar("unpack sprites", "start", 0);
        foreach (var spmeta in textureImporter.spritesheet)
        {
            i++;
            //if (i > 10)
            //{
            //    break;
            //}
            EditorUtility.DisplayProgressBar("unpack sprites", spmeta.name, (float)i/ (float)textureImporter.spritesheet.Length);
            string path = rootPath + "/" + spmeta.name;
            string selectionExt = System.IO.Path.GetExtension(spmeta.name);
            if (selectionExt!=".png"&&selectionExt != ".PNG")
            {
                path+= ".png";
            }
            string subDir = Path.GetDirectoryName(path);
            if (!Directory.Exists(subDir))
            {
                Directory.CreateDirectory(subDir);
            }
            Debug.Log("output :" + path);
            SavePriteToPNG_Meta(selTex, spmeta, path);
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/Plist2Sprite/Sprite导出旋转散图(选择plist)")]
    static void OutputSpriteWithPlist()
    {

        //getdic
        Object selobj = Selection.activeObject;
        string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!selectionPath.EndsWith(".plist") && !selectionPath.EndsWith(".txt"))
        {
            EditorUtility.DisplayDialog("Error", "Please select a plist file!", "OK", "");
            return;
        }

        Debug.LogWarning("#PLisToSprites start:" + selectionPath);
        string fileContent = string.Empty;
        using (FileStream file = new FileStream(selectionPath, FileMode.Open))
        {
            byte[] str = new byte[(int)file.Length];
            file.Read(str, 0, str.Length);
            fileContent = GetUTF8String(str);
            Debug.Log(fileContent);
            file.Close();
            file.Dispose();
        }
        //去掉<!DOCTYPE>,不然异常
        int delStart = fileContent.IndexOf("<!DOCTYPE");
        if (delStart >= 0)
        {
            int delEnd = fileContent.IndexOf("\n", delStart);
            fileContent = fileContent.Remove(delStart, delEnd - delStart);
        }

        Debug.Log(fileContent);
        //解析文件
        PList plist = new PList();
        plist.LoadText(fileContent);//Load(selectionPath);
        TPAtlas at = new TPAtlas();
        at.LoadX(plist);
        Dictionary<string, bool> rotatoDic = new Dictionary<string, bool>();
        foreach (var item in at.sheets)
        {
            if (item.rotated)
            {
                rotatoDic[item.name] = true;
            }
        }

        //output
        string texPath = Path.GetDirectoryName(selectionPath) + "/" + at.realTextureFileName;

        Texture2D selTex =   AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        string rootPath = Path.GetDirectoryName(selectionPath) + "/" + Selection.activeObject.name;
        TextureImporter textureImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;

        int i = 0;
        //EditorUtility.DisplayProgressBar("unpack sprites", "start", 0);
        foreach (var spmeta in textureImporter.spritesheet)
        {
            i++;
            //if (i > 10)
            //{
            //    break;
            //}
            bool rotate = false;
            if (rotatoDic.ContainsKey(spmeta.name))
            {
                rotate = true;
                Debug.LogWarning("conv ret:" + spmeta.name);
            }
            EditorUtility.DisplayProgressBar("unpack sprites", spmeta.name, (float)i / (float)textureImporter.spritesheet.Length);
            string path = rootPath + "/" + spmeta.name;
            string selectionExt = System.IO.Path.GetExtension(spmeta.name);
            if (selectionExt != ".png" && selectionExt != ".PNG")
            {
                path += ".png";
            }
            string subDir = Path.GetDirectoryName(path);
            if (!Directory.Exists(subDir))
            {
                Directory.CreateDirectory(subDir);
            }
            Debug.Log("output :" + path);
            SavePriteToPNG_Meta(selTex, spmeta, path, rotate);
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }
    [MenuItem("Tools/UnPack Sprites")]
    static void SaveSprite()
    {
        string resourcesPath = "Assets/Resources/";
        foreach (Object obj in Selection.objects)
        {
            string selectionPath = AssetDatabase.GetAssetPath(obj);
            // 必须最上级是"Assets/Resources/"
            if (selectionPath.StartsWith(resourcesPath))
            {
                string selectionExt = System.IO.Path.GetExtension(selectionPath);
                if (selectionExt.Length == 0)
                {
                    continue;
                }
                // 从路径"Assets/Resources/UI/testUI.png"得到路径"UI/testUI"
                string loadPath = selectionPath.Remove(selectionPath.Length - selectionExt.Length);
                loadPath = loadPath.Substring(resourcesPath.Length);
                // 加载此文件下的所有资源
                Sprite[] sprites = Resources.LoadAll<Sprite>(loadPath);
                if (sprites.Length > 0)
                {
                // 创建导出文件夹
                    string outPath = Application.dataPath + "/outSprite/" + loadPath;
                    System.IO.Directory.CreateDirectory(outPath);
                    int i = 0;
                    foreach (Sprite sprite in sprites)
                    {
                        i++;
                        //if (i > 10)
                        //{
                        //    break;
                        //}
                        // 创建单独的纹理
                        Texture2D tex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, sprite.texture.format, false);
                    tex.SetPixels(sprite.texture.GetPixels((int)sprite.rect.xMin, (int)sprite.rect.yMin,
                    (int)sprite.rect.width, (int)sprite.rect.height));
                    tex.Apply();
                    // 写入成PNG文件
                    System.IO.File.WriteAllBytes(outPath + "/" + sprite.name + ".png", tex.EncodeToPNG()); 
                    }
                    Debug.Log("SaveSprite to " + outPath);
                }
            }
            else
            {
                Debug.LogWarning("图片必须在Assets/Resources/目录下");
            }
        } 
   
        Debug.Log("SaveSprite Finished");
    }

    static bool SaveSpriteToPNG(Sprite sprite, string outPath)
    {
        // 创建单独的纹理
        Texture2D tex = new Texture2D((int)sprite.rect.width, (int)sprite.rect.height, sprite.texture.format, false);
        tex.SetPixels(sprite.texture.GetPixels((int)sprite.rect.xMin, (int)sprite.rect.yMin,
        (int)sprite.rect.width, (int)sprite.rect.height));
        tex.Apply();
        // 写入成PNG文件
        File.WriteAllBytes(outPath, tex.EncodeToPNG());
        return true;
    }

    static bool SavePriteToPNG_Meta(Texture2D sourceImg,SpriteMetaData metaData, string outPath,bool rotate=true)
    {

        Texture2D myimage = new Texture2D((int)metaData.rect.width, (int)metaData.rect.height, sourceImg.format,false);
        Color[] pixs = sourceImg.GetPixels((int)metaData.rect.x, (int)metaData.rect.y,(int)metaData.rect.width, (int)metaData.rect.height);
        if(rotate)
        {
            //翻转w,h
            myimage = new Texture2D( (int)metaData.rect.height, (int)metaData.rect.width, sourceImg.format, false);
            Color[] basePixs = sourceImg.GetPixels((int)metaData.rect.x, (int)metaData.rect.y,(int)metaData.rect.width, (int)metaData.rect.height);
            for (int x = 0; x < (int)metaData.rect.width; x++)
            {
                for (int y = 0; y < (int)metaData.rect.height; y++)
                {
                    int newx = (int)metaData.rect.height-1 - y;
                    int newy = x;
                    var p = basePixs[y * (int)metaData.rect.width + x];
                    pixs[newy * (int)metaData.rect.height + newx] = p;
                }
            }
        }

        myimage.SetPixels(pixs);
        myimage.Apply();
        //abc_0:(x:2.00, y:400.00, width:103.00, height:112.00)
        //for (int y = (int)metaData.rect.y; y < metaData.rect.y + metaData.rect.height; y++)//Y轴像素
        //{
        //    for (int x = (int)metaData.rect.x; x < metaData.rect.x + metaData.rect.width; x++)
        //        myimage.SetPixel(x - (int)metaData.rect.x, y - (int)metaData.rect.y, sourceImg.GetPixel(x, y));
        //}


        //转换纹理到EncodeToPNG兼容格式
        //if (myimage.format != TextureFormat.ARGB32 && myimage.format != TextureFormat.RGB24)
        //{
        //    Texture2D newTexture = new Texture2D(myimage.width, myimage.height);
        //    newTexture.SetPixels(myimage.GetPixels(0), 0);
        //    myimage = newTexture;
        //}

        //AssetDatabase.CreateAsset(myimage, outPath);
        try
        {
            var png = myimage.EncodeToPNG();
            File.WriteAllBytes(outPath, png);
        }
        catch (System.Exception e)
        {
            Debug.LogError("write file error:" + outPath);
            throw;
        }

        return true;
    }
}