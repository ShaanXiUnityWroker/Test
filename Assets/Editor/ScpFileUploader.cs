using UnityEngine;
using UnityEditor;
using Tamir.SharpSsh;
using System.IO;


namespace Kamikami.EditorTools
{
    public class ScpFileUploader : EditorWindow
    {
        #region Data Members
        /// <summary>
        /// Const data Members
        /// </summary>
        const string KEY_IP         = "ip";
        const string KEY_USERNAME   = "username";
        const string KEY_PASSWORD   = "password";
        const string KEY_SRC_FOLDER = "src folder";
        const string KEY_DES_FOLDER = "des folder";

        const float  SPEED_UPDATE_DURATION = 0.5f;


        /// <summary>
        /// Public data Members
        /// </summary>


        /// <summary>
        /// Private data Members
        /// </summary>
        string   mServerIP;
        string   mUsername;
        string   mPassword;
        string   mSrcFolder;
        string   mDesFolder;
                 
        float    mUploadProgress;
        int      mFileCount;
        int      mTransferredFileCount;
        Scp      mScp;

        // For UI display
        string   mUploadTitle;
                 
        long     mTotalFileSize;
        long     mTransferredFileSize;

        int      mLastTransferredBytes;
        double   mTransferSpeed;
        double   mTransferSpeedTimer;
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
            LoadSettings();
        }


        // ---------------------------- //
        // Draw GUI
        // ---------------------------- //
        void OnGUI()
        {
            mServerIP = DrawInputField("Server IP : ", mServerIP);
            mUsername = DrawInputField("Username : ", mUsername);
            mPassword = DrawInputField("Password : ", mPassword);
            mSrcFolder = DrawInputField("Src Folder : ", mSrcFolder);
            mDesFolder = DrawInputField("Des Folder : ", mDesFolder);
            bool uploading = GUILayout.Button("Upload");
            if (uploading) {
                StartToUpload();
            }
        }


        void Reset()
        {
            mTransferSpeed = 0;
            mLastTransferredBytes = 0;
            mTotalFileSize = 0;
            mTransferredFileSize = 0;
            mTransferredFileCount = 0;
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
            try {
                mScp = new Scp(mServerIP, mUsername, mPassword);
                mScp.OnTransferStart += SshCp_OnTransferStart;
                mScp.OnTransferProgress += SshCp_OnTransferProgress;
                mScp.OnTransferEnd += SshCp_OnTransferEnd;                
                mScp.Connect();

                if (Directory.Exists(mSrcFolder)) {
                    mScp.Recursive = true;
                    mFileCount = GetFileCount(mSrcFolder);
                } else {
                    mScp.Recursive = false;
                    mFileCount = 1;
                }

                mScp.To(mSrcFolder, mDesFolder);
            } catch (System.Exception e) {
                Debug.LogError("Start scp exception : " + e.Message);
                Reset();
            }
        }

        private void SshCp_OnTransferStart(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            mUploadTitle = "Copy Progress(" + (mTransferredFileCount + 1) + "/" + mFileCount + ")      ";
        }

        private void SshCp_OnTransferProgress(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            UpdateTransferSpeed(transferredBytes);
            mUploadProgress = (float)(mTransferredFileSize + transferredBytes) / mTotalFileSize;
            EditorUtility.DisplayProgressBar(mUploadTitle + mTransferSpeed.ToString("0.0") + "KB/S", src, mUploadProgress);
        }

        private void SshCp_OnTransferEnd(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            mTransferredFileCount++;
            mTransferredFileSize += totalBytes;
            if (mTransferredFileCount == mFileCount) {
                SaveSettings();
                mScp.Close();
                Reset();
            }
        }


        int GetFileCount(string dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);

            int fileCount = 0;
            FileInfo[] files = dirInfo.GetFiles();
            fileCount = files.Length;
            foreach (var f in files) {
                mTotalFileSize += f.Length;
            }

            // Get file count recursively
            DirectoryInfo[] dirs = dirInfo.GetDirectories();
            if (dirs != null && dirs.Length > 0) {
                foreach (var d in dirs) {
                    fileCount += GetFileCount(d.FullName);
                }
            }

            return fileCount;
        }

        void UpdateTransferSpeed(int transferredBytes)
        {
            double deltaTime = EditorApplication.timeSinceStartup - mTransferSpeedTimer;
            if (deltaTime > SPEED_UPDATE_DURATION) {
                mTransferSpeed = ((int)mTransferredFileSize + transferredBytes - mLastTransferredBytes) / deltaTime / 1024;
                mTransferSpeedTimer = EditorApplication.timeSinceStartup;
                mLastTransferredBytes = (int)mTransferredFileSize + transferredBytes;
            } 
        }


        void LoadSettings()
        {
            mServerIP = PlayerPrefs.GetString(KEY_IP);
            mUsername = PlayerPrefs.GetString(KEY_USERNAME);
            mPassword = PlayerPrefs.GetString(KEY_PASSWORD);
            mSrcFolder = PlayerPrefs.GetString(KEY_SRC_FOLDER);
            mDesFolder = PlayerPrefs.GetString(KEY_DES_FOLDER);
        }

        void SaveSettings()
        {
            PlayerPrefs.SetString(KEY_IP, mServerIP);
            PlayerPrefs.SetString(KEY_USERNAME, mUsername);
            PlayerPrefs.SetString(KEY_PASSWORD, mPassword);
            PlayerPrefs.SetString(KEY_SRC_FOLDER, mSrcFolder);
            PlayerPrefs.SetString(KEY_DES_FOLDER, mDesFolder);
            PlayerPrefs.Save();
        }
        #endregion
    }
}
