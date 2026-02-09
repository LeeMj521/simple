using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvent : MonoBehaviour
{
    public void AnimationRemove(){
        if(gameObject != null)
            Destroy(gameObject);
    }
}
