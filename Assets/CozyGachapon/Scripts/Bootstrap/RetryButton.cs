using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CozyGachapon
{
    public sealed class RetryButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private string _sceneName = "MainGame";

        private readonly RetryTriggerGuard _guard = new RetryTriggerGuard();
        private UnityAction _onClick;

        private void Awake()
        {
            _onClick = HandleClick;
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(_onClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(_onClick);
        }

        private void HandleClick()
        {
            if (!_guard.TryTrigger())
            {
                return;
            }

            _button.interactable = false;
            SceneManager.LoadScene(_sceneName);
        }
    }
}
