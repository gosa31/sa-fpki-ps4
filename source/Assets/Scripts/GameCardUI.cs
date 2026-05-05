using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using static JsonData;

public class GameCardUI : MonoBehaviour
{
    public RawImage coverImage;
    public Text titleText;
    public Text subtitleText;
    public Text sizeText;
    public Button cardButton;

    private KeyValuePair<string, GameContent> currentItem;
    private Action<KeyValuePair<string, GameContent>> onSelect;

    public void Setup(KeyValuePair<string, GameContent> item,
                      Action<KeyValuePair<string, GameContent>> selectAction,
                      bool isLargeCard)
    {
        currentItem = item;
        onSelect = selectAction;

        if (titleText != null)
            titleText.text = item.Value.name;

        if (subtitleText != null)
            subtitleText.text = item.Value.title_id;

        if (sizeText != null)
            sizeText.text = FormatSizeLabel(item.Value.size);

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(delegate { if (onSelect != null) onSelect(item); });
        }

        if (coverImage != null)
        {
            coverImage.texture = null;
            if (!string.IsNullOrEmpty(item.Value.cover_url))
                StartCoroutine(LoadCoverTexture(item.Value.cover_url));
        }

        if (isLargeCard)
        {
            if (titleText != null)
                titleText.fontSize = 26;
            if (subtitleText != null)
                subtitleText.fontSize = 18;
        }
    }

    private string FormatSizeLabel(string bytesText)
    {
        if (string.IsNullOrEmpty(bytesText))
            return string.Empty;

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

    private System.Collections.IEnumerator LoadCoverTexture(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (!request.isNetworkError && !request.isHttpError)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            if (coverImage != null)
                coverImage.texture = texture;
        }

        request.Dispose();
    }
}