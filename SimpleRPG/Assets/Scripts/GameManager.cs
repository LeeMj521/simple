using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject player;
    public UserObject selectedUser;
    
    private void Update()
    {
        // 우클릭으로 유저 선택
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            
            Collider2D hit = Physics2D.OverlapPoint(mousePos);
            if (hit != null)
            {
                UserObject user = hit.GetComponent<UserObject>();
                if (user != null)
                {
                    selectedUser = user;
                }
            }
        }
    }
}
