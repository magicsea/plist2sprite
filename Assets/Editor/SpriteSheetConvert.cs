using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
#if UNITY_2020_1_OR_NEWER
using UnityEditor.U2D;
using UnityEditor.U2D.Sprites;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    [MenuItem("Assets/Plist2Sprite/egret-json拆分动画(选择json)", validate = true)]
    static bool ValidateConvertEgretJsonToAnim()
    {
        return Selection.activeObject != null && 
            AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".json");
    }

    [MenuItem("Assets/Plist2Sprite/egret-json拆分动画(选择json)")]
    static void ConvertEgretJsonToAnim()
    {
        Object selobj = Selection.activeObject;
        string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!selectionPath.EndsWith(".json"))
        {
            EditorUtility.DisplayDialog("Error", "请选择动画json文件!", "OK", "");
            return;
        }

        Debug.LogWarning("#ConvertEgretJsonToAnim start:" + selectionPath);
        string fileContent = string.Empty;
        using (FileStream file = new FileStream(selectionPath, FileMode.Open))
        {
            byte[] str = new byte[(int)file.Length];
            file.Read(str, 0, str.Length);
            fileContent = GetUTF8String(str);
            file.Close();
            file.Dispose();
        }

        JObject jsonObj = JObject.Parse(fileContent);

        // 解析mc部分
        List<FrameAnimInfo> anims = new List<FrameAnimInfo>();
        JObject mcData = jsonObj["mc"] as JObject;

        // 提前加载所有Sprite
        if (mcData != null)
        {
            foreach (var animClip in mcData)
            {
                FrameAnimInfo animInfo = new FrameAnimInfo();
                string clipName = animClip.Key;
                JToken clipData = animClip.Value;
                animInfo.AnimName = clipName;
                animInfo.FrameRate = clipData["frameRate"]?.ToObject<int>() ?? 24;
                animInfo.Loop = true; // 默认循环播放
                
                JArray frames = clipData["frames"] as JArray;
                if (frames != null)
                {
                    int i = 0;
                    animInfo.Frames = new FrameData[frames.Count];
                    foreach (JToken frame in frames)
                    {
                        string resName = frame["res"].ToString();
                        Vector2 offset = new Vector2(
                            frame["x"]?.ToObject<float>() ?? 0,
                            frame["y"]?.ToObject<float>() ?? 0
                        );
                        var fd = new FrameData() {
                            res = resName,
                            x = offset.x,
                            y = offset.y,
                        }; 
                        animInfo.Frames[i] = fd;
                        i++;
                    }
                }
                anims.Add(animInfo);
            }
        }


        //解析res部分
        JObject resData = jsonObj["res"] as JObject;
        if (resData == null)
        {
            EditorUtility.DisplayDialog("Error", "JSON文件中缺少res部分!", "OK", "");
            return;
        }

        TPAtlas at = new TPAtlas();
        at.realTextureFileName = Path.GetFileNameWithoutExtension(selectionPath) + ".png";
        
        // 读取图片并获取宽高
        string texPath = Path.GetDirectoryName(selectionPath) + "/" + at.realTextureFileName;
        Texture2D selTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (selTex != null)
        {
            at.size = new Vector2(selTex.width, selTex.height);
        }
        else
        {
            Debug.LogError("无法加载纹理: " + texPath);
            return;
        }

        // 解析res部分
        foreach (var frame in resData)
        {
            TPFrameData frameData = new TPFrameData();
            frameData.name = frame.Key;
            JToken frameInfo = frame.Value;
            
            frameData.frame = new Rect(
                float.Parse(frameInfo["x"].ToString()),
                float.Parse(frameInfo["y"].ToString()),
                float.Parse(frameInfo["w"].ToString()),
                float.Parse(frameInfo["h"].ToString())
            );
            
            frameData.offset = Vector2.zero; // 动画json通常没有offset
            frameData.sourceSize = new Vector2(
                float.Parse(frameInfo["w"].ToString()),
                float.Parse(frameInfo["h"].ToString())
            );
            
            at.sheets.Add(frameData);
        }

        // 设置纹理导入器参数
        TextureImporter textureImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
        SpriteMetaData[] sheetMetas = new SpriteMetaData[at.sheets.Count];
        for (int i = 0; i < at.sheets.Count; i++)
        {
            var frameData = at.sheets[i];
            //sheetMetas[i].alignment = 0;
            sheetMetas[i].border = new Vector4(0, 0, 0, 0);
            sheetMetas[i].name = frameData.name;
            //sheetMetas[i].pivot = new Vector2(0.5f, 0.5f);
            sheetMetas[i].rect = new Rect(
                frameData.frame.x, 
                at.size.y - frameData.frame.y - frameData.frame.height,
                frameData.frame.width, 
                frameData.frame.height
            );
            
            // 查找帧信息
            FrameData frameInfo = null;
            foreach(var anim in anims)
            {
                frameInfo = anim.Frames.FirstOrDefault(f => f.res == frameData.name);
                if(frameInfo != null) break;
            }
            
            // 计算pivot
            Vector2 pivot = Vector2.zero;
            if(frameInfo != null && frameData.sourceSize.x > 0 && frameData.sourceSize.y > 0)
            {
                // 计算pivot时考虑负偏移
                float pivotX = - (frameInfo.x / frameData.sourceSize.x);
                float pivotY = 1 + (frameInfo.y / frameData.sourceSize.y);
                pivot = new Vector2(pivotX, pivotY);
            }
            else
            {
                pivot = new Vector2(0.5f, 0.5f); // 默认居中
            }
            
            sheetMetas[i].pivot = pivot;
            sheetMetas[i].alignment = (int)SpriteAlignment.Custom; // 设置为Custom类型
        }

        // 使用ISpriteEditorDataProvider设置精灵数据
        var factory = new SpriteDataProviderFactories();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(textureImporter);
        dataProvider.InitSpriteEditorDataProvider();

        textureImporter.isReadable = true;
        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;

        var spriteRects = new List<SpriteRect>();
        for (int i = 0; i < sheetMetas.Length; i++)
        {
            var meta = sheetMetas[i];
            var spriteRect = new SpriteRect()
            {
                name = meta.name,
                rect = meta.rect,
                alignment = (SpriteAlignment)meta.alignment,
                border = meta.border,
                pivot = meta.pivot
            };
            spriteRects.Add(spriteRect);
        }

        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();

        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        Debug.LogWarning("#ConvertEgretJsonToAnim output sprites end:" + selectionPath);
        // 关联sprite
        // 等待一帧确保Sprite数据已写入
        EditorApplication.delayCall += () => {
            // 提前加载所有Sprite
            Sprite[] allSprites = AssetDatabase.LoadAllAssetsAtPath(texPath).OfType<Sprite>().ToArray();
            
            if (mcData != null)
            {
                foreach (var animClip in anims)
                {
                    foreach (var frame in animClip.Frames)
                    {
                        // 从已加载的Sprite数组中查找
                        Sprite targetSprite = allSprites.FirstOrDefault(s => s.name == frame.res);
                        if (targetSprite == null)
                        {
                            Debug.LogError("无法加载Sprite: " + frame.res);
                        }
                        frame.sprite = targetSprite;  // 关联对应的Sprite
                    }
                }
            }

            // 创建预设
            string prefabPath = Path.GetDirectoryName(selectionPath) + "/" + Path.GetFileNameWithoutExtension(selectionPath) + ".prefab";
            
            // 删除已存在的预设
            if (File.Exists(prefabPath))
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }
            
            GameObject prefab = new GameObject(Path.GetFileNameWithoutExtension(selectionPath));
            FrameAnimData animData = prefab.AddComponent<FrameAnimData>();

            animData.AnimRes = new FrameAnimRes(){Infos = anims.ToList()} ;
            
            // 保存预设
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            GameObject.DestroyImmediate(prefab);

            Debug.LogWarning("#ConvertEgretJsonToAnim end:" + texPath);
        };
    }


    [MenuItem("Assets/Plist2Sprite/egret-json拆分UI(选择json)", validate = true)]
    static bool ValidateConvertEgretJsonToUI()
    {
        return Selection.activeObject != null && 
               AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".json");
    }
    

    [MenuItem("Assets/Plist2Sprite/egret-json拆分UI(选择json)")]
    static void ConvertEgretJsonToUI()
    {
        //EditorUtility.DisplayDialog("MyTool", "Do It in C# !", "OK", "");
        Object selobj = Selection.activeObject;
        string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!selectionPath.EndsWith(".json"))
        {
            EditorUtility.DisplayDialog("Error", "Please select a json file!", "OK", "");
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
        //读取json文件，解析内容，结构：
        /*
        {
            "file":"createRole.png",
            "frames":{
                        "login_btn":{"x":300,"y":1,"w":297,"h":129,"offX":0,"offY":0,"sourceW":297,"sourceH":129}
                    }
            }
        */
        JObject jsonObj = JObject.Parse(fileContent);
        TPAtlas at = new TPAtlas();
        at.realTextureFileName = jsonObj["file"].ToString();
        // 读取图片并获取宽高
        string texPath = Path.GetDirectoryName(selectionPath) + "/" + at.realTextureFileName;
        Texture2D selTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (selTex != null)
        {
            at.size = new Vector2(selTex.width, selTex.height);
        }
        else
        {
            Debug.LogError("无法加载纹理: " + texPath);
            return;
        }

        // 解析frames
        JObject frames = (JObject)jsonObj["frames"];
        foreach (var frame in frames)
        {
                TPFrameData frameData = new TPFrameData();
                frameData.name = frame.Key;
                JToken frameInfo = frame.Value;
                
                frameData.frame = new Rect(
                    float.Parse(frameInfo["x"].ToString()),
                    float.Parse(frameInfo["y"].ToString()),
                    float.Parse(frameInfo["w"].ToString()),
                    float.Parse(frameInfo["h"].ToString())
                );
                
                frameData.offset = new Vector2(
                    float.Parse(frameInfo["offX"].ToString()),
                    float.Parse(frameInfo["offY"].ToString())
                );
                
                frameData.sourceSize = new Vector2(
                    float.Parse(frameInfo["sourceW"].ToString()),
                    float.Parse(frameInfo["sourceH"].ToString())
                );
                
                at.sheets.Add(frameData);
        }   
      
        
        //重写meta
        //string texPath = Path.GetDirectoryName(selectionPath) + "/" + at.realTextureFileName;
        //Texture2D selTex = AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D)) as Texture2D;
        Debug.Log("texture:" + texPath);
        Debug.Log("write texture:" + selTex.name+ "  size:"+selTex.texelSize);
        
        TextureImporter textureImporter = AssetImporter.GetAtPath(texPath) as TextureImporter;
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
                sheetMetas[i].rect = new Rect(frameData.frame.x, at.size.y - frameData.frame.y - h,w, h);
            }
            else
            {
                sheetMetas[i].rect = new Rect(frameData.frame.x, at.size.y - frameData.frame.y - frameData.frame.height,
                    frameData.frame.width, frameData.frame.height);
            }
        }

        // 使用ISpriteEditorDataProvider设置精灵数据
        #if UNITY_2020_1_OR_NEWER
        var factory = new SpriteDataProviderFactories();
        var dataProvider = factory.GetSpriteEditorDataProviderFromObject(textureImporter);
        dataProvider.InitSpriteEditorDataProvider();
    
        // 设置纹理导入器参数
        textureImporter.isReadable = true;
        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;
    
        // 将SpriteMetaData转换为SpriteRect
        var spriteRects = new List<SpriteRect>();
        for (int i = 0; i < sheetMetas.Length; i++)
        {
            var meta = sheetMetas[i];
            var spriteRect = new SpriteRect()
            {
                name = meta.name,
                rect = meta.rect,
                alignment = (SpriteAlignment)meta.alignment,
                border = meta.border,
                pivot = meta.pivot
            };
            spriteRects.Add(spriteRect);
        }
    
        // 设置精灵数据
        dataProvider.SetSpriteRects(spriteRects.ToArray());
        dataProvider.Apply();
    
        // 应用修改
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        #else
        // 旧版本Unity的回退方案
        textureImporter.isReadable = true;
        textureImporter.textureType = TextureImporterType.Sprite;
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;
        textureImporter.spritesheet = sheetMetas;
        EditorUtility.SetDirty(textureImporter);
        AssetDatabase.WriteImportSettingsIfDirty(texPath);
        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        #endif

        Debug.LogWarning("#ConvertEgretJsonToUI end:" + texPath);
    }


    [MenuItem("Assets/Plist2Sprite/PLis拆分Sprite(选择PList)", validate = true)]
    static bool ValidateConvertSprite()
    {
        return Selection.activeObject != null &&
               (AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".plist") || AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith(".txt"));
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
                sheetMetas[i].rect = new Rect(frameData.frame.x, at.size.y - frameData.frame.y - frameData.frame.height,//原点左上
    frameData.frame.width, frameData.frame.height);//这里原点在左下角，y相反
            }

            //Debug.Log("do sprite:" + frameData.name);
        }
        //textureImporter.spriteImportMode = SpriteImportMode.Multiple;
               textureImporter.spritesheet = sheetMetas;

        //save
        textureImporter.textureType = TextureImporterType.Sprite;       //bug?
        textureImporter.spriteImportMode = SpriteImportMode.Multiple;   //不加这两句会导致无法保存meta

        EditorUtility.SetDirty(textureImporter);
        AssetDatabase.WriteImportSettingsIfDirty(texPath);
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
            Debug.Log("write file error:" + outPath+e);
            throw;
        }

        return true;
    }
}
