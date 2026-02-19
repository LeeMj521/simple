using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectedUserStatusUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("GameManager (비어 있으면 자동 검색)")]
    [SerializeField] private GameManager gameManager;

    [Header("UI")]
    [SerializeField] private Image userProfileImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI jobText;

    private UserObject _lastSelectedUser;

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (gameManager == null)
            return;

        // 선택된 유저가 변경되었는지 확인
        if (gameManager.selectedUser != _lastSelectedUser)
        {
            _lastSelectedUser = gameManager.selectedUser;
            UpdateUI();
        }
    }

    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (gameManager == null)
            return;

        UserObject selectedUser = gameManager.selectedUser;

        if (selectedUser == null)
        {
            // 선택된 유저가 없으면 UI 비우기
            if (userProfileImage != null)
                userProfileImage.sprite = null;
            if (nameText != null)
                nameText.text = "";
            return;
        }

        if (userProfileImage != null && selectedUser.profileSprite != null)
            userProfileImage.sprite = selectedUser.profileSprite.sprite;
        if (nameText != null)
            nameText.text = selectedUser.UserName;
        if (attackText != null)
            attackText.text = selectedUser.attack.ToString();
        if (jobText != null)
            jobText.text = selectedUser.job.ToString();
    }
}
