### 流程
1. 切换到对应平台
```
  微信小游戏需要在Player中添加宏：WEIXINMINIGAME
  抖音小游戏需要在Player中添加宏：DOUYINMINIGAME
  注意：抖音小游戏需要移除微信导出插件，否则运行报错卡死；微信小游戏不要移除抖音导出插件，运行有报错但不影响；
```
2. 安装HybridCLR
3. 执行Tools/HybridCLRGenerate
4. 执行Tools/YooAssetBuild
5. 上传资源到CDN几种方式：
```
  使用Build/hfs.exe本地测试
  Tools/UploadBundlesCDN，上传到腾讯储存桶，需要配置Preferences/Customer腾讯储存桶
  手动上传到其他
  目录结构
    WebGL
      VersionConfig.txt(Build目录中)
      1.2.0
        .bundle
    Android
      VersionConfig.txt(Build目录中)
      1.2.0
        .bundle
```
6. 导出工程：
```
  修改Assets\Scripts\MainScripts\GameSetting.cs中的CDN
  配置Preferences/Customer BuildPlayer路径
  执行Tools/BuildProject
 ```
### 其他
1. 配置导出：Tools/CopyConfig，需要配置Preferences/Customer Luban路径
2. Proto消息导出：Tools/CopyMsg，需要配置Preferences/Customer Msg路径
3. Jenkins部署：配置文件Build/JenkinsConfig/Game.xml
