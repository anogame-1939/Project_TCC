using UnityEditor;
using UnityEngine;

public class StickBehavior : MonoBehaviour
{
    [SerializeField]
    public StickBehavior2 _StickBehavior;
    public void Attack()
    {
        _StickBehavior.Attack();
    }

    void ReturnToInitial()
    {
    }

}
