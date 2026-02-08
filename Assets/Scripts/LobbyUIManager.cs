using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefaultNamespace
{
    public class LobbyUIManager : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button tutorialButton;
        [SerializeField] private Button exitTutorialButton;

        [Header("Panels")] 
        [SerializeField] private GameObject tutorialPanel;

        private void Awake()
        {
            startButton.onClick.AddListener(OnStartClicked);
            tutorialButton.onClick.AddListener(OnTutorialClicked);
            exitTutorialButton.onClick.AddListener(OnExitTutorialClicked);
            
            tutorialPanel.SetActive(false);
        }

        private void OnStartClicked()
        {
            SceneManager.LoadScene("InGameScene");
        }

        private void OnTutorialClicked()
        {
            tutorialPanel.SetActive(true);
        }

        private void OnExitTutorialClicked()
        {
            tutorialPanel.SetActive(false);
        }
    }
}