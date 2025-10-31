using UnityEngine;

namespace MonkePhone.Behaviours;

public class Keyboard : MonoBehaviour
{
    public bool Active
    {
        get => gameObject.activeSelf;
        set => gameObject.SetActive(value);
    }

    public MeshRenderer Mesh => transform.Find("Model").GetComponent<MeshRenderer>();
}