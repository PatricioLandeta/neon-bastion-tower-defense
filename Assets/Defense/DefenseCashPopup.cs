using UnityEngine;
using UnityEngine.UI;

// Cosmetic payout feedback; the game director awards money exactly once per destroyed enemy.
public class DefenseCashPopup : MonoBehaviour
{
    private Text label;
    private float age;
    public void Initialize(Text text) { label=text; }
    void Update()
    {
        age+=Time.deltaTime;
        label.rectTransform.anchoredPosition+=Vector2.up*(38f*Time.deltaTime);
        Color color=label.color;color.a=1f-Mathf.Clamp01((age-.35f)/.65f);label.color=color;
        if(age>=1f) Destroy(gameObject);
    }
}
