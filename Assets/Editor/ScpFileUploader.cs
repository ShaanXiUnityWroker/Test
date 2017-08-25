using UnityEngine;
using UnityEditor;
using Tamir.SharpSsh;


namespace Kamikami.EditorTools
{
    public class ScpFileUploader : EditorWindow
    {
        #region Data Members
        /// <summary>
        /// Const data Members
        /// </summary>
        


        /// <summary>
        /// Public data Members
        /// </summary>


        /// <summary>
        /// Private data Members
        /// </summary>
        string mServerIP = "192.168.159.200";
        string mUsername = string.Empty;
        string mPassword = string.Empty;
        string mSrcFolder = string.Empty;
        string mDesFolder = string.Empty;

        bool   mIsUploading;
        float  mUploadProgress;
        string mUploadRealtimeMessage;
        #endregion


        #region Function Members
        // ---------------------------- //
        // Show file upload window
        // ---------------------------- //
        [MenuItem("Window/Scp File Uploader")]
        public static void ShowWindow()
        {
            GetWindow<ScpFileUploader>().Show();
        }


        // ---------------------------- //
        // Use this to initialize
        // ---------------------------- //
        void OnEnable()
        {
            titleContent = new GUIContent("Scp File Uploader");
            minSize = new Vector2(400, 400);
        }


        // ---------------------------- //
        // Draw GUI
        // ---------------------------- //
        void OnGUI()
        {
            if (mIsUploading) {
                GUILayout.Label("Uploading...");
            } else {
                mServerIP = DrawInputField("Server IP : ", mServerIP);
                mUsername = DrawInputField("Username : ", mUsername);
                mPassword = DrawInputField("Password : ", mPassword);
                mSrcFolder = DrawInputField("Src Folder : ", mSrcFolder);
                mDesFolder = DrawInputField("Des Folder : ", mDesFolder);
                mIsUploading = GUILayout.Button("Upload");
                if (mIsUploading) {
                    StartToUpload();
                }
            }
        }


        void Reset()
        {
            mIsUploading = false;
            EditorUtility.ClearProgressBar();
        }


        string DrawInputField(string label, string initValue)
        {
            string input = null;
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(100));
            input = EditorGUILayout.TextArea(initValue);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);

            return input;
        }


        void StartToUpload()
        {
            mUploadRealtimeMessage = "Start to upload...";
            try {
                Scp scp = new Scp(mServerIP, mUsername, mPassword);
                scp.OnTransferStart += SshCp_OnTransferStart;
                scp.OnTransferProgress += SshCp_OnTransferProgress;
                scp.OnTransferEnd += SshCp_OnTransferEnd;

                mUploadRealtimeMessage = "Connecting...";
                scp.Connect();
                mUploadRealtimeMessage = "Connect OK!";

                if (System.IO.Directory.Exists(mSrcFolder)) {
                    scp.Recursive = true;
                } else {
                    scp.Recursive = false;
                }
                scp.Put(mSrcFolder, mDesFolder);
            } catch (System.Exception e) {
                Debug.LogError("Start scp exception : " + e.Message);
                Reset();
            }
        }

        private void SshCp_OnTransferStart(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            EditorUtility.DisplayProgressBar("Copy Progress", mUploadRealtimeMessage, mUploadProgress);
            mUploadProgress = (float)transferredBytes / totalBytes;
            mUploadRealtimeMessage = message;
        }

        private void SshCp_OnTransferProgress(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            mUploadProgress = (float)transferredBytes / totalBytes;
            mUploadRealtimeMessage = message;
        }

        private void SshCp_OnTransferEnd(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            Reset();
        }
        #endregion
    }
}
