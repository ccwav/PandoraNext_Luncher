# PandoraNext_Luncher

自动监控本地IP，当IP更改自动下载Lic并重启PandoraNext;每天自动检测token是否过期，如果过期则自动用用户名密码获取对应的access tonken并自动更新sharetoken.

1.修改LuncherConfig.json 填入从  https://dash.pandoranext.com/  获取的Bearer秘钥和PandoraNext的API地址

2.将最新版本的PandoraNext 下载并解压到PandoraNext_Luncher.exe相同文件夹

3.修改tokens.json填入openai登录的用户名和密码

4.执行PandoraNext_Luncher.exe即可
