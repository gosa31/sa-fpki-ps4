﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static JsonData;

public class ModernUIManager : MonoBehaviour
{
    [Header("Prefabs & Containers")]
    public GameObject gameCardPrefab;
    public RectTransform topCarouselContainer;
    public RectTransform bottomListContainer;
    public ScrollRect bottomScrollRect;

    [Header("Search & Sort")]
    public InputField searchInput;
    public Button triangleButton;
    public GameObject sortPanel;
    public Button sortByNameButton;
    public Button sortBySizeButton;
    public Button sortByReleaseButton;
    public Button sortByRegionButton;
    public Text sortStatusText;

    [Header("Details Panel")]
    public GameObject detailsPanel;
    public RawImage detailsCover;
    public Text detailsName;
    public Text detailsTitleId;
    public Text detailsVersion;
    public Text detailsRelease;
    public Text detailsSize;
    public Text detailsMinFW;
    public Button detailsDownloadButton;
    public Button detailsCloseButton;

    [Header("Download Panel")]
    public GameObject downloadPanel;
    public Text downloadHeaderText;
    public Text downloadDownloadedText;
    public Text downloadPausedText;
    public Text downloadCancelledText;
    public Slider downloadProgressBar;
    public Button downloadCloseButton;

    private List<KeyValuePair<string, GameContent>> allGames = new List<KeyValuePair<string, GameContent>>();
    private List<KeyValuePair<string, GameContent>> filteredGames = new List<KeyValuePair<string, GameContent>>();
    private KeyValuePair<string, GameContent> currentSelected;
    private SortBy currentSort = SortBy.Name;

    private void Awake()
    {
        if (triangleButton != null)
            triangleButton.onClick.AddListener(ToggleSortPanel);

        if (sortByNameButton != null)
            sortByNameButton.onClick.AddListener(() => SetSort(SortBy.Name));

        if (sortBySizeButton != null)
            sortBySizeButton.onClick.AddListener(() => SetSort(SortBy.Size));

        if (sortByReleaseButton != null)
            sortByReleaseButton.onClick.AddListener(() => SetSort(SortBy.TitleID));

        if (sortByRegionButton != null)
            sortByRegionButton.onClick.AddListener(() => SetSort(SortBy.Region));

        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchChanged);

        if (detailsCloseButton != null)
            detailsCloseButton.onClick.AddListener(() => detailsPanel.SetActive(false));

        if (downloadCloseButton != null)
            downloadCloseButton.onClick.AddListener(() => downloadPanel.SetActive(false));

        if (detailsDownloadButton != null)
            detailsDownloadButton.onClick.AddListener(OpenDownloadPanel);

        if (sortPanel != null)
            sortPanel.SetActive(false);

        if (detailsPanel != null)
            detailsPanel.SetActive(false);

        if (downloadPanel != null)
            downloadPanel.SetActive(false);
    }

    private IEnumerator Start()
    {
        yield return WaitForContentData();
        LoadGameData();
        RefreshUI();
    }

    private IEnumerator WaitForContentData()
    {
        while (ContentHandler.allContentCache == null || ContentHandler.allContentCache.Count == 0)
        {
            yield return null;
        }
    }

    private void LoadGameData()
    {
        allGames = ContentHandler.allContentCache.ToList();
        filteredGames = new List<KeyValuePair<string, GameContent>>(allGames);
    }

    private void RefreshUI()
    {
        ApplySearchFilter(searchInput != null ? searchInput.text : string.Empty);
        PopulateTopCarousel();
        PopulateBottomList();
        UpdateSortText();
    }

    private void ApplySearchFilter(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            filteredGames = GetSortedList(allGames);
            return;
        }

        query = query.Trim().ToLowerInvariant();
        filteredGames = allGames
            .Where(item => (item.Value.name != null && item.Value.name.ToLowerInvariant().Contains(query))
                        || (item.Value.title_id != null && item.Value.title_id.ToLowerInvariant().Contains(query)))
            .ToList();

        filteredGames = GetSortedList(filteredGames);
    }

    private void PopulateTopCarousel()
    {
        if (topCarouselContainer == null || gameCardPrefab == null)
            return;

        ClearChildren(topCarouselContainer);

        var random = new System.Random();
        var pool = filteredGames.OrderBy(_ => random.Next()).Take(6).ToList();

        foreach (var item in pool)
        {
            var instance = Instantiate(gameCardPrefab, topCarouselContainer);
            var card = instance.GetComponent<GameCardUI>();
            if (card != null)
                card.Setup(item, ShowGameDetails, true);
        }
    }

    private void PopulateBottomList()
    {
        if (bottomListContainer == null || gameCardPrefab == null)
            return;

        ClearChildren(bottomListContainer);

        foreach (var item in filteredGames)
        {
            var instance = Instantiate(gameCardPrefab, bottomListContainer);
            var card = instance.GetComponent<GameCardUI>();
            if (card != null)
                card.Setup(item, ShowGameDetails, false);
        }

        if (bottomScrollRect != null)
            bottomScrollRect.verticalNormalizedPosition = 1f;
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
                Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void OnSearchChanged(string query)
    {
        ApplySearchFilter(query);
        PopulateBottomList();
    }

    private void ToggleSortPanel()
    {
        if (sortPanel == null) return;
        sortPanel.SetActive(!sortPanel.activeSelf);
    }

    private void SetSort(SortBy sort)
    {
        currentSort = sort;
        filteredGames = GetSortedList(filteredGames);
        PopulateBottomList();
        UpdateSortText();
        if (sortPanel != null)
            sortPanel.SetActive(false);
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
            case SortBy.Name:
            default:
                return input.OrderBy(item => item.Value.name).ToList();
        }
    }

    private void UpdateSortText()
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

    private float ParseSize(string sizeText)
    {
        if (string.IsNullOrEmpty(sizeText))
            return 0f;

        if (long.TryParse(sizeText, out long bytes))
            return bytes;

        var numeric = new string(sizeText.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (float.TryParse(numeric, out float result))
            return result;

        return 0f;
    }

    private void ShowGameDetails(KeyValuePair<string, GameContent> item)
    {
        currentSelected = item;
        if (detailsPanel != null)
            detailsPanel.SetActive(true);

        if (detailsName != null)
            detailsName.text = item.Value.name;

        if (detailsTitleId != null)
            detailsTitleId.text = $"Title ID: {item.Value.title_id} [{item.Value.region}]";

        if (detailsVersion != null)
            detailsVersion.text = $"Version: {item.Value.version}";

        if (detailsRelease != null)
            detailsRelease.text = $"Release: {item.Value.release}";

        if (detailsSize != null)
            detailsSize.text = $"Size: {FormatSizeLabel(item.Value.size)}";

        if (detailsMinFW != null)
            detailsMinFW.text = $"Min FW: {item.Value.min_fw}";

        if (detailsCover != null)
        {
            detailsCover.texture = null;
            if (!string.IsNullOrEmpty(item.Value.cover_url))
                StartCoroutine(LoadCoverTexture(item.Value.cover_url, detailsCover));
        }
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

    private void OpenDownloadPanel()
    {
        if (downloadPanel != null)
            downloadPanel.SetActive(true);

        if (downloadHeaderText != null)
            downloadHeaderText.text = $"Download status for: {currentSelected.Value.name}";

        if (downloadDownloadedText != null)
            downloadDownloadedText.text = "Downloaded: 1 / 3 files";

        if (downloadPausedText != null)
            downloadPausedText.text = "Paused: 0";

        if (downloadCancelledText != null)
            downloadCancelledText.text = "Canceled: 0";

        if (downloadProgressBar != null)
            downloadProgressBar.value = 0.32f;
    }

    private IEnumerator LoadCoverTexture(string url, RawImage targetImage)
    {
        if (string.IsNullOrEmpty(url) || targetImage == null)
            yield break;

        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (!request.isNetworkError && !request.isHttpError)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            targetImage.texture = texture;
        }

        request.Dispose();
    }

    public void LoadFeaturedGames()
    {
        PopulateTopCarousel();
    }
}
