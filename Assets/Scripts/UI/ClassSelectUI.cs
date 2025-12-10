using System.Collections.Generic;
using ClassSystem.Classes;
using ExitGames.Client.Photon;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight class picker for the lobby. Stores the chosen class in Photon
/// custom properties and exposes helpers for lobby UI to query selection state.
/// </summary>
public class ClassSelectUI : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Exact order of classes as shown by the three buttons.")]
    [SerializeField] private List<PlayerClass> classes = new List<PlayerClass>();

    [Tooltip("Three UI buttons that map to the classes list by index.")]
    [SerializeField] private List<Button> classButtons = new List<Button>();

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI selectionLabel;
    [SerializeField, Tooltip("Lobby MenuUI to refresh player list after selection.")]
    private MenuUI menuUi;

    private int selectedIndex = -1;

    public bool HasSelection => selectedIndex >= 0 && selectedIndex < classes.Count;
    public int SelectedIndex => selectedIndex;
    public PlayerClass SelectedClass => HasSelection ? classes[selectedIndex] : null;
    public int ClassCount => classes.Count;

    private void Awake()
    {
        // Wire up button clicks to selection
        for (int i = 0; i < classButtons.Count; i++)
        {
            int captured = i;
            if (classButtons[captured] != null)
            {
                classButtons[captured].onClick.AddListener(() => Select(captured));
            }
        }
    }

    private void OnEnable()
    {
        // Sync local UI with any existing custom properties (e.g., re-open panel)
        HydrateFromProperties();
        RefreshSelectionLabel();
    }

    /// <summary>
    /// Called by buttons (or code) to choose a class index.
    /// </summary>
    public void Select(int index)
    {
        if (index < 0 || index >= classes.Count)
        {
            Debug.LogWarning($"[ClassSelectUI] Invalid class index {index}.", this);
            return;
        }

        selectedIndex = index;
        SetPhotonClassProperty(index);
        Debug.Log($"[ClassSelectUI] Selected class {index}: {SelectedClass?.displayName ?? "Unknown"}");
        RefreshSelectionLabel();
        menuUi?.UpdateLobbyUI();
    }

    private void SetPhotonClassProperty(int index)
    {
        // Persist selection so the lobby sees it and late-joiners inherit it.
        var props = new Hashtable { { "classId", index } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void HydrateFromProperties()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("classId", out object value)
            && value is int idx && idx >= 0 && idx < classes.Count)
        {
            selectedIndex = idx;
        }
    }

    private void RefreshSelectionLabel()
    {
        if (selectionLabel == null) return;

        string name = HasSelection ? (SelectedClass?.displayName ?? "Unknown") : "None";
        selectionLabel.text = $"Class: {name}";
    }

    public string GetClassName(int index)
    {
        if (index < 0 || index >= classes.Count) return "Unknown";
        return classes[index] != null ? classes[index].displayName : "Unknown";
    }
}
