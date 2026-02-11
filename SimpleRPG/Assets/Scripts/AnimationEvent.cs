using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEvent : MonoBehaviour
{
  public void OnAnimationEnd(){
    if(gameObject != null)
      Destroy(gameObject);
  }
}
