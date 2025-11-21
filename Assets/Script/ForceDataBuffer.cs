using System;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ForceDataBuffer : MonoBehaviour
{
    public static event Action OnForceDataBufferSetup;
    public static event Action OnForceDataBufferReset;
    private static ForceDataBuffer _instance;
    public static ForceDataBuffer Instance
    {
        get => _instance;
        private set => _instance = value;
    }

    [Header("Buffer Settings")]
    [SerializeField, Tooltip("Maximum number of force readings to store in the buffer.")]
    private int maxBufferSize = 100;

    [SerializeField, Tooltip("Queue to store force readings.")]
    private Queue<float> forceBufferQueue = new Queue<float>();
    private readonly object bufferLock = new object();

    private float _latest; // For UnityMainthread
    private bool _hasNew;
    public static event Action<float> OnForceUpdatedMainThread;

    [Header("Debug")]
    [SerializeField, Tooltip("Latest force reading received.")]
    private float latestBuffer = 0f;
    [SerializeField, Tooltip("Lastest force show in list")]
    private List<float> forceBufferList = new List<float>();



#region Setup
    private void Awake()
    {
        maxBufferSize = Mathf.Max(1, maxBufferSize);
        SingletonSetup();
    }

    private void SingletonSetup()
    {
        if (_instance == null)
        {
            _instance = this;
            OnForceDataBufferSetup?.Invoke();
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this.gameObject); 
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
#endregion

#region Runtime
    private void Update()
    {
        if (!_hasNew) return;

        float value;
        lock (bufferLock)
        {
            value = _latest;
            _hasNew = false;
        }

        OnForceUpdatedMainThread?.Invoke(value);
    }

#endregion


#region Getter properties
    // Summary : Public method to get the latest force reading
    public float LatestBuffer
    {
        get
        {
            lock(bufferLock)
            {
                return latestBuffer;
            }
        }
    }
    // Summary : Public method to get a copy of the entire force buffer queue
    public Queue<float> BufferQueue
    {
        get
        {
            lock(bufferLock)
            {
                return new Queue<float>(forceBufferQueue);
            }
        }
    }
    // Summary : Public method to get a specified amount of force readings from the buffer queue oldest
    public Queue<float> GetBufferQueueOldest(int amount)
    {
        lock(bufferLock)
        {
            amount = Math.Max(0,Math.Min(amount, BufferSize));
            if(amount == 0) return null;

            Queue<float> temp = new Queue<float>(amount);
            foreach(var item in forceBufferQueue)
            {
                if(amount -- <=0) break;
                temp.Enqueue(item);
            }
            return temp;
        }
    }
    // Summary : Public method to get a specified amount of force readings from the buffer queue newest
    public Queue<float> GetBufferQueueNewest(int amount)
    {
        lock(bufferLock)
        {
            amount = Math.Max(0,Math.Min(amount, BufferSize));
            if(amount == 0) return null;

            Queue<float> temp = new Queue<float>(amount);
            float[] arr = forceBufferQueue.ToArray();

            // TLDR:
            // start from the length of orginal array - amount to get the newest values
            // since newest values are at the end of the array
            // ex. if array are 30 and amount needed is 20, so it 30 - 20  = 10 start from index 10 to 29 (20 values)

            for(int i = arr.Length - amount; i < arr.Length; i++) 
            {
                temp.Enqueue(arr[i]);
            }
            return temp;

        }
    }

    // Summary : Public method to get the current size of the buffer queue
    public int BufferSize
    {
        get
        {
            lock(bufferLock)
            {
                return forceBufferQueue.Count;
            }
        }
    }

    // Summary : Public method to get the max buffer size
    public int MaxBufferSize
    {
        get
        {
            lock(bufferLock)
            {
                return maxBufferSize;
            }
        }
    }

    // Summary : Public method to check if the queue is full
    public bool IsBufferFull
    {
        get
        {
            lock(bufferLock)
            {
                return forceBufferQueue.Count >= maxBufferSize;
            }
        }
    }

    // Summary : Public method to check if the queue is empty
    public bool IsBufferEmpty
    {
        get
        {
            lock(bufferLock)
            {
                return forceBufferQueue.Count == 0;
            }
        }
    }

    // Summary : Public method to check if the buffer has any value 
    public bool HasValue
    {
        get
        {
            lock(bufferLock)
            {
                return forceBufferQueue.Count > 0;
            }
        }
    }

#endregion

#region Setter Methods

    // Summary : Public method to add a force to queue buffer
    public void AddForceBuffer(float force)
    {
        force = Mathf.Max(0f, force); 

        lock(bufferLock)
        {
            _latest = force;
            _hasNew = true;

            forceBufferQueue.Enqueue(force);
            latestBuffer = force;

            forceBufferList.Add(force);

            if(forceBufferQueue.Count > maxBufferSize)
            {
                forceBufferQueue.Dequeue();
            }

            if(forceBufferList.Count > MaxBufferSize)
            {
                forceBufferList.RemoveAt(0);
            }

        }
        
    }

    // Summary : Public method to clear the buffer queue
    public void ClearBufferQueue()
    {
        lock(bufferLock)
        {
            forceBufferQueue.Clear();
            forceBufferList.Clear();
            latestBuffer = 0f;
        }
        OnForceDataBufferReset?.Invoke();
    }

    // Summary : Public method to set the max buffer size
    public void SetMaxBufferSize(int size)
    {
        size = Mathf.Max(1, size);

        lock(bufferLock)
        {
            maxBufferSize = size;

            while(forceBufferQueue.Count > maxBufferSize)
            {
                forceBufferQueue.Dequeue();
            }
        }
    }
#endregion
}
