using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static JsonData;

public class ModernUIAutoBuilder : MonoBehaviour
{
    private Canvas mainCanvas;
    private RectTransform rootRect;
    private RectTransform topCarouselRect;
    private RectTransform bottomListContent;
    private ScrollRect bottomScrollRect;
    private InputField searchInput;
    private GameObject sortPanel;
    private Text sortStatusText;
    private GameObject detailsPanel;
    private RawImage detailsCover;
    private Text detailsName;
    private Text detailsTitleId;
    private Text detailsVersion;
    private Text detailsRelease;
    private Text detailsSize;
    private Text detailsMinFW;
    private GameObject downloadPanel;
    private Text downloadHeaderText;
    private Text downloadDownloadedText;
    private Text downloadPausedText;
    private Text downloadCancelledText;
    private Slider downloadProgressBar;
    private List<KeyValuePair<string, GameContent>> allGames = new List<KeyValuePair<string, GameContent>>();
    private List<KeyValuePair<string, GameContent>> filteredGames = new List<KeyValuePair<string, GameContent>>();
    private SortBy currentSort = SortBy.Name;
    private string currentSearch = string.Empty;
    private Dictionary<string, bool> sidebarToggleStates = new Dictionary<string, bool>();
    private HashSet<string> selectedRegions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "USA",
        "Europe",
        "Japan",
        "Asia"
    };
    private ContentType selectedContentType = ContentType.ALL;
    private bool enableAppUpdates = true;
    private bool populateViaWeb = false;
    private int currentBackgroundIndex = 0;
    private string alternateDownloadPath = string.Empty;
    private Text footerContentInfoText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeBuilder()
    {
        GameObject bootstrap = new GameObject("ModernUIAutoBuilder");
        DontDestroyOnLoad(bootstrap);
        bootstrap.AddComponent<ModernUIAutoBuilder>();
    }

    private void Awake()
    {
        StartCoroutine(InitializeWhenDataReady());
    }

    private IEnumerator InitializeWhenDataReady()
    {
        while (ContentHandler.allContentCache == null || ContentHandler.allContentCache.Count == 0)
            yield return null;

        LoadContentData();
        BuildUI();
        RefreshUI();
    }

    private void LoadContentData()
    {
        allGames = ContentHandler.allContentCache.ToList();
        filteredGames = new List<KeyValuePair<string, GameContent>>(allGames);
    }

    private void BuildUI()
    {
        CreateCanvas();
        CreateBackground();
        CreateHeader();
        CreateTopCarousel();
        CreateSearchBar();
        CreateBottomList();
        CreateDetailsPanel();
        CreateDownloadPanel();
        CreateSidebarMenu();
        CreateFooterBar();
    }

    private void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("ModernUICanvas");
        canvasObj.layer = LayerMask.NameToLayer("UI");
        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        rootRect = canvasObj.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        CreateEventSystemIfNeeded();
    }

    private void CreateEventSystemIfNeeded()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
        DontDestroyOnLoad(es);
    }

    private void CreateBackground()
    {
        GameObject bgObj = CreateUIObject("Background", rootRect);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.04f, 0.08f, 0.18f, 1f);
        SetRect(bgObj, new Vector2(0, 0), new Vector2(1, 1));

        GameObject glow = CreateUIObject("GlowOverlay", bgObj.transform);
        SetRect(glow, new Vector2(0, 0.4f), new Vector2(1, 1));
        Image glowImage = glow.AddComponent<Image>();
        glowImage.color = new Color(0.15f, 0.30f, 0.62f, 0.14f);

        GameObject bottomGlow = CreateUIObject("BottomGlow", bgObj.transform);
        SetRect(bottomGlow, new Vector2(0, 0), new Vector2(1, 0.25f));
        Image bottomGlowImage = bottomGlow.AddComponent<Image>();
        bottomGlowImage.color = new Color(0.08f, 0.14f, 0.28f, 0.28f);
    }

    private void CreateHeader()
    {
        GameObject headerObj = CreateUIObject("Header", rootRect);
        SetRect(headerObj, new Vector2(0.02f, 0.91f), new Vector2(0.98f, 0.99f));
        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = new Color(0.02f, 0.06f, 0.14f, 0.92f);
        headerBg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        headerBg.type = Image.Type.Sliced;

        HorizontalLayoutGroup hlg = headerObj.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 10, 10);
        hlg.spacing = 18;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;

        GameObject titleGroup = CreateUIObject("TitleGroup", headerObj.transform);
        VerticalLayoutGroup vlg = titleGroup.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = false;

        Text title = CreateText(titleGroup.transform, "PS4  FPKGi", 26, FontStyle.Bold, TextAnchor.MiddleLeft);
        title.color = new Color(0.86f, 0.92f, 1f, 1f);
        Text subtitle = CreateText(titleGroup.transform, "FPKGi v2.1.0 Downloads & Status", 16, FontStyle.Normal, TextAnchor.MiddleLeft);
        subtitle.color = new Color(0.72f, 0.80f, 0.92f, 1f);

        GameObject spacer = new GameObject("HeaderSpacer");
        spacer.transform.SetParent(headerObj.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

        GameObject statusGroup = CreateUIObject("StatusGroup", headerObj.transform);
        HorizontalLayoutGroup statusHlg = statusGroup.AddComponent<HorizontalLayoutGroup>();
        statusHlg.spacing = 14;
        statusHlg.childAlignment = TextAnchor.MiddleRight;
        statusHlg.childForceExpandHeight = false;
        statusHlg.childForceExpandWidth = false;

        CreateText(statusGroup.transform, "CPU: 58°C / 136°F", 16, FontStyle.Normal, TextAnchor.MiddleRight).color = new Color(0.78f, 0.88f, 0.96f, 1f);
        CreateText(statusGroup.transform, "120.3 MB", 16, FontStyle.Normal, TextAnchor.MiddleRight).color = new Color(0.78f, 0.88f, 0.96f, 1f);
        CreateText(statusGroup.transform, "4.28 GB", 16, FontStyle.Normal, TextAnchor.MiddleRight).color = new Color(0.78f, 0.88f, 0.96f, 1f);
    }

    private void CreateTopCarousel()
    {
        GameObject topPanel = CreateUIObject("TopCarouselPanel", rootRect);
        SetRect(topPanel, new Vector2(0.02f, 0.67f), new Vector2(0.78f, 0.88f));
        Image panelImage = topPanel.AddComponent<Image>();
        panelImage.color = new Color(0.03f, 0.08f, 0.18f, 0.82f);
        panelImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        panelImage.type = Image.Type.Sliced;

        GameObject scrollObj = CreateUIObject("TopCarouselScroll", topPanel.transform);
        SetRect(scrollObj, new Vector2(0, 0), new Vector2(1, 1));
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        GameObject viewport = CreateUIObject("Viewport", scrollObj.transform);
        SetRect(viewport, new Vector2(0, 0), new Vector2(1, 1));
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);

        GameObject content = CreateUIObject("Content", viewport.transform);
        topCarouselRect = content.GetComponent<RectTransform>();
        topCarouselRect.anchorMin = new Vector2(0, 0.5f);
        topCarouselRect.anchorMax = new Vector2(0, 0.5f);
        topCarouselRect.pivot = new Vector2(0, 0.5f);
        topCarouselRect.anchoredPosition = new Vector2(20, 0);

        HorizontalLayoutGroup hlg = content.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20;
        hlg.padding = new RectOffset(20, 20, 20, 20);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = topCarouselRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
    }

    private void CreateSearchBar()
    {
        GameObject searchPanel = CreateUIObject("SearchPanel", rootRect);
        SetRect(searchPanel, new Vector2(0.02f, 0.62f), new Vector2(0.78f, 0.67f));
        Image bg = searchPanel.AddComponent<Image>();
        bg.color = new Color(0.00f, 0.04f, 0.10f, 0.90f);
        bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;

        HorizontalLayoutGroup hlg = searchPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(18, 18, 10, 10);
        hlg.spacing = 14;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;

        searchInput = CreateInputField(searchPanel.transform, "SEARCH...", 0.68f);
        GameObject sortButton = CreateButton(searchPanel.transform, "SortButton", "SORT", 0.14f);
        sortButton.GetComponent<Button>().onClick.AddListener(ToggleSortPanel);

        sortStatusText = CreateText(searchPanel.transform, "Sort: Name", 16, FontStyle.Normal, TextAnchor.MiddleLeft);
        sortStatusText.color = new Color(0.74f, 0.84f, 0.94f, 1f);
        sortStatusText.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 28);

        sortPanel = CreateSortPanel(rootRect);
    }

    private void CreateBottomList()
    {
        GameObject bottomPanel = CreateUIObject("BottomListPanel", rootRect);
        SetRect(bottomPanel, new Vector2(0.02f, 0.10f), new Vector2(0.78f, 0.54f));
        Image bg = bottomPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.06f, 0.12f, 0.92f);
        bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;

        GameObject scrollObj = CreateUIObject("BottomScroll", bottomPanel.transform);
        SetRect(scrollObj, new Vector2(0, 0), new Vector2(1, 1));
        bottomScrollRect = scrollObj.AddComponent<ScrollRect>();
        bottomScrollRect.horizontal = false;
        bottomScrollRect.vertical = true;
        bottomScrollRect.inertia = true;
        bottomScrollRect.scrollSensitivity = 28f;

        GameObject viewport = CreateUIObject("Viewport", scrollObj.transform);
        SetRect(viewport, new Vector2(0, 0), new Vector2(1, 1));
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);

        GameObject content = CreateUIObject("Content", viewport.transform);
        bottomListContent = content.GetComponent<RectTransform>();
        bottomListContent.anchorMin = new Vector2(0, 1);
        bottomListContent.anchorMax = new Vector2(1, 1);
        bottomListContent.pivot = new Vector2(0.5f, 1);
        bottomListContent.anchoredPosition = Vector2.zero;
        bottomListContent.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(242, 120);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.spacing = new Vector2(14, 14);
        grid.padding = new RectOffset(14, 14, 14, 14);
        grid.childAlignment = TextAnchor.UpperLeft;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bottomScrollRect.content = bottomListContent;
        bottomScrollRect.viewport = viewport.GetComponent<RectTransform>();
    }

    private void CreateDetailsPanel()
    {
        detailsPanel = CreateUIObject("DetailsPanel", rootRect);
        SetRect(detailsPanel, new Vector2(0.10f, 0.10f), new Vector2(0.84f, 0.78f));
        Image bg = detailsPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.05f, 0.10f, 0.96f);
        bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;
        detailsPanel.SetActive(false);

        HorizontalLayoutGroup hlg = detailsPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 18;
        hlg.padding = new RectOffset(22, 22, 22, 22);
        hlg.childAlignment = TextAnchor.UpperLeft;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = false;

        GameObject leftColumn = CreateUIObject("DetailsLeft", detailsPanel.transform);
        RectTransform leftRect = leftColumn.GetComponent<RectTransform>();
        leftRect.sizeDelta = new Vector2(400, 0);
        detailsCover = CreateRawImage(leftColumn.transform, "CoverImage", new Vector2(0, 0));
        detailsCover.rectTransform.sizeDelta = new Vector2(380, 520);
        detailsCover.color = new Color(0.06f, 0.08f, 0.14f, 1f);

        GameObject rightColumn = CreateUIObject("DetailsRight", detailsPanel.transform);
        VerticalLayoutGroup rightVlg = rightColumn.AddComponent<VerticalLayoutGroup>();
        rightVlg.spacing = 12;
        rightVlg.childAlignment = TextAnchor.UpperLeft;
        rightVlg.childForceExpandHeight = false;
        rightVlg.childForceExpandWidth = true;

        detailsName = CreateText(rightColumn.transform, "Marvel's Spider-Man 2 (FPKG Edition)", 30, FontStyle.Bold, TextAnchor.UpperLeft);
        detailsName.color = Color.white;
        detailsTitleId = CreateText(rightColumn.transform, "Serial Number: CUSA30964", 18, FontStyle.Normal, TextAnchor.UpperLeft);
        detailsTitleId.color = new Color(0.78f, 0.86f, 0.96f, 1f);
        detailsVersion = CreateText(rightColumn.transform, "Region: Region 2 (Europe / Middle East)", 18, FontStyle.Normal, TextAnchor.UpperLeft);
        detailsVersion.color = new Color(0.78f, 0.86f, 0.96f, 1f);
        detailsRelease = CreateText(rightColumn.transform, "Total Size: 75.1 GB", 18, FontStyle.Normal, TextAnchor.UpperLeft);
        detailsRelease.color = new Color(0.78f, 0.86f, 0.96f, 1f);
        detailsSize = CreateText(rightColumn.transform, "", 16, FontStyle.Normal, TextAnchor.UpperLeft);
        detailsSize.color = new Color(0.72f, 0.82f, 0.92f, 1f);
        detailsMinFW = CreateText(rightColumn.transform, "", 16, FontStyle.Normal, TextAnchor.UpperLeft);
        detailsMinFW.color = new Color(0.72f, 0.82f, 0.92f, 1f);

        CreateText(rightColumn.transform, "Screenshots", 20, FontStyle.Bold, TextAnchor.UpperLeft).color = Color.white;
        GameObject screenshotRow = CreateUIObject("ScreenshotRow", rightColumn.transform);
        HorizontalLayoutGroup screenshotHlg = screenshotRow.AddComponent<HorizontalLayoutGroup>();
        screenshotHlg.spacing = 12;
        screenshotHlg.childForceExpandHeight = false;
        screenshotHlg.childForceExpandWidth = false;

        for (int i = 0; i < 3; i++)
        {
            RawImage shot = CreateRawImage(screenshotRow.transform, $"Shot{i}", new Vector2(0, 0));
            shot.rectTransform.sizeDelta = new Vector2(160, 90);
            shot.color = new Color(0.08f, 0.10f, 0.18f, 1f);
        }

        GameObject actionRow = CreateUIObject("DetailsActions", rightColumn.transform);
        HorizontalLayoutGroup actionHlg = actionRow.AddComponent<HorizontalLayoutGroup>();
        actionHlg.spacing = 14;
        actionHlg.childAlignment = TextAnchor.MiddleLeft;
        actionHlg.childForceExpandWidth = false;
        actionHlg.childForceExpandHeight = false;

        CreateButton(actionRow.transform, "DownloadDetails", "DOWNLOAD", 0.40f).GetComponent<Button>().onClick.AddListener(OpenDownloadPanel);
        CreateButton(actionRow.transform, "CloseDetails", "CLOSE", 0.28f).GetComponent<Button>().onClick.AddListener(() => detailsPanel.SetActive(false));
    }

    private void CreateDownloadPanel()
    {
        downloadPanel = CreateUIObject("DownloadPanel", rootRect);
        SetRect(downloadPanel, new Vector2(0.14f, 0.14f), new Vector2(0.82f, 0.78f));
        Image bg = downloadPanel.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.05f, 0.10f, 0.98f);
        bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;
        downloadPanel.SetActive(false);

        VerticalLayoutGroup vlg = downloadPanel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        Text header = CreateText(downloadPanel.transform, "CURRENT DOWNLOADS (4)", 24, FontStyle.Bold, TextAnchor.UpperLeft);
        header.color = Color.white;

        GameObject listHolder = CreateUIObject("DownloadListHolder", downloadPanel.transform);
        RectTransform listRect = listHolder.GetComponent<RectTransform>();
        listRect.sizeDelta = new Vector2(0, 340);

        ScrollRect scrollRect = listHolder.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 28f;

        GameObject viewport = CreateUIObject("Viewport", listHolder.transform);
        SetRect(viewport, new Vector2(0, 0), new Vector2(1, 1));
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup itemLayout = content.AddComponent<VerticalLayoutGroup>();
        itemLayout.spacing = 12;
        itemLayout.padding = new RectOffset(0, 0, 0, 0);
        itemLayout.childForceExpandHeight = false;
        itemLayout.childForceExpandWidth = true;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        for (int i = 0; i < 4; i++)
        {
            var item = i < filteredGames.Count ? filteredGames[i] : new KeyValuePair<string, GameContent>("", new GameContent { name = "Waiting...", title_id = "----", size = "0", region = "N/A", version = "--", release = "" });
            CreateDownloadEntry(content.transform, item, i);
        }

        GameObject buttonsRow = CreateUIObject("DownloadButtons", downloadPanel.transform);
        HorizontalLayoutGroup buttonsHlg = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        buttonsHlg.spacing = 14;
        buttonsHlg.childForceExpandWidth = false;
        buttonsHlg.childForceExpandHeight = true;

        CreateButton(buttonsRow.transform, "CloseDownloads", "CLOSE", 0.22f).GetComponent<Button>().onClick.AddListener(() => downloadPanel.SetActive(false));
    }

    private void CreateDownloadEntry(Transform parent, KeyValuePair<string, GameContent> item, int index)
    {
        GameObject row = CreateUIObject($"DownloadEntry_{index}", parent);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0, 96);

        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.12f, 0.20f, 0.96f);

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.padding = new RectOffset(12, 12, 12, 12);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = false;

        RawImage icon = CreateRawImage(row.transform, "DownloadIcon", new Vector2(0, 0));
        icon.rectTransform.sizeDelta = new Vector2(84, 84);
        icon.color = new Color(0.12f, 0.16f, 0.24f, 1f);

        GameObject info = CreateUIObject("DownloadInfo", row.transform);
        VerticalLayoutGroup vlg = info.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        Text nameText = CreateText(info.transform, item.Value.name, 18, FontStyle.Bold, TextAnchor.UpperLeft);
        nameText.color = Color.white;
        Text statusText = CreateText(info.transform, GetDownloadStatus(index), 14, FontStyle.Normal, TextAnchor.UpperLeft);
        statusText.color = new Color(0.76f, 0.84f, 0.95f, 1f);

        CreateProgressBar(info.transform, GetDownloadProgress(index));
        Text metricsText = CreateText(info.transform, GetDownloadMetrics(index), 14, FontStyle.Normal, TextAnchor.UpperLeft);
        metricsText.color = new Color(0.72f, 0.82f, 0.92f, 1f);
    }

    private string GetDownloadStatus(int index)
    {
        switch (index)
        {
            case 0: return "45%   Downloading";
            case 1: return "80%   Paused";
            case 2: return "43%   Downloading";
            case 3: return "50% (Frozen)";
            default: return "Pending";
        }
    }

    private float GetDownloadProgress(int index)
    {
        switch (index)
        {
            case 0: return 0.45f;
            case 1: return 0.80f;
            case 2: return 0.43f;
            case 3: return 0.50f;
            default: return 0f;
        }
    }

    private string GetDownloadMetrics(int index)
    {
        switch (index)
        {
            case 0: return "2.1 MB/s   4.5 GB / 10.0 GB   2h 15m";
            case 1: return "-- MB/s (Paused)   20.0 GB / 25.0 GB   4h 00m";
            case 2: return "12.5 MB/s   1.2 GB / 2.8 GB   45m";
            case 3: return "0.0 MB/s (Cancelled)   0.5 GB / 1.0 GB   30m";
            default: return string.Empty;
        }
    }

    private void CreateSidebarMenu()
    {
        GameObject sidebarObj = CreateUIObject("SidebarMenu", rootRect);
        SetRect(sidebarObj, new Vector2(0.82f, 0.10f), new Vector2(0.98f, 0.92f));
        Image bgImage = sidebarObj.AddComponent<Image>();
        bgImage.color = new Color(0.03f, 0.08f, 0.14f, 0.95f);
        bgImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        bgImage.type = Image.Type.Sliced;

        ScrollRect scrollRect = sidebarObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = true;
        scrollRect.scrollSensitivity = 28f;

        GameObject viewport = CreateUIObject("Viewport", sidebarObj.transform);
        SetRect(viewport, new Vector2(0, 0), new Vector2(1, 1));
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 14, 14);
        vlg.spacing = 10;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();

        CreateSidebarSection(content.transform, "SIDEBAR MENU", true);
        CreateSidebarOption(content.transform, "SORTING OPTIONS", true);
        CreateSidebarOption(content.transform, "Size", false);
        CreateSidebarOption(content.transform, "Region", false);
        CreateSidebarOption(content.transform, "^ Name", true);
        CreateSidebarOption(content.transform, "Title ID", false);

        CreateSidebarSection(content.transform, "CONTENT TYPES", false);
        CreateSidebarToggle(content.transform, "> Homebrew", true);

        CreateSidebarSection(content.transform, "REGIONS", false);
        CreateSidebarToggle(content.transform, "x USA", true);
        CreateSidebarToggle(content.transform, "x Europe", false);
        CreateSidebarToggle(content.transform, "x Japan", false);
        CreateSidebarToggle(content.transform, "x Asia", true);

        CreateSidebarSection(content.transform, "USER PREFERENCES", false);
        CreateSidebarToggle(content.transform, "Direct Download", true);
        CreateSidebarToggle(content.transform, "Install Once Done", true);
        CreateSidebarToggle(content.transform, "Delete After Install", false);
        CreateSidebarToggle(content.transform, "Delete On Cancel", false);
        CreateSidebarToggle(content.transform, "Background Music", true);

        CreateSidebarOption(content.transform, "o Enable App Updates", false);
        CreateSidebarOption(content.transform, "o Populate via Web", false);
        CreateSidebarButton(content.transform, "Change Background", 1f);
        CreateSidebarButton(content.transform, "Change Save Path", 1f);
        CreateSidebarButton(content.transform, "Reload JSON Files", 1f);
    }

    private void CreateSidebarSection(Transform parent, string title, bool hasIcon)
    {
        GameObject section = CreateUIObject("Section_" + title, parent);
        RectTransform rect = section.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 28);

        HorizontalLayoutGroup hlg = section.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(8, 8, 4, 4);
        hlg.spacing = 8;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        if (hasIcon)
        {
            Text icon = CreateText(section.transform, "△", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            LayoutElement iconLE = icon.gameObject.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 20;
            iconLE.preferredHeight = 24;
        }

        Text titleText = CreateText(section.transform, title, 14, FontStyle.Bold, TextAnchor.MiddleLeft);
        titleText.color = new Color(0.9f, 0.9f, 0.95f, 1f);
    }

    private void CreateSidebarOption(Transform parent, string text, bool emphasized)
    {
        GameObject option = CreateUIObject("Option_" + text, parent);
        RectTransform rect = option.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 26);

        HorizontalLayoutGroup hlg = option.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 6, 4, 4);
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth = true;

        Image bg = option.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        Button button = option.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.05f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.10f);
        button.colors = colors;

        Text optionText = CreateText(option.transform, text, 12, emphasized ? FontStyle.Bold : FontStyle.Normal, TextAnchor.MiddleLeft);
        optionText.color = emphasized ? Color.white : new Color(0.8f, 0.84f, 0.92f, 1f);

        if (text == "Size")
            button.onClick.AddListener(() => SetSort(SortBy.Size));
        else if (text == "Region")
            button.onClick.AddListener(() => SetSort(SortBy.Region));
        else if (text == "^ Name")
            button.onClick.AddListener(() => SetSort(SortBy.Name));
        else if (text == "Title ID")
            button.onClick.AddListener(() => SetSort(SortBy.TitleID));
        else if (text.Contains("Enable App Updates"))
            button.onClick.AddListener(() => ToggleAppUpdates(optionText));
        else if (text.Contains("Populate via Web"))
            button.onClick.AddListener(() => TogglePopulateViaWeb(optionText));
    }

    private void CreateSidebarToggle(Transform parent, string text, bool isOn)
    {
        GameObject toggle = CreateUIObject("Toggle_" + text, parent);
        RectTransform rect = toggle.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 26);

        HorizontalLayoutGroup hlg = toggle.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 6, 4, 4);
        hlg.spacing = 8;
        hlg.childForceExpandHeight = true;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        Image bg = toggle.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        Button button = toggle.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.05f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.10f);
        button.colors = colors;

        Text toggleText = CreateText(toggle.transform, text, 12, FontStyle.Normal, TextAnchor.MiddleLeft);
        toggleText.color = new Color(0.84f, 0.88f, 0.96f, 1f);

        GameObject toggleBtn = CreateUIObject("ToggleBtn", toggle.transform);
        RectTransform toggleRect = toggleBtn.GetComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(32, 18);
        Image toggleBg = toggleBtn.AddComponent<Image>();
        toggleBg.color = isOn ? new Color(0.22f, 0.62f, 1f, 1f) : new Color(0.4f, 0.4f, 0.48f, 1f);

        sidebarToggleStates[text] = isOn;
        button.onClick.AddListener(() => ToggleSidebarState(text, toggleText, toggleBg));

        HandleToggleAction(text, isOn);
    }

    private void CreateSidebarButton(Transform parent, string text, float heightPercent)
    {
        GameObject button = CreateUIObject("SideBtn_" + text, parent);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 32);

        Image bg = button.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.14f, 0.22f, 0.95f);

        Button btn = button.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.normalColor = bg.color;
        colors.highlightedColor = new Color(0.18f, 0.22f, 0.32f, 1f);
        colors.pressedColor = new Color(0.08f, 0.10f, 0.18f, 1f);
        btn.colors = colors;

        LayoutElement le = button.AddComponent<LayoutElement>();
        le.preferredHeight = 32;

        Text btnText = CreateText(button.transform, text, 12, FontStyle.Normal, TextAnchor.MiddleCenter);
        btnText.color = new Color(0.78f, 0.84f, 0.94f, 1f);

        if (text.Contains("Change Background"))
            btn.onClick.AddListener(CycleBackground);
        else if (text.Contains("Change Save Path"))
            btn.onClick.AddListener(CycleSavePath);
        else if (text.Contains("Reload JSON Files"))
            btn.onClick.AddListener(ReloadJsonFiles);
    }

    private void CreateFooterBar()
    {
        GameObject footerObj = CreateUIObject("FooterBar", rootRect);
        SetRect(footerObj, new Vector2(0, 0), new Vector2(1, 0.06f));
        Image bg = footerObj.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.10f, 0.18f, 0.96f);

        HorizontalLayoutGroup hlg = footerObj.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = 18;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;

        CreateFooterLabel(footerObj.transform, "✕ DOWNLOAD", 14);
        CreateFooterLabel(footerObj.transform, "▤ VIEW DOWNLOADS", 14);
        CreateFooterLabel(footerObj.transform, "△ DETAILS", 14);
        CreateFooterLabel(footerObj.transform, "L1 MENU", 14);
        CreateFooterLabel(footerObj.transform, "⊘ EXIT", 14);

        GameObject spacer = new GameObject("FooterSpacer");
        spacer.transform.SetParent(footerObj.transform, false);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

        footerContentInfoText = CreateText(footerObj.transform, "CONTENT: 0 / [0]   FREE SPACE: N/A", 14, FontStyle.Normal, TextAnchor.MiddleRight);
        footerContentInfoText.color = new Color(0.76f, 0.84f, 0.92f, 1f);
        footerContentInfoText.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 24);
    }

    private void CreateFooterLabel(Transform parent, string label, int size)
    {
        bool isInteractive = label.Contains("DOWNLOAD") || label.Contains("VIEW DOWNLOADS") || label.Contains("EXIT");
        if (!isInteractive)
        {
            Text text = CreateText(parent, label, size, FontStyle.Normal, TextAnchor.MiddleCenter);
            text.color = new Color(0.82f, 0.88f, 0.96f, 1f);
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(140, 24);
            return;
        }

        GameObject button = CreateUIObject("FooterButton_" + label, parent);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140, 24);

        Image bg = button.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0f);

        Button btn = button.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        var colors = btn.colors;
        colors.normalColor = new Color(0f, 0f, 0f, 0f);
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.08f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.12f);
        btn.colors = colors;

        Text text = CreateText(button.transform, label, size, FontStyle.Normal, TextAnchor.MiddleCenter);
        text.color = new Color(0.82f, 0.88f, 0.96f, 1f);

        if (label.Contains("VIEW DOWNLOADS") || label.Contains("DOWNLOAD"))
            btn.onClick.AddListener(OpenDownloadPanel);
        else if (label.Contains("EXIT"))
            btn.onClick.AddListener(() => Application.Quit());
    }

    private void RefreshUI()
    {
        ApplySearch(currentSearch);
    }

    private void ApplySearch(string query)
    {
        currentSearch = query ?? string.Empty;
        IEnumerable<KeyValuePair<string, GameContent>> source = allGames;

        if (selectedContentType != ContentType.ALL)
        {
            if (selectedContentType == ContentType.Homebrew)
                source = ContentHandler.GetHomebrewCombinedCache().ToList();
            else if (ContentHandler.contentTypeCache.ContainsKey(selectedContentType) && ContentHandler.contentTypeCache[selectedContentType] != null)
                source = ContentHandler.contentTypeCache[selectedContentType].ToList();
        }

        if (selectedRegions.Any())
        {
            source = source.Where(item => string.IsNullOrEmpty(item.Value.region)
                || item.Value.region.Equals("ALL", StringComparison.OrdinalIgnoreCase)
                || selectedRegions.Contains(item.Value.region, StringComparer.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(currentSearch))
        {
            string filter = currentSearch.ToLowerInvariant();
            source = source.Where(item => (item.Value.name != null && item.Value.name.ToLowerInvariant().Contains(filter))
                || (item.Value.title_id != null && item.Value.title_id.ToLowerInvariant().Contains(filter)));
        }

        filteredGames = GetSortedList(source.ToList());
        PopulateTopCarousel();
        PopulateBottomList();
        UpdateSortStatus();
        UpdateFooterInfo();
    }

    private void PopulateTopCarousel()
    {
        foreach (Transform child in topCarouselRect) Destroy(child.gameObject);
        var random = new System.Random();
        var featuredItems = filteredGames.OrderBy(_ => random.Next()).Take(6).ToList();
        foreach (var item in featuredItems)
            CreateLargeCard(topCarouselRect, item);
    }

    private void PopulateBottomList()
    {
        foreach (Transform child in bottomListContent) Destroy(child.gameObject);
        foreach (var item in filteredGames)
            CreateSmallCard(bottomListContent, item);

        if (bottomScrollRect != null)
            bottomScrollRect.verticalNormalizedPosition = 1f;
    }

    private void UpdateSortStatus()
    {
        if (sortStatusText == null) return;
        switch (currentSort)
        {
            case SortBy.Name:
                sortStatusText.text = "Sort: Name";
                break;
            case SortBy.Size:
                sortStatusText.text = "Sort: Size";
                break;
            case SortBy.Region:
                sortStatusText.text = "Sort: Region";
                break;
            case SortBy.TitleID:
                sortStatusText.text = "Sort: Title ID";
                break;
            default:
                sortStatusText.text = "Sort: Name";
                break;
        }
    }

    private List<KeyValuePair<string, GameContent>> GetSortedList(List<KeyValuePair<string, GameContent>> input)
    {
        switch (currentSort)
        {
            case SortBy.Size:
                return input.OrderBy(item => ParseSize(item.Value.size)).ToList();
            case SortBy.Region:
                return input.OrderBy(item => item.Value.region).ThenBy(item => item.Value.name).ToList();
            case SortBy.TitleID:
                return input.OrderBy(item => item.Value.title_id).ToList();
            default:
                return input.OrderBy(item => item.Value.name).ToList();
        }
    }

    private void CreateLargeCard(Transform parent, KeyValuePair<string, GameContent> item)
    {
        GameObject card = CreateUIObject("FeaturedCard_" + item.Value.title_id, parent);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(240, 300);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.14f, 0.24f, 0.95f);
        bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;

        Outline outline = card.AddComponent<Outline>();
        outline.effectColor = new Color(0.34f, 0.62f, 1f, 0.32f);
        outline.effectDistance = new Vector2(2, -2);

        Button button = card.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = bg.color;
        cb.highlightedColor = new Color(0.16f, 0.24f, 0.38f, 0.96f);
        cb.pressedColor = new Color(0.07f, 0.12f, 0.22f, 0.96f);
        button.colors = cb;
        button.onClick.AddListener(() => ShowDetails(item));

        VerticalLayoutGroup vlg = card.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        RawImage cover = CreateRawImage(card.transform, "Cover", new Vector2(0, 0));
        cover.rectTransform.sizeDelta = new Vector2(220, 240);
        cover.color = new Color(0.12f, 0.16f, 0.24f, 1f);
        if (!string.IsNullOrEmpty(item.Value.cover_url))
            StartCoroutine(LoadCoverTexture(item.Value.cover_url, cover));

        Text title = CreateText(card.transform, item.Value.name, 18, FontStyle.Bold, TextAnchor.UpperCenter);
        title.color = Color.white;
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 40);

        Text subtitle = CreateText(card.transform, item.Value.title_id, 14, FontStyle.Normal, TextAnchor.UpperCenter);
        subtitle.color = new Color(0.78f, 0.84f, 0.95f, 1f);
    }

    private void CreateSmallCard(Transform parent, KeyValuePair<string, GameContent> item)
    {
        GameObject card = CreateUIObject("ListCard_" + item.Value.title_id, parent);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(242, 130);

        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.10f, 0.18f, 0.96f);
        bg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        bg.type = Image.Type.Sliced;

        Button button = card.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = bg.color;
        cb.highlightedColor = new Color(0.12f, 0.18f, 0.28f, 0.96f);
        cb.pressedColor = new Color(0.04f, 0.08f, 0.14f, 0.96f);
        button.colors = cb;
        button.onClick.AddListener(() => ShowDetails(item));

        HorizontalLayoutGroup hlg = card.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.padding = new RectOffset(10, 10, 10, 10);
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childForceExpandHeight = false;
        hlg.childForceExpandWidth = false;

        RawImage cover = CreateRawImage(card.transform, "Cover", new Vector2(0, 0));
        cover.rectTransform.sizeDelta = new Vector2(92, 92);
        cover.color = new Color(0.12f, 0.16f, 0.22f, 1f);
        if (!string.IsNullOrEmpty(item.Value.cover_url))
            StartCoroutine(LoadCoverTexture(item.Value.cover_url, cover));

        GameObject info = CreateUIObject("Info", card.transform);
        VerticalLayoutGroup vlg = info.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        Text title = CreateText(info.transform, item.Value.name, 16, FontStyle.Bold, TextAnchor.UpperLeft);
        title.color = Color.white;
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 40);

        Text id = CreateText(info.transform, item.Value.title_id, 13, FontStyle.Normal, TextAnchor.UpperLeft);
        id.color = new Color(0.76f, 0.82f, 0.92f, 1f);
        Text size = CreateText(info.transform, FormatSizeLabel(item.Value.size), 13, FontStyle.Normal, TextAnchor.UpperLeft);
        size.color = new Color(0.66f, 0.78f, 0.92f, 1f);
    }

    private void ShowDetails(KeyValuePair<string, GameContent> item)
    {
        if (detailsPanel == null) return;
        detailsPanel.SetActive(true);
        detailsName.text = item.Value.name;
        detailsTitleId.text = $"Title ID: {item.Value.title_id} [{item.Value.region}]";
        detailsVersion.text = $"Version: {item.Value.version}";
        detailsRelease.text = $"Release: {item.Value.release}";
        detailsSize.text = $"Size: {FormatSizeLabel(item.Value.size)}";
        detailsMinFW.text = $"Min FW: {item.Value.min_fw}";
        detailsCover.texture = Texture2D.whiteTexture;
        detailsCover.color = new Color(0.08f, 0.10f, 0.16f, 1f);
        if (!string.IsNullOrEmpty(item.Value.cover_url))
            StartCoroutine(LoadCoverTexture(item.Value.cover_url, detailsCover));
    }

    private void ToggleSidebarState(string label, Text labelText, Image toggleImage)
    {
        bool currentState = sidebarToggleStates.ContainsKey(label) && sidebarToggleStates[label];
        bool nextState = !currentState;
        sidebarToggleStates[label] = nextState;
        toggleImage.color = nextState ? new Color(0.22f, 0.62f, 1f, 1f) : new Color(0.4f, 0.4f, 0.48f, 1f);
        HandleToggleAction(label, nextState, labelText, toggleImage);
    }

    private void HandleToggleAction(string label, bool isOn, Text labelText = null, Image iconImage = null)
    {
        if (label.StartsWith("> Homebrew", StringComparison.OrdinalIgnoreCase))
        {
            if (isOn)
                SetContentFilter(ContentType.Homebrew);
            else
                SetContentFilter(ContentType.ALL);
        }
        else if (label.StartsWith("x USA", StringComparison.OrdinalIgnoreCase))
        {
            ToggleRegion("USA", isOn);
        }
        else if (label.StartsWith("x Europe", StringComparison.OrdinalIgnoreCase))
        {
            ToggleRegion("Europe", isOn);
        }
        else if (label.StartsWith("x Japan", StringComparison.OrdinalIgnoreCase))
        {
            ToggleRegion("Japan", isOn);
        }
        else if (label.StartsWith("x Asia", StringComparison.OrdinalIgnoreCase))
        {
            ToggleRegion("Asia", isOn);
        }
        else if (label.StartsWith("Direct Download", StringComparison.OrdinalIgnoreCase))
        {
            Variables.directDownload = isOn;
        }
        else if (label.StartsWith("Install Once Done", StringComparison.OrdinalIgnoreCase))
        {
            Variables.installAfter = isOn;
        }
        else if (label.StartsWith("Delete After Install", StringComparison.OrdinalIgnoreCase))
        {
            Variables.deleteAfter = isOn;
        }
        else if (label.StartsWith("Delete On Cancel", StringComparison.OrdinalIgnoreCase))
        {
            Variables.deleteOnCancel = isOn;
        }
        else if (label.StartsWith("Background Music", StringComparison.OrdinalIgnoreCase))
        {
            Variables.backgroundMusic = isOn;
        }

        if (labelText != null)
            labelText.color = isOn ? Color.white : new Color(0.84f, 0.88f, 0.96f, 1f);

        UpdateFooterInfo();
    }

    private void ToggleRegion(string region, bool isOn)
    {
        if (isOn)
            selectedRegions.Add(region);
        else
            selectedRegions.Remove(region);

        ApplyFilters();
    }

    private void SetContentFilter(ContentType contentType)
    {
        selectedContentType = contentType;
        ApplyFilters();
    }

    private void UpdateFooterInfo()
    {
        if (footerContentInfoText == null)
            return;

        int contentCount = filteredGames?.Count ?? 0;
        footerContentInfoText.text = $"CONTENT: {contentCount} / [{allGames.Count}]   FREE SPACE: N/A";
    }

    private void ToggleAppUpdates(Text labelText)
    {
        enableAppUpdates = !enableAppUpdates;
        if (labelText != null)
            labelText.text = enableAppUpdates ? "o Enable App Updates" : "x Enable App Updates";
    }

    private void TogglePopulateViaWeb(Text labelText)
    {
        populateViaWeb = !populateViaWeb;
        if (labelText != null)
            labelText.text = populateViaWeb ? "o Populate via Web" : "x Populate via Web";
    }

    private void CycleBackground()
    {
        string backgroundDirectory = Path.Combine(Application.dataPath, "..", "DATA", "Backgrounds");
        if (!Directory.Exists(backgroundDirectory))
            return;

        var files = Directory.GetFiles(backgroundDirectory)
            .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (files.Length == 0)
            return;

        currentBackgroundIndex = (currentBackgroundIndex + 1) % files.Length;
        string selectedPath = files[currentBackgroundIndex];
        StartCoroutine(LoadBackgroundTexture(selectedPath));
    }

    private IEnumerator LoadBackgroundTexture(string filePath)
    {
        if (!File.Exists(filePath))
            yield break;

        byte[] imageData = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageData))
        {
            var background = rootRect.Find("Background")?.GetComponent<Image>();
            if (background != null)
            {
                background.color = Color.white;
                background.material = null;
                background.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }
    }

    private void CycleSavePath()
    {
        if (string.IsNullOrEmpty(alternateDownloadPath))
            alternateDownloadPath = Path.Combine(Application.dataPath, "..", "DATA", "ExternalDownloads");

        if (Variables.downloadPath == alternateDownloadPath)
            Variables.downloadPath = Path.Combine(Application.dataPath, "..", "DATA", "Downloads");
        else
            Variables.downloadPath = alternateDownloadPath;

        Variables.downloadPath = Path.GetFullPath(Variables.downloadPath).Replace("\\", "/");
        if (!Variables.downloadPath.EndsWith("/"))
            Variables.downloadPath += "/";
    }

    private void ReloadJsonFiles()
    {
        if (ContentHandler.allContentCache != null)
            ContentHandler.allContentCache.Clear();

        ContentHandler.UpdatePkgCount();
        StartCoroutine(WaitForReload());
    }

    private IEnumerator WaitForReload()
    {
        yield return new WaitForSeconds(0.5f);
        LoadContentData();
        RefreshUI();
    }

    private void ApplyFilters()
    {
        ApplySearch(currentSearch);
    }

    private void OpenDownloadPanel()
    {
        if (downloadPanel == null) return;
        downloadPanel.SetActive(true);
        if (downloadProgressBar != null)
            downloadProgressBar.value = 0.01f;
    }

    private void ToggleSortPanel()
    {
        if (sortPanel == null) return;
        sortPanel.SetActive(!sortPanel.activeSelf);
    }

    private void SetSort(SortBy sort)
    {
        currentSort = sort;
        ApplySearch(currentSearch);
        if (sortPanel != null)
            sortPanel.SetActive(false);
    }

    private void CreateSortOption(string text, Transform parent, SortBy sort)
    {
        GameObject button = CreateButton(parent, text, text, 1f);
        button.GetComponent<Button>().onClick.AddListener(() => SetSort(sort));
    }

    private string FormatSizeLabel(string bytesText)
    {
        if (string.IsNullOrEmpty(bytesText))
            return "Unknown";

        if (long.TryParse(bytesText, out long bytes))
        {
            if (bytes > 1024 * 1024 * 1024)
                return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
            if (bytes > 1024 * 1024)
                return $"{bytes / (1024f * 1024f):F2} MB";
            if (bytes > 1024)
                return $"{bytes / 1024f:F2} KB";
            return $"{bytes} B";
        }

        return bytesText;
    }

    private GameObject CreateSortPanel(Transform parent)
    {
        GameObject panel = CreateUIObject("SortPanel", parent);
        SetRect(panel, new Vector2(0.04f, 0.50f), new Vector2(0.38f, 0.57f));
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.9f);
        panel.SetActive(false);

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(14, 14, 14, 14);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        CreateSortOption("Name", panel.transform, SortBy.Name);
        CreateSortOption("Size", panel.transform, SortBy.Size);
        CreateSortOption("Region", panel.transform, SortBy.Region);
        CreateSortOption("Title ID", panel.transform, SortBy.TitleID);

        return panel;
    }

    private GameObject CreateButton(Transform parent, string name, string label, float widthPercent)
    {
        GameObject buttonObj = CreateUIObject(name, parent);
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180 * widthPercent, 42);

        Image img = buttonObj.AddComponent<Image>();
        img.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = new Color(0.12f, 0.52f, 0.98f, 1f);

        Button btn = buttonObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = img.color;
        colors.highlightedColor = new Color(0.18f, 0.66f, 1f, 1f);
        colors.pressedColor = new Color(0.08f, 0.32f, 0.68f, 1f);
        btn.colors = colors;

        Text text = CreateText(buttonObj.transform, label, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = Color.white;
        return buttonObj;
    }

    private InputField CreateInputField(Transform parent, string placeholder, float widthPercent)
    {
        GameObject inputObject = CreateUIObject("SearchInput", parent);
        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(860 * widthPercent, 48);

        Image background = inputObject.AddComponent<Image>();
        background.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        background.type = Image.Type.Sliced;
        background.color = new Color(0.08f, 0.10f, 0.18f, 1f);

        InputField inputField = inputObject.AddComponent<InputField>();
        Text textComponent = CreateText(inputObject.transform, string.Empty, 18, FontStyle.Normal, TextAnchor.MiddleLeft);
        textComponent.color = Color.white;
        textComponent.GetComponent<RectTransform>().offsetMin = new Vector2(12, 0);
        inputField.textComponent = textComponent;

        Text placeholderText = CreateText(inputObject.transform, placeholder, 18, FontStyle.Italic, TextAnchor.MiddleLeft);
        placeholderText.color = new Color(0.65f, 0.70f, 0.78f, 0.8f);
        placeholderText.GetComponent<RectTransform>().offsetMin = new Vector2(12, 0);
        inputField.placeholder = placeholderText;

        inputField.lineType = InputField.LineType.SingleLine;
        inputField.onValueChanged.AddListener(ApplySearch);

        return inputField;
    }

    private Slider CreateProgressBar(Transform parent, float value)
    {
        GameObject sliderObj = CreateUIObject("ProgressBar", parent);
        RectTransform rect = sliderObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 18);

        Image background = sliderObj.AddComponent<Image>();
        background.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
        background.type = Image.Type.Sliced;
        background.color = new Color(0.1f, 0.14f, 0.22f, 1f);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;
        slider.interactable = false;

        GameObject fillArea = CreateUIObject("FillArea", sliderObj.transform);
        SetRect(fillArea, new Vector2(0, 0), new Vector2(1, 1));
        Image fillImage = fillArea.AddComponent<Image>();
        fillImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
        fillImage.type = Image.Type.Sliced;
        fillImage.color = new Color(0.22f, 0.60f, 0.96f, 1f);

        slider.targetGraphic = fillImage;
        slider.fillRect = fillImage.rectTransform;

        return slider;
    }

    private RawImage CreateRawImage(Transform parent, string name, Vector2 sizePercent)
    {
        GameObject rawObj = CreateUIObject(name, parent);
        RectTransform rect = rawObj.GetComponent<RectTransform>();
        rect.sizeDelta = Vector2.zero;
        RawImage rawImage = rawObj.AddComponent<RawImage>();
        rawImage.color = new Color(0.08f, 0.10f, 0.16f, 1f);
        return rawImage;
    }

    private Text CreateText(Transform parent, string text, int size, FontStyle style, TextAnchor alignment)
    {
        GameObject textObj = CreateUIObject("Text", parent);
        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 28);

        Text uiText = textObj.AddComponent<Text>();
        uiText.text = text;
        uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        uiText.fontSize = size;
        uiText.fontStyle = style;
        uiText.color = Color.white;
        uiText.alignment = alignment;
        uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
        return uiText;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.localPosition = Vector3.zero;
        return go;
    }

    private void SetRect(GameObject obj, Vector2 min, Vector2 max)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private float ParseSize(string sizeText)
    {
        if (string.IsNullOrEmpty(sizeText))
            return 0f;

        if (long.TryParse(sizeText, out long bytes))
            return bytes;

        string numeric = new string(sizeText.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (float.TryParse(numeric, out float result))
            return result;

        return 0f;
    }

    private IEnumerator LoadCoverTexture(string url, RawImage image)
    {
        if (string.IsNullOrEmpty(url) || image == null)
            yield break;

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (!request.isNetworkError && !request.isHttpError)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            image.texture = texture;
        }

        request.Dispose();
    }
}
