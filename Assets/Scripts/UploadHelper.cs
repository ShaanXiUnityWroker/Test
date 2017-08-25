using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class UploadHelper
{

    public static void CallUploadProcess(string arguments , string winScpPath)
    {

        try
        {
            Process proc = null;

            bool redirectOutput = false;

            proc = new Process();
            proc.StartInfo.FileName = winScpPath;
            proc.StartInfo.Arguments = arguments;

            if (redirectOutput)
            {
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.CreateNoWindow = true;
            }
            else
            {
                proc.StartInfo.CreateNoWindow = false;
            }
            proc.Start();

            if (redirectOutput)
            {
                //重定向,显示上传工具输出
                StreamReader sr = proc.StandardOutput;
                while (!sr.EndOfStream)
                {
                    string s = sr.ReadLine();
                    UnityEngine.Debug.Log(s);
                }
            }

            proc.WaitForExit();

            if (proc.ExitCode == 0)
            {
                UnityEngine.Debug.LogFormat("[{0}] 上传完毕!", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                UnityEngine.Debug.LogFormat("[{0}] 上传失败! ExitCode:{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), proc.ExitCode);
            }

        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError(String.Format("Exception Occurred :{0},{1}", ex.Message, ex.StackTrace.ToString()));
        }
    }

}
