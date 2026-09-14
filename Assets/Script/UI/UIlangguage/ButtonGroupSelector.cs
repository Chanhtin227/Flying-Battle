using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class ButtonGroupSelector : MonoBehaviour
{
    [System.Serializable]
    public class ButtonItem
    {
        public Button button;
        public Image background;   // kéo object "Background" của button vào đây
    }

    public List<ButtonItem> buttons;

    public Color selectedColor = Color.white;              // sáng
    public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1f); // tối

    private int currentIndex = 0;

    void Start()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i; // tránh lỗi closure
            buttons[i].button.onClick.AddListener(() => OnSelect(index));
        }

        RefreshUI();
    }
    
    void OnSelect(int index)
    {
        currentIndex = index;
        RefreshUI();

        // TODO: xử lý logic khi chọn button (đổi ngôn ngữ, đổi tab, v.v...)
    }
    IEnumerator FadeColor(Image img, Color target, float duration = 0.15f)
    {
        Color start = img.color;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            img.color = Color.Lerp(start, target, time / duration);
            yield return null;
        }
        img.color = target;
    }
    void RefreshUI()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            bool isSelected = (i == currentIndex);   // khai báo TRONG vòng lặp

            StartCoroutine(FadeColor(
                buttons[i].background,
                isSelected ? selectedColor : unselectedColor   // dùng NGAY trong vòng lặp
            ));
        }
    }

}