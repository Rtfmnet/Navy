// Navy.Presentation.Common

using UnityEngine;

namespace Navy.Presentation.Common
{
    /// <summary>
    /// Base class for all UI panels. Provides Show/Hide helpers.
    /// Attach to the root GameObject of each panel.
    /// </summary>
    public abstract class UIPanelBase : MonoBehaviour
    {
        public virtual void Show() => gameObject.SetActive(true);

        public virtual void Hide() => gameObject.SetActive(false);

        public bool IsVisible => gameObject.activeSelf;
    }
}
