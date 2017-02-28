using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;



namespace Kamikami.EditorTools
{
    public class SpriteMerger : EditorWindow
    {
        #region Data Members
        // ---------------------------- //
        // Const data Members
        // ---------------------------- //
        const int MAX_TEXTURE_SIZE                   // Max texture size(Unity limitation)
#if UNITY_5
        = 8192;
#else
        = 2048;
#endif
        const string REPEAT_HINT_PREFIX = "Following sprite names are repeated!\n";


        // ---------------------------- //
        // Public data Members
        // ---------------------------- //


        // ---------------------------- //
        // Private data Members
        // ---------------------------- //
        Vector2                     mScroll;            // Scroll reference
        string                      mOutputFilePath;    // Output file path
        string                      mRepeatedSprites;   // Sprite already exist in atlas
        Texture2D                   mOutputTexture;     // Output texture
        List<Sprite>                mTempSprites;       // Temp sprite list for performance
        Dictionary<string, Sprite>  mSpritesToMerge;    // Sprites in selected textures
        #endregion


        #region Function Members
        // ---------------------------- //
        // Show texture packer window
        // ---------------------------- //
        [MenuItem("Window/Sprite Merger")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow<SpriteMerger>().Show();
        }


        // ---------------------------- //
        // Use this to initialize
        // ---------------------------- //
        void OnEnable()
        {
            titleContent.text = "Sprite Merger";
            minSize = new Vector2(400, 400);
            if (mTempSprites == null) mTempSprites = new List<Sprite>();
            if (mSpritesToMerge == null) mSpritesToMerge = new Dictionary<string, Sprite>();
        }


        // ---------------------------- //
        // Draw GUI
        // ---------------------------- //
        void OnGUI()
        {
            // Display selected textures
            // and sprites in output atlas
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Sprites");
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);

            if (UpdateSprites()) {
                GUILayout.BeginHorizontal();
                GUILayout.Space(3f);
                GUILayout.BeginVertical();

                // Show sprites in selected texture
                // If none of their name is repeated
                mScroll = GUILayout.BeginScrollView(mScroll);
                foreach (var s in mSpritesToMerge) {
                    GUILayout.Space(-1f);
                    GUILayout.BeginHorizontal("AS TextArea", GUILayout.MinHeight(20f));
                    GUILayout.Label(s.Value.name, GUILayout.Height(20f));
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                GUILayout.Space(3f);
                GUILayout.EndHorizontal();

                if (mSpritesToMerge.Count > 0) {
                    // Display "Merge" buttom
                    GUILayout.Space(10f);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Merge", GUILayout.Height(30))) {
                        MergeTexture();
                    }
                    GUILayout.EndHorizontal();
                }
            } else {
                // Show repeated names of selected textures
                GUILayout.Label(mRepeatedSprites, GUILayout.MinHeight(100));
            }
        }


        // ---------------------------- //
        // Setup sprites of selected textures
        // ---------------------------- //
        bool UpdateSprites()
        {
            bool result = true;

            // Update sprite list by selected textures
            mSpritesToMerge.Clear();
            mRepeatedSprites = REPEAT_HINT_PREFIX;
            Object[] selectedTextureObjects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            foreach (Texture2D t in selectedTextureObjects) {
                foreach (Sprite s in GetSpritesFromTexture(t)) {
                    if (!mSpritesToMerge.ContainsKey(s.name)) {
                        // Add to merge list if name is unique
                        mSpritesToMerge.Add(s.name, s);
                    } else {
                        // Record repeated sprite and texture name
                        result = false;
                        mRepeatedSprites += s.name + " in " + t.name + "\n";
                    }
                }
            }

            return result;
        }


        // ---------------------------- //
        // Get all sprites in selected textrue
        // ---------------------------- //
        Sprite[] GetSpritesFromTexture(Texture2D texture)
        {
            Sprite[] result = null;
            mTempSprites.Clear();

            // Get sprites in the texture
            Sprite sprite = null;
            string path = AssetDatabase.GetAssetPath(texture);
            Object[] objs = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var o in objs) {
                sprite = o as Sprite;
                if (sprite != null)
                    mTempSprites.Add(sprite);
            }

            if (mTempSprites.Count > 0) {
                result = mTempSprites.ToArray();
            } else {
                Debug.LogError("No sprite found in texture : " + texture.name);
            }

            return result;
        }


        // ---------------------------- //
        // Create output texture
        // ---------------------------- //
        void MergeTexture()
        {
            mOutputFilePath = EditorUtility.SaveFilePanel("Save atlas as PNG file", Application.dataPath, "New Atlas", "png");
            if (mOutputFilePath != null && mOutputFilePath.Length > 0) {
                // Create textures to pack from sprites 
                int textureIndex = 0;
                Texture2D[] texturesToPack = new Texture2D[mSpritesToMerge.Count];
                foreach (var s in mSpritesToMerge) {
                    texturesToPack[textureIndex++] = CreateTextureFromSprite(s.Value);
                }

                // Pack textures as one file
                mOutputTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                Rect[] rects = mOutputTexture.PackTextures(texturesToPack, 1, MAX_TEXTURE_SIZE * 2);
                
                // Save the texture
                SaveTexture(mOutputTexture, mOutputFilePath);

                // Setup its sprites
                SetupAtlas(mOutputFilePath, rects, texturesToPack);
            }
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
                    importer.mipmapEnabled = false;

                    // ---------------------------- //
                    // Create sprite sheet for the texture
                    // ---------------------------- //
                    int index = 0;
                    Sprite sprite = null;
                    SpriteMetaData[] spritesheet = new SpriteMetaData[rects.Length];
                    foreach (Rect rect in rects) {
                        spritesheet[index] = new SpriteMetaData();
                        spritesheet[index].name = packedTextures[index].name;
                        spritesheet[index].alignment = (int)SpriteAlignment.Custom;
                        spritesheet[index].rect = ConverseRect(mOutputTexture, rect);
                        if (mSpritesToMerge.ContainsKey(spritesheet[index].name)) {
                            sprite = mSpritesToMerge[spritesheet[index].name];
                            spritesheet[index].border = sprite.border;
                            spritesheet[index].pivot = new Vector2(sprite.pivot.x / sprite.rect.size.x, sprite.pivot.y / sprite.rect.size.y);
                        } else {
                            Debug.LogError("Sprite : " + packedTextures[index].name + " is not found in sprite pack list!");
                        }
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



        #region Editor event handler
        // ---------------------------- //
        // On selection change process
        // ---------------------------- //
        void OnSelectionChange()
        {
            UpdateSprites();

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

            UpdateSprites();
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
        #endregion
    }
}
