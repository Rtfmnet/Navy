// Navy.Presentation.Common

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Navy.Presentation.Common
{
    public enum AppScreen
    {
        Menu,
        Lobby,
        MapSelect,
        Setup,
        Game,
        Settings,
        Result
    }

    /// <summary>
    /// Manages which UI panel is currently active.
    /// Assign all panels via the Inspector on PC #2.
    /// </summary>
    public sealed class PanelRouter : MonoBehaviour
    {
        [SerializeField] private List<PanelEntry> _panels;

        private AppScreen _currentScreen;

        [System.Serializable]
        private class PanelEntry
        {
            public AppScreen Screen;
            public UIPanelBase Panel;
        }

        public void Show(AppScreen screen)
        {
            _currentScreen = screen;
            foreach (var entry in _panels)
            {
                if (entry.Panel == null) continue;
                if (entry.Screen == screen)
                    entry.Panel.Show();
                else
                    entry.Panel.Hide();
            }
        }

        public AppScreen CurrentScreen => _currentScreen;
    }
}
