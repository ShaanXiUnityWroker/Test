using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;



namespace Kamikami.EditorTools
{
    public class TexturePacker : EditorWindow
    {
        public enum Operation
        {
            None,
            Create,
            Update,
            Delete
        }


        #region Data Members
        // ---------------------------- //
        // Const data Members
        // ---------------------------- //
        const int    MAX_TEXTURE_SIZE    = 2048;        // Max texture size(Unity limitation)
        const int    CREATE_TEXTURE_SIZE = 4096;        // Created texture size(must bigger than MAX_TEXTURE_SIZE)
        const string TEXTURE_OP_ADD      = "   Add";    // Add texture operation string
        const string TEXTURE_OP_UPDATE   = "Update";    // Update texture operation string


        // ---------------------------- //
        // Public data Members
        // ---------------------------- //


        // ---------------------------- //
        // Private data Members
        // ---------------------------- //
        Vector2                       mScroll;          // Scroll reference
        string                        mOutputFilePath;  // Output file path
        Texture2D                     mOutputTexture;   // Output texture
        List<Sprite>                  mSpriteList;      // Sprite already exist in atlas
        Dictionary<Texture2D, string> mSelectedTextures;// Selected texture in assets
        #endregion


        #region Function Members
        // ---------------------------- //
        // Show texture packer window
        // ---------------------------- //
        [MenuItem("Window/Texture Packer")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<TexturePacker>().Show();
        }


        // ---------------------------- //
        // Use this to initialize
        // ---------------------------- //
        void OnEnable()
        {
            titleContent = new GUIContent("Texture Packer");
            minSize = new Vector2(400, 400);
            mSpriteList = new List<Sprite>();
            mSelectedTextures = new Dictionary<Texture2D, string>();
        }


        // ---------------------------- //
        // Draw GUI
        // ---------------------------- //
        void OnGUI()
        {
            Operation op = Operation.None;
            Sprite spriteToRemove = null;

            // Display selected textures
            // and sprites in output atlas
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sprites");
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);


            GUILayout.BeginHorizontal();
            GUILayout.Space(3f);
            GUILayout.BeginVertical();

            mScroll = GUILayout.BeginScrollView(mScroll);

            // Show selected texture
            Sprite spriteInAtlas = null;
            foreach (KeyValuePair<Texture2D, string> keyValue in mSelectedTextures) {
                GUILayout.Space(-1f);
                GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                GUILayout.Label(keyValue.Key.name, GUILayout.Height(20f));

                // Display operation
                GUILayout.Label(keyValue.Value, GUILayout.Width(45));
                if (keyValue.Value == TEXTURE_OP_UPDATE) {                    
                    if (GUILayout.Button("X", GUILayout.Width(22f))) {
                        op = Operation.Delete;
                        spriteToRemove = spriteInAtlas;
                    }
                }
                GUILayout.EndHorizontal();
            }

            // Show sprites already in atlas
            foreach (Sprite s in mSpriteList) {
                GUILayout.Space(-1f);
                GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                GUILayout.Label(s.name, GUILayout.Height(20f));

                // Display operation
                if (GUILayout.Button("X", GUILayout.Width(22f))) {
                    op = Operation.Delete;
                    spriteToRemove = s;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.Space(3f);
            GUILayout.EndHorizontal();


            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Atlas", GUILayout.Width(45));
            // Define output texture
            Texture2D orgTexture = mOutputTexture;
            mOutputTexture = EditorGUILayout.ObjectField(mOutputTexture, typeof(Texture2D), false, GUILayout.MinWidth(200)) as Texture2D;
            if (mOutputTexture != orgTexture) {
                UpdateDisplayList();
            }

            // Display Update or Create button
            if (GUILayout.Button("Create", GUILayout.MaxWidth(80))) {
                op = Operation.Create;
            }

            if (mOutputTexture != null) {
                if (GUILayout.Button("Update", GUILayout.MaxWidth(80))) {
                    op = Operation.Update;
                }
            }
            GUILayout.EndHorizontal();


            // Process required operation
            switch (op) {
                case Operation.None:
                    // Do nothing
                    break;
                case Operation.Create:
                    CreateTexture();
                    break;
                case Operation.Update:
                    UpdateTexture();
                    break;
                case Operation.Delete:
                    DeleteTexture(spriteToRemove);
                    break;
                default:
                    Debug.LogError("Unexpected operation : " + op);
                    break;
            }
        }


        // ---------------------------- //
        // On selection change process
        // ---------------------------- //
        void OnSelectionChange()
        {
            UpdateDisplayList();

            Repaint();
        }


        // ---------------------------- //
        // On focus process
        // ---------------------------- //
        void OnFocus()
        {
            // Make sure output texture exist
            if (mOutputTexture != null) {
                string path = AssetDatabase.GetAssetPath(mOutputTexture);
                if (path == null || path.Length == 0) {
                    mOutputTexture = null;
                }
            }

            UpdateDisplayList();
        }


        // ---------------------------- //
        // Update display list
        // ---------------------------- //
        void UpdateDisplayList()
        {
            // Maintain selected textures
            // and sprites in output atlas
            if (mSpriteList != null && mSelectedTextures != null) {
                mSpriteList.Clear();
                mSelectedTextures.Clear();
                if (ValidateSelection()) {
                    // Update selected items
                    Object[] selectedTextureObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
                    foreach (Texture2D t in selectedTextureObjects) {
                        if (t != mOutputTexture) {
                            mSelectedTextures.Add(t, TEXTURE_OP_ADD);
                        }
                    }
                }

                // Update sprites in output atlas
                if (mOutputTexture != null) {
                    mOutputFilePath = AssetDatabase.GetAssetPath(mOutputTexture);
                    Object[] objs = AssetDatabase.LoadAllAssetsAtPath(mOutputFilePath);
                    if (objs != null && objs.Length > 1) {
                        // Get all sprite references
                        Sprite spriteToAdd = null;
                        mSpriteList.Clear();
                        for (int i = 1; i < objs.Length; i++) {
                            spriteToAdd = objs[i] as Sprite;
                            if (spriteToAdd != null) {
                                mSpriteList.Add(spriteToAdd);
                            } else {
                                Debug.LogError("Sprite conversion failed.");
                                break;
                            }
                        }
                    } else {
                        mOutputTexture = null;
                        Debug.LogError("Output texture is not a multiple sprite");
                    }
                }

                // Remove selected texture from output atlas
                Sprite spriteToRemove = null;
                List<Texture2D> texturesToUpdate = new List<Texture2D>();
                foreach (KeyValuePair<Texture2D, string> keyValue in mSelectedTextures) {
                    foreach (Sprite sp in mSpriteList) {
                        if (sp.name == keyValue.Key.name) {
                            spriteToRemove = sp;
                            texturesToUpdate.Add(keyValue.Key);
                            break;
                        }
                    }

                    if (spriteToRemove != null) {
                        mSpriteList.Remove(spriteToRemove);
                        spriteToRemove = null;
                    }
                }

                // Update texture display status
                foreach (Texture2D t in texturesToUpdate) {
                    mSelectedTextures[t] = TEXTURE_OP_UPDATE;
                }
            }
        }


        // ---------------------------- //
        // Update output texture
        // ---------------------------- //
        void UpdateTexture()
        {
            // ---------------------------- //
            // Setup textures to pack
            // ---------------------------- //
            int textureIndex = 0;
            Texture2D[] texturesToPack = new Texture2D[mSpriteList.Count + mSelectedTextures.Count];
            foreach (KeyValuePair<Texture2D, string> keyValue in mSelectedTextures) {
                texturesToPack[textureIndex++] = SetTextureReadable(AssetDatabase.GetAssetPath(keyValue.Key));
            }

            foreach (Sprite s in mSpriteList) {
                texturesToPack[textureIndex++] = CreateTextureFromSprite(s);
            }

            mOutputTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            Rect[] rects = mOutputTexture.PackTextures(texturesToPack, 1, CREATE_TEXTURE_SIZE);

            SaveTexture(mOutputTexture, mOutputFilePath);
            SetupAtlas(mOutputFilePath, rects, texturesToPack);

            UpdateDisplayList();
        }


        // ---------------------------- //
        // Create output texture
        // ---------------------------- //
        void CreateTexture()
        {
            mOutputFilePath = EditorUtility.SaveFilePanel("Save atlas as PNG file", Application.dataPath, "New Atlas", "png");
            if (mOutputFilePath != null && mOutputFilePath.Length > 0) {
                mSpriteList.Clear();
                UpdateTexture();
            }
        }


        // ---------------------------- //
        // Delete sprite from texture
        // ---------------------------- //
        void DeleteTexture(Sprite sprite)
        {
            mSpriteList.Remove(sprite);
            UpdateTexture();
        }


        // ---------------------------- //
        // Save output texture
        // ---------------------------- //
        void SaveTexture(Texture2D texture, string path)
        {
            try {
                byte[] imageData = texture.EncodeToPNG();
                File.WriteAllBytes(path, imageData);
                AssetDatabase.Refresh();
            } catch (System.Exception e) {
                Debug.LogError("Save file failed, " + e.ToString());
            }
        }

        
        // ---------------------------- //
        // Create texture from sprite
        // ---------------------------- //
        Texture2D CreateTextureFromSprite(Sprite sprite)
        {
            int x = (int)sprite.rect.x;
            int y = (int)sprite.rect.y;
            int width = (int)sprite.rect.width;
            int height = (int)sprite.rect.height;

            Texture2D result = new Texture2D(width, height);
            result.name = sprite.name;
            Texture2D sourceTexture = SetTextureReadable(AssetDatabase.GetAssetPath(sprite.texture));
            result.SetPixels(sourceTexture.GetPixels(x, y, width, height));

            return result;
        }


        // ---------------------------- //
        // Setup atlas texture
        // ---------------------------- //
        void SetupAtlas(string path, Rect[] rects, Texture2D[] packedTextures)
        {
            // ---------------------------- //
            // Make sure this is a relative path
            // ---------------------------- //
            if (path.StartsWith(Application.dataPath)) {
                path = path.Remove(0, Application.dataPath.Length - 6);
            }

            mOutputTexture = SetTextureReadable(path);
            if (mOutputTexture != null) {
                // ---------------------------- //
                // Setup every sprite properties
                // in this texture
                // ---------------------------- //
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null) {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Multiple;

                    // ---------------------------- //
                    // Create sprite sheet for the texture
                    // ---------------------------- //
                    int index = 0;
                    SpriteMetaData[] spritesheet = new SpriteMetaData[rects.Length];
                    foreach (Rect rect in rects) {
                        spritesheet[index] = new SpriteMetaData();
                        spritesheet[index].name = packedTextures[index].name;
                        spritesheet[index].rect = ConverseRect(mOutputTexture, rect);
                        spritesheet[index].alignment = (int)SpriteAlignment.Center;
                        index++;
                    }
                    importer.spritesheet = spritesheet;

                    // ---------------------------- //
                    // Save changes
                    // ---------------------------- //
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    mOutputTexture = SetTextureReadable(path);
                } else {
                    mOutputTexture = null;
                    Debug.LogError("Get TextureImporter of saved image failed!");
                }
            } else {
                Debug.LogError("Get Texture failed, Path : " + path);
            }
        }


        // ---------------------------- //
        // Set specified textures readable
        // ---------------------------- //
        Texture2D SetTextureReadable(string relativePathFile)
        {
            Texture2D texture = null;
            TextureImporter importer = AssetImporter.GetAtPath(relativePathFile) as TextureImporter;
            if (importer != null) {
                importer.maxTextureSize = MAX_TEXTURE_SIZE;
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (!settings.readable || settings.npotScale != TextureImporterNPOTScale.None) {
                    settings.readable = true;
                    //settings.textureFormat = TextureImporterFormat.ARGB32;
                    settings.npotScale = TextureImporterNPOTScale.None;
                    importer.SetTextureSettings(settings);
                    AssetDatabase.ImportAsset(relativePathFile, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                }

                // ---------------------------- //
                // Update asset DB and get texture
                // ---------------------------- //
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                texture = AssetDatabase.LoadAssetAtPath(relativePathFile, typeof(Texture2D)) as Texture2D;
                if (texture == null) {
                    Debug.LogError("Get texture failed, Path : " + relativePathFile);
                }
            } else {
                Debug.LogError("Set texture readable failed, texture path : " + relativePathFile);
            }

            return texture;
        }


        // ---------------------------- //
        // Convert rect from relative
        // based to pixel based
        // ---------------------------- //
        Rect ConverseRect(Texture2D tex, Rect rect)
        {
            rect.x = tex.width * rect.x;
            rect.y = tex.height * rect.y;
            rect.width = tex.width * rect.width;
            rect.height = tex.height * rect.height;

            // ---------------------------- //
            // Avoid sprite less than 1 pixel
            // ---------------------------- //
            rect.width = rect.width >= 1 ? rect.width : 1;
            rect.height = rect.height >= 1 ? rect.height : 1;

            return rect;
        }


        // ---------------------------- //
        // Validate selection if they r textures
        // ---------------------------- //
        bool ValidateSelection()
        {
            bool result = false;
            int objCount = Selection.GetFiltered(typeof(Object), SelectionMode.Assets).Length;
            if (objCount > 0) {
                result = (objCount == Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets).Length);
            }

            return result;
        }
        #endregion
    }
}
