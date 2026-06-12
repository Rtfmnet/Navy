// Navy.Presentation.Game

using System.Collections.Generic;
using Navy.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Navy.Presentation.Game
{
    /// <summary>
    /// Side panel listing shot history (FR-GP-08).
    /// Newest entry at the top, oldest at the bottom.
    /// </summary>
    public sealed class HistoryPanelView : MonoBehaviour
    {
        [SerializeField] private Transform     _entryContainer;
        [SerializeField] private GameObject    _entryPrefab;   // TMP_Text prefab, set on PC #2
        [SerializeField] private Button        _toggleButton;
        [SerializeField] private GameObject    _panelRoot;

        private readonly List<GameObject> _entries = new();
        private bool _isOpen;

        private void Start()
        {
            _toggleButton.onClick.AddListener(Toggle);
            _panelRoot.SetActive(false);
        }

        public void AddEntry(string nickName, Cell coord, ShotResult result)
        {
            string resultStr = result switch
            {
                ShotResult.Hit  => "Hit",
                ShotResult.Sunk => "Sunk",
                _               => "Miss"
            };

            string text = $"{nickName} — {CoordLabel(coord)} — {resultStr}";
            var go = Instantiate(_entryPrefab, _entryContainer);
            go.GetComponent<TMP_Text>().text = text;
            go.transform.SetAsFirstSibling();
            _entries.Add(go);
        }

        public void ClearEntries()
        {
            foreach (var e in _entries) Destroy(e);
            _entries.Clear();
        }

        private void Toggle()
        {
            _isOpen = !_isOpen;
            _panelRoot.SetActive(_isOpen);
        }

        private static string CoordLabel(Cell c)
        {
            char col = (char)('A' + c.X);
            return $"{col}{c.Y + 1}";
        }
    }
}
