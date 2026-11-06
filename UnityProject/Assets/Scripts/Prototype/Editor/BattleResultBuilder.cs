#if UNITY_EDITOR
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace HongikIngan.Editor
{
    public static class BattleResultBuilder
    {
        static readonly Color Ink = new Color(.22f,.09f,.29f);
        static TMP_FontAsset font;
        public static void Build()
        {
            Apply(); BattleResultChecks.Run(); SynergyUiChecks.Run(); PrototypeBuilder.BuildExpansion();
        }
        [MenuItem("HongikIngan/Apply battle result screens")]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Battle.unity");
            var screen = Object.FindFirstObjectByType<BattleScreen>();
            if (screen.rewardPopup != null && screen.rewardPopup.lines != null && screen.rewardPopup.lines.Length == 3)
            {
                var replacement=EnergyIcon();
                foreach(var row in screen.rewardPopup.lines)row.energyIcon.sprite=replacement;
                screen.rewardPopup.balanceLabel.transform.parent.Find("EnergyIcon").GetComponent<Image>().sprite=replacement;
                Validate(screen.rewardPopup); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                AssetDatabase.DeleteAsset("Assets/UI/Sprites/MiracleEnergyIcon.asset"); AssetDatabase.SaveAssets(); return;
            }
            if (screen.rewardPopup != null)
            {
                if (screen.rewardPopup.modal != null) Object.DestroyImmediate(screen.rewardPopup.modal);
                Object.DestroyImmediate(screen.rewardPopup);
            }
            font = BattleUiFont.DefaultFont;
            var tiles = AssetDatabase.LoadAllAssetsAtPath("Assets/sprite/UI/sides (1).png").OfType<Sprite>().OrderBy(s=>s.rect.y).ToArray();
            if (tiles.Length != 3) throw new InvalidOperationException("Expected the three existing transition slices.");
            var logo = AssetDatabase.LoadAllAssetsAtPath("Assets/sprite/UI/ui two.png").OfType<Sprite>().Single(s=>s.name=="battle_start");
            var icon = EnergyIcon();
            var root = new GameObject("BattleResultCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 2500;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600,900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var view = root.AddComponent<BattleRewardPopup>();
            var modal = Image(root.transform,"ResultScreen",0,0,1600,900,Color.clear); Stretch(modal.rectTransform); modal.raycastTarget = true;
            view.modal = modal.gameObject;
            view.cover = Image(modal.transform,"HeldTransitionSprite",0,0,2960,990,Color.white);
            Center(view.cover.rectTransform); view.cover.sprite = tiles[2]; view.rewardSprite = tiles[2]; view.gameOverSprite = tiles[1];
            var content = Rect(modal.transform,"Content",0,0,1600,900); Center(content);
            view.content = content.gameObject.AddComponent<CanvasGroup>();
            view.titleLabel = Text(content,"Title",250,75,1100,100,"스테이지 1",68,TextAlignmentOptions.Center);
            view.detailsLabel = Text(content,"Details",250,180,1100,105,"BATTLE REWARDS",25,TextAlignmentOptions.Center);
            var wallet = Image(content,"EnergyBalance",1080,20,440,75,new Color(1,.97f,.89f,.94f));
            var walletIcon = Image(wallet.transform,"EnergyIcon",12,7,62,62,Color.white); walletIcon.sprite = icon; walletIcon.preserveAspect = true;
            view.balanceLabel = Text(wallet.transform,"Amount",84,7,334,60,"100",38,TextAlignmentOptions.Right);
            Image(content,"ReceiptBorder",246,296,1108,358,Ink);
            var receipt = Image(content,"Receipt",252,302,1096,346,new Color(1,.97f,.89f,.96f));
            view.lines = new BattleRewardPopup.ResultLine[3];
            for(int i=0;i<3;i++)
            {
                var row = Rect(receipt.transform,"Row"+(i+1),28,20+i*102,1040,84);
                var item = new BattleRewardPopup.ResultLine { root=row.gameObject };
                item.label = Text(row,"Label",0,0,450,84,"",32,TextAlignmentOptions.Left);
                item.dots = Text(row,"DotLeaders",455,0,290,84,"",27,TextAlignmentOptions.Left);
                item.energyIcon = Image(row,"EnergyIcon",750,22,40,40,Color.white); item.energyIcon.sprite = icon; item.energyIcon.preserveAspect = true;
                item.value = Text(row,"Value",800,0,225,84,"",38,TextAlignmentOptions.Right);
                view.lines[i] = item;
            }
            var buttonImage = Image(content,"Confirm",630,735,340,82,Ink); buttonImage.raycastTarget = true;
            view.claimButton = buttonImage.gameObject.AddComponent<Button>(); view.claimButton.targetGraphic = buttonImage;
            view.claimButton.navigation = new Navigation { mode=Navigation.Mode.None };
            var label = Text(buttonImage.transform,"Label",10,5,320,72,"확인",36,TextAlignmentOptions.Center); label.color = new Color(1,.96f,.86f);
            view.returnLabel = Text(content,"ReturnPrompt",250,760,1100,60,"Press Any Key to Return",30,TextAlignmentOptions.Center);
            view.logo = Image(modal.transform,"BattleStartLogo",0,0,1296,720,Color.white); Center(view.logo.rectTransform);
            view.logo.sprite = logo; view.logo.preserveAspect = true;
            view.modal.SetActive(false); screen.rewardPopup = view;
            EditorUtility.SetDirty(screen); Validate(view);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("BATTLE_RESULTS_AUTHORED: orange reward, pink game over, existing logo and energy artwork.");
        }
        static Sprite EnergyIcon()
        {
            const string path = "Assets/UI/Sprites/MiracleEnergyIcon.png";
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.textureType!=TextureImporterType.Sprite || importer.spriteImportMode!=SpriteImportMode.Single)
            {
                importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
                importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void Validate(BattleRewardPopup view)
        {
            if(view.modal==null||view.cover==null||view.logo==null||view.content==null||view.claimButton==null||view.returnLabel==null||
                view.rewardSprite==null||view.gameOverSprite==null||view.lines.Length!=3)throw new InvalidOperationException("Result view references missing");
            foreach(var row in view.lines)if(row.label==null||row.dots==null||row.value==null||row.energyIcon.sprite==null)throw new InvalidOperationException("Result row missing");
        }
        static RectTransform Rect(Transform parent,string name,float x,float y,float width,float height)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);return rect;
        }
        static void Center(RectTransform rect){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;}
        static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static Image Image(Transform parent,string name,float x,float y,float width,float height,Color color)
        {var image=Rect(parent,name,x,y,width,height).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;}
        static TMP_Text Text(Transform parent,string name,float x,float y,float width,float height,string value,float size,TextAlignmentOptions alignment)
        {
            var text=Rect(parent,name,x,y,width,height).gameObject.AddComponent<TextMeshProUGUI>();text.font=font;text.text=value;text.fontSize=size;text.color=Ink;
            text.alignment=alignment;text.raycastTarget=false;text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;
            text.enableAutoSizing=true;text.fontSizeMin=20;text.fontSizeMax=size;return text;
        }
    }
}
#endif
