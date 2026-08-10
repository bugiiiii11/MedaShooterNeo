using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Cryptomeda.Minigames.BackendComs;
using Determinism;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Builds the Phase 3 level-select + Daily Challenge buttons in the inventory
/// scene AT RUNTIME, by cloning the existing startgame button -- zero scene
/// YAML edits. The scene owns nothing new: clones die with the scene and the
/// sceneLoaded handler rebuilds them on every visit, so retry/exit loops stay
/// naturally idempotent.
///
/// Interaction model: the three L1/L2/L3 buttons are SELECTORS (sticky pref +
/// highlight, no scene load) and the ORIGINAL startgame button launches the
/// selected level through its untouched serialized path. Only DAILY launches
/// directly (one-shot flag consumed by the gameplay scene).
///
/// Fail-open everywhere: no startgame button -> log and bail (the game
/// degrades to exactly the pre-Phase-3 flow); daily-state fetch failure ->
/// button stays ENABLED, because the server's 409 at /run/start is the real
/// one-attempt gate, not this UI.
/// </summary>
public static class MsModeSelectBootstrap
{
    private const string InventorySceneName = "inventory";
    private const string SourceButtonName = "startgame";
    private const int GameplaySceneIndex = 3;

    // Layout in canvas units, same parent as the source button. The source
    // sits at (381.6, -506) sized 577x178.
    //
    // The selectors live in the column ABOVE Daily, not above START: the
    // equipped weapon/hero icons occupy the space over START and, being in a
    // later-drawn hierarchy, swallowed the clicks -- the buttons rendered as
    // tabs peeking out from under the icons and hit-tested against them.
    //
    // Spacing MUST exceed SelectorSize.x. At 150 apart with 170 width the three
    // overlapped by 20 on each seam, and uGUI gives an overlap to the topmost
    // sibling -- that is what made a click on L1 select L2.
    private static readonly Vector2[] SelectorPositions =
    {
        new Vector2(-300f, -360f),
        new Vector2(-140f, -360f),
        new Vector2(20f, -360f),
    };
    private static readonly Vector2 SelectorSize = new Vector2(140f, 70f);
    private static readonly Vector2 DailyPosition = new Vector2(-100f, -506f);
    private static readonly Vector2 DailySize = new Vector2(400f, 150f);

    private static readonly Color SelectedTint = Color.white;
    private static readonly Color UnselectedTint = new Color(0.45f, 0.5f, 0.55f, 0.9f);

    private static readonly List<Button> selectorButtons = new List<Button>();
    private static Button dailyButton;
    private static TextMeshProUGUI dailyLabel;
    private static MsModeSelectRunner coroutineRunner;

    [Serializable]
    private class DailyStateResponse
    {
        // field names match the backend JSON exactly (JsonUtility)
        public string date_key;
        public bool attempted;
        public int level;
        public string resets_at;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Hook()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (scene.name != InventorySceneName)
            return;

        try
        {
            Build();
        }
        catch (Exception e)
        {
            // A UI nicety must never break the inventory scene.
            Debug.LogWarning($"[MsModeSelect] UI build failed (game continues without it): {e.Message}");
        }
    }

    private static void Build()
    {
        var source = GameObject.Find(SourceButtonName);
        if (source == null || source.GetComponent<Button>() == null)
        {
            Debug.LogWarning("[MsModeSelect] startgame button not found; level select unavailable this visit");
            return;
        }

        selectorButtons.Clear();
        dailyButton = null;
        dailyLabel = null;

        for (var i = 0; i < SelectorPositions.Length; i++)
        {
            var level = i + 1;
            var clone = CloneButton(source, $"ms_level_select_{level}", SelectorPositions[i], SelectorSize, $"L{level}");
            var button = clone.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                MsLevelSelect.SetSelectedLevel(level);
                RefreshSelectorTints();
            });
            selectorButtons.Add(button);
        }

        var daily = CloneButton(source, "ms_daily_challenge", DailyPosition, DailySize,
            $"DAILY L{MsLevelSelect.DailyRotationLevel(DateTime.UtcNow)}");
        dailyButton = daily.GetComponent<Button>();
        dailyLabel = daily.GetComponentInChildren<TextMeshProUGUI>();
        dailyButton.onClick.AddListener(() =>
        {
            MsLevelSelect.ArmDaily();
            SceneManager.LoadScene(GameplaySceneIndex);
        });

        RefreshSelectorTints();

        // A host object for the coroutines; dies with the scene like the clones.
        coroutineRunner = new GameObject("ms_mode_select_runner").AddComponent<MsModeSelectRunner>();
        coroutineRunner.StartCoroutine(FetchDailyStateWhenWalletReady());
    }

    private static GameObject CloneButton(GameObject source, string name, Vector2 anchoredPosition, Vector2 size, string label)
    {
        var clone = UnityEngine.Object.Instantiate(source, source.transform.parent, false);
        clone.name = name;

        var rect = clone.GetComponent<RectTransform>();
        // The source may be anchored to a corner or stretched; pin the clones to
        // a fixed centre anchor so anchoredPosition/sizeDelta mean literal canvas
        // units rather than edge offsets against an inherited anchor rect.
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        // Draw (and therefore hit-test) above the equipped-item icons that share
        // this corner of the inventory.
        rect.SetAsLastSibling();

        // The label child is 656 wide -- wider than the 577 source button, and
        // far wider than a selector. It is a raycast target by default, so it
        // kept catching clicks well outside the visible box and handed them to
        // whichever selector sat on top: a click on L1 selected L2. Only the
        // clone's own image should be hittable, and it matches what is drawn.
        foreach (var graphic in clone.GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.gameObject != clone)
                graphic.raycastTarget = false;
        }

        var rootImage = clone.GetComponent<Image>();
        if (rootImage != null)
            rootImage.raycastTarget = true;

        // The source pulses to draw the eye to START; the clones must not
        // compete with it.
        var pulsation = clone.GetComponent<Pulsation>();
        if (pulsation != null)
            pulsation.enabled = false;

        // The clone carries the source's serialized PersistentCalls --
        // SceneLoader.LoadLevel(3) AND RestfulManager.SetApi("stage-api").
        // They cannot be removed at runtime, only disabled; missing the second
        // one would flip the API environment on every selector click.
        var button = clone.GetComponent<Button>();
        for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        button.onClick.RemoveAllListeners();
        button.interactable = true;

        var text = clone.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = label;
            text.enableAutoSizing = true;
            text.fontSizeMax = 100f;
            text.fontSizeMin = 20f;
        }

        return clone;
    }

    private static void RefreshSelectorTints()
    {
        var selected = MsLevelSelect.SelectedLevel;
        for (var i = 0; i < selectorButtons.Count; i++)
        {
            var image = selectorButtons[i] != null ? selectorButtons[i].GetComponent<Image>() : null;
            if (image != null)
                image.color = (i + 1) == selected ? SelectedTint : UnselectedTint;
        }
    }

    private static IEnumerator FetchDailyStateWhenWalletReady()
    {
        // The wallet arrives asynchronously via JavascriptHook.SetWalletAddress;
        // poll briefly rather than racing it. No wallet (editor practice run)
        // -> the button stays enabled and the run simply plays unanchored.
        var waited = 0f;
        while (waited < 15f)
        {
            var wallet = PlayerProfileInfo.instance ? PlayerProfileInfo.instance.WalletAddress : null;
            if (!string.IsNullOrEmpty(wallet))
            {
                RestfulManager.Post(RestfulEndpoint.DailyState, "{\"address\":\"" + wallet + "\"}", OnDailyState);
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.5f);
            waited += 0.5f;
        }
    }

    private static void OnDailyState(Response response)
    {
        if (dailyButton == null)
            return; // scene already gone

        if (response.Code != 200)
        {
            Debug.Log($"[MsModeSelect] daily/state returned {response.Code} -- daily button stays enabled (server gates the attempt)");
            return;
        }

        // PostCo prefixes the body with "Code: NNN:\n" (same handling as
        // MsRunAnchor).
        var text = response.Text;
        var brace = string.IsNullOrEmpty(text) ? -1 : text.IndexOf('{');
        if (brace < 0)
            return;

        DailyStateResponse parsed;
        try
        {
            parsed = JsonUtility.FromJson<DailyStateResponse>(text.Substring(brace));
        }
        catch (Exception e)
        {
            Debug.Log($"[MsModeSelect] daily/state response unparseable: {e.Message}");
            return;
        }

        if (parsed == null)
            return;

        if (parsed.level >= MsLevelSelect.MinLevel && parsed.level <= MsLevelSelect.MaxLevel && dailyLabel != null)
            dailyLabel.text = $"DAILY L{parsed.level}";

        if (parsed.attempted)
        {
            dailyButton.interactable = false;

            if (coroutineRunner != null)
                coroutineRunner.StartCoroutine(CountdownUntilReset(ParseResetsAt(parsed.resets_at)));
        }
    }

    private static DateTime ParseResetsAt(string resetsAt)
    {
        if (!string.IsNullOrEmpty(resetsAt)
            && DateTime.TryParse(resetsAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed;
        }

        // fallback: next UTC midnight, computed locally
        return DateTime.UtcNow.Date.AddDays(1);
    }

    private static IEnumerator CountdownUntilReset(DateTime resetsAtUtc)
    {
        var wait = new WaitForSecondsRealtime(1f);
        while (dailyButton != null && dailyLabel != null)
        {
            var remaining = resetsAtUtc - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0)
            {
                // a new UTC day began while the player sat in the inventory --
                // fresh attempt available
                dailyLabel.text = $"DAILY L{MsLevelSelect.DailyRotationLevel(DateTime.UtcNow)}";
                dailyButton.interactable = true;
                yield break;
            }

            dailyLabel.text = $"DONE {(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            yield return wait;
        }
    }

    /// <summary>Empty MonoBehaviour that hosts the bootstrap's coroutines and
    /// dies with the inventory scene.</summary>
    private class MsModeSelectRunner : MonoBehaviour
    {
    }
}
