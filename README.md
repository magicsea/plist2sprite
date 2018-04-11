plist文件转unity sprite工具，也可以将sprite拆分png散图。

### 注意事项
- plist必须是utf8格式
- 图片放到unity后import settings需要修改
```
1.spriteMode:Multiple
2.Read/write Enable:true 
3.Compression:None
```

### 操作步骤
- 1.plist转sprte:选择plist文件，然后右键菜单Plist2Sprite/PLis拆分Sprite(选择PList)

- 2.导出散图:完成上个步骤后，选择图片文件,然后右键菜单Plist2Sprite/Sprite导出散图(选择图片)

- 3.导出散图（带旋转）:完成上个步骤1后,选择plist文件，然后右键菜单Plist2Sprite/Sprite导出旋转散图(选择plist)