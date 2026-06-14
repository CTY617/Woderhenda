using System;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Serializable]
    public class DrawerTab
    {
        public MenuDrawerTag tag;
        public GameObject content;
    }

    [SerializeField] DrawerTab[] tabs = Array.Empty<DrawerTab>();
    [SerializeField] int defaultTabIndex;
    [SerializeField] bool animateTransitions = true;

    int currentTabIndex = -1;

    void Awake()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i].tag == null)
                continue;

            tabs[i].tag.Bind(this, i);
        }
    }

    void Start()
    {
        if (tabs.Length == 0)
            return;

        int index = Mathf.Clamp(defaultTabIndex, 0, tabs.Length - 1);
        Select(index, false);
    }

    public void Select(int index)
    {
        Select(index, animateTransitions);
    }

    public void Select(int index, bool animate)
    {
        if (tabs.Length == 0 || index < 0 || index >= tabs.Length)
            return;

        if (index == currentTabIndex)
            return;

        for (int i = 0; i < tabs.Length; i++)
        {
            bool focused = i == index;
            if (tabs[i].tag != null)
                tabs[i].tag.SetFocused(focused, animate && currentTabIndex >= 0);

            if (tabs[i].content != null)
                tabs[i].content.SetActive(focused);
        }

        currentTabIndex = index;
    }
}
