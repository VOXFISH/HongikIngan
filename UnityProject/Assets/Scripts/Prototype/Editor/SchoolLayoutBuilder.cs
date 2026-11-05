#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HongikIngan.Editor
{
    /// <summary>Creates real, editable scene UI. No textures or portrait artwork are generated.</summary>
    public static class SchoolLayoutBuilder
    {
        static readonly Color Ink = Hex("30344F"), Pink = Hex("D9629F"), Navy = Hex("243D59");
        static readonly Color Cream = Hex("FFF6E4"), Gold = Hex("CFA76C"), Paper = Hex("FFFDF7");
        static Font font;

        [MenuItem("HongikIngan/Apply School reference layout")]
        public static void Apply()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/School.unity");
            var screen = UnityEngine.Object.FindFirstObjectByType<SchoolScreen>();
            if (screen == null) throw new InvalidOperationException("SchoolScreen is missing.");
            // Never overwrite subsequent hand edits to a scene already using this layout.
            if (screen.schoolCanvas != null)
            {
                ValidateBindings(screen);
                Debug.Log("School layout already exists; edit its Canvas in the scene.");
                return;
            }
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("SchoolCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440, 810); scaler.matchWidthOrHeight = .5f;
            screen.schoolCanvas = root.GetComponent<CanvasGroup>();
            Transform ui = root.transform;
            Image(ui, "ClassroomBackdrop", 0, 0, 1440, 810, Hex("F2DDE6"));
            Image(ui, "WallWainscot", 0, 275, 1440, 300, Hex("E9C9CE"));
            Image(ui, "Floor", 0, 574, 1440, 236, Hex("E1C5AD"));
            Image(ui, "WallRail", 0, 280, 1440, 7, Gold);
            for (int i = 0; i < 12; i++) Image(ui, "WallPanel" + i, i * 130, 90, 3, 185, Hex("E5BCCF"));

            var energy = Frame(ui, "Energy", 24, 18, 228, 64, Pink, Cream, 3);
            screen.energyLabel = Text(energy, "Value", 12, 4, 204, 56, "미라클 에너지\n160", 19, Ink, TextAnchor.MiddleCenter);
            var roster = Frame(ui, "Roster", 264, 18, 160, 64, Navy, Hex("EDF4FA"), 3);
            screen.rosterLabel = Text(roster, "Value", 8, 4, 144, 56, "전대 인원\n1 / 3", 18, Navy, TextAnchor.MiddleCenter);
            var photos = Frame(ui, "Photos", 436, 18, 194, 64, Pink, Cream, 3);
            screen.photosLabel = Text(photos, "Value", 8, 4, 178, 56, "폴라로이드 0", 18, Ink, TextAnchor.MiddleCenter);
            screen.stageLabel = Text(ui, "Stage", 648, 25, 300, 45, "STAGE 01 · 방과 전", 24, Ink, TextAnchor.MiddleLeft);
            screen.restartButton = Button(ui, "NewGame", 1128, 24, 126, 40, "새 게임", Hex("FFF6ED"), Ink);
            screen.audioButton = Button(ui, "Sound", 1266, 24, 146, 40, "사운드 설정", Hex("FFF6ED"), Ink);

            var board = Frame(ui, "Chalkboard", 240, 100, 700, 194, Gold, Hex("335653"), 9);
            Text(board, "Caption", 20, 7, 660, 29, "우리들의 보물 칠판", 20, Cream, TextAnchor.MiddleCenter);
            screen.relicButtons = new Button[3]; screen.relicLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Button(board, "RelicOffer" + i, 28 + i * 220, 43, 204, 98, "", Hex("F6EEE2"), Ink);
                Blank(b.transform, "RelicPlaceholder", 10, 10, 54);
                screen.relicButtons[i] = b;
                screen.relicLabels[i] = Text(b.transform, "Offer", 73, 10, 121, 78, "유물\n30 에너지", 17, Ink, TextAnchor.MiddleCenter);
            }
            screen.ownedRelicsLabel = Text(board, "OwnedRelics", 25, 149, 650, 31, "아직 획득한 유물이 없어요", 15, Cream, TextAnchor.MiddleCenter);

            var recruits = Frame(ui, "Recruitment", 24, 100, 200, 194, Pink, Hex("FFF5F8"), 3);
            Text(recruits, "Caption", 8, 5, 184, 26, "전학생 모집", 19, Ink, TextAnchor.MiddleCenter);
            screen.recruitButtons = new Button[3]; screen.recruitNames = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Button(recruits, "Recruit" + i, 9, 37 + i * 49, 182, 44, "", Hex("F4DFEB"), Ink);
                Blank(b.transform, "PortraitPlaceholder", 5, 4, 36);
                screen.recruitButtons[i] = b;
                screen.recruitNames[i] = Text(b.transform, "Name", 49, 2, 128, 40, "학생 이름", 15, Ink, TextAnchor.MiddleLeft);
            }
            var pets = Frame(ui, "Pets", 958, 100, 454, 194, Pink, Hex("FFF5F8"), 3);
            Text(pets, "Caption", 8, 5, 438, 28, "새로운 인연 · 펫 부르기", 20, Pink, TextAnchor.MiddleCenter);
            screen.petButtons = new Button[3]; screen.petLabels = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                var b = Button(pets, "Pet" + i, 12 + i * 147, 38, 137, 143, "", Hex("F5E5F0"), Ink);
                Blank(b.transform, "PetPlaceholder", 40, 8, 57);
                screen.petButtons[i] = b;
                screen.petLabels[i] = Text(b.transform, "Offer", 6, 73, 125, 61, "펫 부르기", 15, Ink, TextAnchor.MiddleCenter);
            }

            Text(ui, "FormationCaption", 30, 301, 760, 29, "우리 전대  /  학생을 끌어 자리를 바꿔 보세요", 18, Ink, TextAnchor.MiddleLeft);
            var row = Rect(ui, "StudentDesks", 24, 340, 1080, 280);
            screen.slots = new SchoolStudentSlot[9];
            for (int i = 0; i < screen.slots.Length; i++)
            {
                var desk = Image(row, "DeskSlot" + i, i * 216, 0, 200, 280, new Color(0, 0, 0, 0));
                desk.raycastTarget = true;
                var slot = desk.gameObject.AddComponent<SchoolStudentSlot>();
                // The hit area stays on the desk while the student visual follows the pointer.
                Image(desk.transform, "DeskLegLeft", 23, 246, 11, 34, Hex("9D8079"));
                Image(desk.transform, "DeskLegRight", 166, 246, 11, 34, Hex("9D8079"));
                Frame(desk.transform, "Desktop", 0, 235, 200, 16, Hex("AC805F"), Hex("E8BA82"), 3);
                slot.emptyLabel = Text(desk.transform, "EmptyLabel", 10, 92, 180, 60, i < 3 ? "빈 책상" : "확장 가능한 책상", 18, Hex("9E8295"), TextAnchor.MiddleCenter);
                slot.studentVisual = Rect(desk.transform, "StudentVisual", 0, 0, 200, 208);
                slot.visualGroup = slot.studentVisual.gameObject.AddComponent<CanvasGroup>();
                slot.selectionFrame = Image(slot.studentVisual, "Selected", 12, 31, 176, 176, Pink);
                slot.nameLabel = Text(slot.studentVisual, "Name", 5, 0, 190, 29, "학생 이름", 20, Ink, TextAnchor.MiddleCenter);
                Blank(slot.studentVisual, "StudentPlaceholder", 16, 35, 168);
                slot.roleLabel = Text(slot.studentVisual, "Role", 16, 171, 168, 30, "1학년 · Dealer", 16, Ink, TextAnchor.MiddleCenter);
                slot.books = new GameObject[3]; slot.bookLabels = new Text[3];
                for (int b = 0; b < 3; b++)
                {
                    var book = Book(desk.transform, "SynergyBook" + b, 29 + b * 5, 219 - b * 20, 142, 19, b == 1 ? Hex("D787A7") : Navy);
                    slot.books[b] = book.gameObject;
                    slot.bookLabels[b] = Text(book, "Synergy", 16, 0, 118, 14, "시너지", 11, Cream, TextAnchor.MiddleCenter);
                }
                screen.slots[i] = slot;
                if (i > 4) desk.gameObject.SetActive(false);
            }
            var synergies = Frame(ui, "SynergyPanel", 1122, 318, 290, 229, Gold, Navy, 3);
            Text(synergies, "Title", 12, 8, 266, 32, "전대 시너지", 22, Cream, TextAnchor.MiddleCenter);
            var viewport = Image(synergies, "Viewport", 16, 48, 258, 166, new Color(0, 0, 0, 0));
            viewport.gameObject.AddComponent<RectMask2D>(); viewport.raycastTarget = true;
            screen.synergyLabel = Text(viewport.transform, "Counts", 0, 0, 258, 166, "보유 시너지", 18, Cream, TextAnchor.UpperLeft);
            var fit = screen.synergyLabel.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = (RectTransform)viewport.transform;
            scroll.content = screen.synergyLabel.rectTransform; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            screen.expandButton = Button(ui, "ExpandDesks", 1122, 560, 290, 42, "책상 확장", Hex("FBF0DF"), Ink);
            screen.statusLabel = Text(ui, "Status", 32, 607, 1370, 26, "교실에 오신 것을 환영합니다.", 16, Ink, TextAnchor.MiddleLeft);

            screen.rerollButton = Button(ui, "Reroll", 24, 661, 194, 120, "새로운 만남\n리롤 20", Pink, Color.white);
            var detail = Frame(ui, "StudentDetailBook", 238, 649, 864, 138, Gold, Cream, 4);
            Image(detail, "LeftPage", 9, 8, 155, 122, Paper);
            Image(detail, "RightPage", 172, 8, 682, 122, Paper);
            Image(detail, "Binding", 165, 5, 5, 128, Hex("D8C5AE"));
            Blank(detail, "PortraitPlaceholder", 36, 20, 104);
            screen.detailTitle = Text(detail, "Name", 189, 9, 470, 30, "우리들의 교실", 22, Ink, TextAnchor.MiddleLeft);
            screen.detailBody = Text(detail, "Description", 189, 45, 470, 84, "학생을 선택하세요", 17, Ink, TextAnchor.UpperLeft);
            screen.learnButton = Button(detail, "Upgrade", 678, 12, 166, 49, "보충 학습", Navy, Color.white);
            screen.sellButton = Button(detail, "Release", 678, 75, 166, 49, "계약 해제", Pink, Color.white);
            screen.recruitButton = Button(detail, "Recruit", 678, 40, 166, 64, "영입하기", Navy, Color.white);
            screen.replaceButton = Button(detail, "Replace", 678, 40, 166, 64, "교체 · 기존 학생 판매", Navy, Color.white);
            screen.recruitButton.gameObject.SetActive(false); screen.replaceButton.gameObject.SetActive(false);
            screen.battleButton = Button(ui, "EnterBattle", 1122, 661, 290, 120, "방과 후!\n전투 시작", Pink, Color.white);
            screen.dragLayer = Rect(ui, "DragLayer", 0, 0, 1440, 810);

            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("SchoolEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            EditorUtility.SetDirty(screen);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            ValidateBindings(screen);
            PrototypeChecks.Run();
            Debug.Log("SCHOOL_REFERENCE_LAYOUT_SAVED");
        }
        static void ValidateBindings(SchoolScreen screen)
        {
            var serialized = new SerializedObject(screen);
            var property = serialized.GetIterator();
            while (property.NextVisible(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                    throw new InvalidOperationException("Missing School reference: " + property.propertyPath);
            if (screen.slots.Length != 9 || screen.recruitButtons.Length != 3 || screen.petButtons.Length != 3 || screen.relicButtons.Length != 3)
                throw new InvalidOperationException("Incorrect School slot/offer bindings.");
            foreach (var slot in screen.slots)
            {
                if (slot.studentVisual == null || slot.visualGroup == null || slot.books.Length != 3 || slot.bookLabels.Length != 3)
                    throw new InvalidOperationException("Incomplete desk: " + slot.name);
                foreach (var book in slot.books)
                    if (book.transform.Find("Spine") == null || book.transform.Find("PageBlock") == null)
                        throw new InvalidOperationException("Book cover/spine/pages are missing.");
            }
            foreach (var image in screen.schoolCanvas.GetComponentsInChildren<Image>(true))
                if (image.sprite != null) throw new InvalidOperationException("School placeholders should not contain artwork.");
            Debug.Log("SCHOOL_LAYOUT_BINDINGS_PASS: nine desks, three offers per category, blank images, book geometry.");
        }
        [MenuItem("HongikIngan/Verify School cursor drag (Play Mode)")]
        public static void VerifyDrag()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run School in Play Mode first.");
            var screen = UnityEngine.Object.FindFirstObjectByType<SchoolScreen>();
            if (screen == null || !screen.CanInteract || PrototypeSession.Ensure().Run.Students.Count == 0)
                throw new InvalidOperationException("School needs one student and no open modal.");
            ValidateBindings(screen);
            var run = PrototypeSession.Ensure().Run;
            SchoolStudentSlot slot = null;
            foreach (var candidate in screen.slots)
                if (run.StudentAtSchoolDesk(candidate.Index) != null) { slot = candidate; break; }
            if (slot == null) throw new InvalidOperationException("No occupied School desk was found.");
            var originalParent = slot.studentVisual.parent;
            var originalPosition = slot.studentVisual.localPosition;
            var originalWorld = slot.studentVisual.position;
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, originalWorld),
                pointerDrag = slot.gameObject
            };
            try
            {
                slot.OnBeginDrag(pointer);
                if (!slot.IsDragging || slot.visualGroup.blocksRaycasts) throw new InvalidOperationException("Drag capture failed.");
                pointer.position += new Vector2(93, -47);
                slot.OnDrag(pointer);
                var actualDelta = slot.studentVisual.position - originalWorld;
                if (Vector3.Distance(actualDelta, new Vector3(93, -47, 0)) > .1f)
                    throw new InvalidOperationException("Student did not follow cursor delta: " + actualDelta);
                slot.OnEndDrag(pointer);
                if (slot.IsDragging || slot.studentVisual.parent != originalParent || Vector3.Distance(slot.studentVisual.localPosition, originalPosition) > .01f)
                    throw new InvalidOperationException("Invalid-drop return failed.");
                Debug.Log("SCHOOL_CURSOR_DRAG_PASS: held drag follows screen cursor; raycasts released; invalid drop restores desk.");
            }
            finally { slot.CancelDrag(); }
        }
        static Color Hex(string html) { ColorUtility.TryParseHtmlString("#" + html, out var value); return value; }
        static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
        }
        static Image Image(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            var image = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        static RectTransform Frame(Transform p, string name, float x, float y, float w, float h, Color border, Color fill, float thickness)
        {
            var frame = Image(p, name, x, y, w, h, border).rectTransform;
            Image(frame, "Fill", thickness, thickness, w - thickness * 2, h - thickness * 2, fill); return frame;
        }
        static void Blank(Transform p, string name, float x, float y, float size)
        {
            Frame(p, name, x, y, size, size, Hex("D9C4D2"), Hex("F6EDF3"), 2);
        }
        static RectTransform Book(Transform p, string name, float x, float y, float w, float h, Color cover)
        {
            var book = Image(p, name, x, y, w, h, cover).rectTransform;
            Image(book, "PageBlock", 10, h - 6, w - 14, 4, Cream);
            Image(book, "PageLine", 12, h - 4, w - 18, 1, Hex("CDBD9F"));
            Image(book, "Spine", 0, 0, 9, h, Hex("263650"));
            Image(book, "Gilding", 11, 1, w - 15, 1, Gold);
            return book;
        }
        static Text Text(Transform p, string name, float x, float y, float w, float h, string value, int size, Color color, TextAnchor align)
        {
            var text = Rect(p, name, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color; text.alignment = align;
            text.raycastTarget = false; text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        static Button Button(Transform p, string name, float x, float y, float w, float h, string value, Color fill, Color foreground)
        {
            var image = Image(p, name, x, y, w, h, fill); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(.95f, .88f, .96f); colors.pressedColor = new Color(.85f, .76f, .87f);
            button.colors = colors;
            var label = Text(button.transform, "Label", 8, 3, w - 16, h - 6, value, 20, foreground, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            return button;
        }
    }
}
#endif
