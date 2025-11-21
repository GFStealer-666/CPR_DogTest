using TMPro;
using UnityEngine;

public class ForceDataUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _forceText;

    void OnEnable()
    {
        ForceDataBuffer.OnForceUpdatedMainThread += DisplayForce;
    }
    void OnDisable()
    {
        ForceDataBuffer.OnForceUpdatedMainThread -= DisplayForce;
    }

    private void DisplayForce(float amount)
    {
        _forceText.text = $"Force : {amount}";
    }

    // void Update()
    // {
        
    //     _forceText.text = ForceDataBuffer.Instance?.LatestBuffer.ToString();
    // } 

}
