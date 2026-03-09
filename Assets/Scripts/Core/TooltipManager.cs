using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [SerializeField] private TooltipUI tooltipUI;
    private Coroutine _showCoroutine;

    void Awake() => Instance = this;

    public void Show(ITooltipProvider provider, float delay = 0.3f)
    {
        if (_showCoroutine != null) StopCoroutine(_showCoroutine);
        _showCoroutine = StartCoroutine(ShowAfterDelay(provider, delay));
    }

    public void Hide()
    {
        if (_showCoroutine != null) StopCoroutine(_showCoroutine);
        tooltipUI.Hide();
    }

    private IEnumerator ShowAfterDelay(ITooltipProvider provider, float delay)
    {
        yield return new WaitForSeconds(delay);
        tooltipUI.Populate(provider.GetTooltipData());
        tooltipUI.Show();
    }
}
public interface ITooltipProvider
{
    string GetTooltipTitle();
    string GetTooltipDescription();
    Sprite GetTooltipIcon();         // optional
    TooltipData GetTooltipData();    // for richer tooltips
}

