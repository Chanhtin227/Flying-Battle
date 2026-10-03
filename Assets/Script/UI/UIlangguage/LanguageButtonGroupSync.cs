using UnityEngine;
using Localization;


[RequireComponent(typeof(ButtonGroupSelector))]
public class LanguageButtonGroupSync : MonoBehaviour, ILocalizedElement
{
    private ButtonGroupSelector _selector;

    private void Awake()
    {
        _selector = GetComponent<ButtonGroupSelector>();
    }

    private void OnEnable()
    {
        LocalizationManager.Register(this);
    }

    private void OnDisable()
    {
        LocalizationManager.Unregister(this);
    }

    /// <summary>Được LocalizationManager tự động gọi — không cần gọi tay.</summary>
    public void ApplyLanguage(LanguageCode language)
    {
        if (_selector == null) return;

        _selector.SelectWithoutNotify((int)language);
    }
}