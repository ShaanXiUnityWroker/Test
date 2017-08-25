using System.IO;
using UnityEditor;
using UnityEngine;

public class UploadToServer
{

    [MenuItem("Assets/Upload All AssetBundles")]
    private static void UploadToTarget()
    {
        string remoteFolder = "/mnt/hgfs/Ubuntu_Share/Build";//远程服务器资源所在路径

        string localPath = Directory.GetParent(Application.dataPath).ToString();

        string localFolder = localPath + "/Build";//本地资源目录路径


        string winScpPath = "D:/WinSCP/WinSCP.exe";
        //脚本路径
        string scriptPath = localPath + "/UploadAssetBundles.script";
        //Log路径
        string logPath = localPath + "/upload.log";

        string username = "wangwei";//远程服务器登录用户名
        string password = "123456";//远程服务器登录密码
        string ipAddress = "192.168.190.15";//远程服务器IP或域名

        string[] param ={
                username,
                password,
                ipAddress,
                localFolder,
                remoteFolder,
                scriptPath,
                logPath,
            };

        string arguments = System.String.Format
            ("/console /log={6} /script={5} /parameter \"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\"", param);

        UploadHelper.CallUploadProcess(arguments, winScpPath);

    }
}
