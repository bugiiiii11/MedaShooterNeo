using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SendOnTrigger : MonoBehaviour
{
    public UnityEvent OnCollisionAction;

    public void OnCollisionEnter2D(Collision2D collision)
    {
        var col = (collision.collider);
        if(col && OnCollisionAction != null)
        {
            OnCollisionAction.Invoke();
        }
    }
}
