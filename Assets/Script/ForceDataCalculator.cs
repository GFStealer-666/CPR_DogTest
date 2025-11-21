using UnityEngine;
using System;

// Singleton class for using later on
public class ForceDataCalculator : MonoBehaviour
{
    public static ForceDataCalculator Instance
    {
        get => _instance;
        private set => _instance = value;
    }
    private static ForceDataCalculator _instance;

    private void Awake()
    {
        if(_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    private void OnDestroy()
    {   
        if(_instance)
        {
            _instance = null;
        }
    }
}
