using Dummiesman;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public static class ModelButtonGenerator
{
    private static readonly string modelsFolder = "Ready Models";
    public static string ModelsFolder => Path.Combine(Application.persistentDataPath, modelsFolder);

    private const string 
        texturePlaceholder = "Textures/Placeholder",
        previewImageName = "Preview.png";


    public static List<ModelButton> DTInit(GridLayoutGroup gridLayoutGroup)
    {
        List<ModelButton> list = new();

        //find or create the folder containing the models 
        string modelsPath = ModelsFolder;
        if (!Directory.Exists(modelsPath))
        {
            Directory.CreateDirectory(modelsPath);
            Debug.Log($"Model folder created at: {modelsPath}");
        }

        string[] folders = Directory.GetDirectories(modelsPath);

        foreach (string folderPath in folders)
        {
            Sprite previewImage = GetSprite(folderPath);
            if (previewImage == null)
            {
                Debug.Log("preview sprite null");
            }
            ModelButton button = DTCreateModelUiElement(Path.GetFileName(folderPath), previewImage, gridLayoutGroup);
            list.Add(button);
        }

        return list;
    }

    public static List<HM_SpawnModel> VRInit(HM_SpawnModel template)
    {
        List<HM_SpawnModel> list = new();

        //find or create the folder containing the models 
        string modelsPath = ModelsFolder;
        if (!Directory.Exists(modelsPath))
        {
            Directory.CreateDirectory(modelsPath);
            Debug.Log($"Model folder created at: {modelsPath}");
        }

        string[] folders = Directory.GetDirectories(modelsPath);

        foreach (string folderPath in folders)
        {
            Sprite previewImage = GetSprite(folderPath);
            if (previewImage == null)
            {
                Debug.LogError("preview sprite null");
            }

            HM_SpawnModel card = VRCreateModelUiElement(Path.GetFileName(folderPath), previewImage, template);
            list.Add(card);
        }

        return list;
    }

    /// <summary>
    /// Find and return the image preview in the folder of the model,
    /// if not present, it's generated
    /// </summary>
    /// <param name="folderPath">path to the model folder (contains model + preview image)</param>
    /// <returns>Sprite of the preview</returns>
    /// <exception cref="System.Exception"></exception>
    static private Sprite GetSprite(string folderPath)
    {
        Texture2D tex = null;

        //get texture from path
        var previewPath = Path.Combine(folderPath, previewImageName);
        if (File.Exists(previewPath))
        {
            byte[] bytes = File.ReadAllBytes(previewPath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
        }
        //generate texture
        else
        {
            string OBJPath = Directory.GetFiles(folderPath, "*.obj").FirstOrDefault();
            if (string.IsNullOrEmpty(OBJPath))
            {
                Debug.LogError($"Can't find any .obj file in {folderPath}");
            }
            else
            {
                GameObject OBJModel = new OBJLoader().Load(OBJPath);
                if (OBJModel != null)
                {
                    tex = PreviewGenerator.GeneratePrefabPreview(OBJModel);
                }
                else
                {
                    Debug.LogError($"Failed to load OBJ model: {OBJPath}");
                }
            }


            string path = Path.Combine(folderPath, previewImageName);
            File.WriteAllBytes(path, tex.EncodeToPNG());
        }
        //if generation failed, use a texture missing color tex
        if (tex == null)
        {
            // Load JPEG as TextAsset from Resources
            tex = Resources.Load<Texture2D>(texturePlaceholder);
        }

        Assert.AreNotEqual(tex, null,  "File not found in Resources: " + texturePlaceholder);

        //return the new sprite
        return Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));
    }

    static private ModelButton DTCreateModelUiElement(string name, Sprite previewImg, GridLayoutGroup contentMenu)
    {
        // Create GameObject
        GameObject go = new(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(ModelButton),
            typeof(Button)
            );

        // Set sprite
        Image img = go.GetComponent<Image>();
        img.sprite = previewImg;

        GameObject text = new(
            $"Text ({name})",
            typeof(TextMeshProUGUI)
        );

        var r = text.GetComponent<RectTransform>();
        float tWidth = contentMenu.cellSize.x;
        float tHeight = contentMenu.spacing.y * 0.8f;
        r.sizeDelta = new(tWidth, tHeight);

        // Set position directly below parent
        r.anchoredPosition = new Vector2(0, -contentMenu.cellSize.y/ 2 - tHeight/2);

        r.SetParent(go.transform, false);
        var t = text.GetComponent<TextMeshProUGUI>();
        t.text = name;
        t.enableAutoSizing = true;
        t.fontSizeMin = 10f;
        t.fontSizeMax = 72f;
        t.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Orbitron-VariableFont_wght SDF");
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.horizontalAlignment = HorizontalAlignmentOptions.Center;
        t.verticalAlignment = VerticalAlignmentOptions.Middle;

        return go.GetComponent<ModelButton>();
    }

    static private HM_SpawnModel VRCreateModelUiElement(string name, Sprite previewImg, HM_SpawnModel template)
    {
        GameObject card = GameObject.Instantiate(template.gameObject);
        card.name = name;

        if(card.transform.TryGetComponentOnlyInChildren<TextMeshProUGUI>(out var text))
            text.text = name;

        if(card.transform.TryGetComponentOnlyInChildren<Image>(out var image))
            image.sprite = previewImg;

        var _path = Path.Combine(ModelButtonGenerator.ModelsFolder, name);
        _path = Directory.GetFiles(_path, "*.obj", SearchOption.TopDirectoryOnly).FirstOrDefault();

        var hmEntry = card.GetComponent<HM_SpawnModel>();
        hmEntry.modelFullPath = _path;

        return hmEntry;
    }
}
