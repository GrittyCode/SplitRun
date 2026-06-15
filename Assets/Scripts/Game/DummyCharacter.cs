using UnityEngine;
using SplitRun.Utility;
using R3;

public class DummyCharacter : MonoBehaviour
{
    private void Start()
    {
        GetComponent<SwipeDetector>().OnSwipe
            .Subscribe(dir => Debug.Log($"[SwipeDetector] {dir}"))
            .AddTo(this);
    }
}
