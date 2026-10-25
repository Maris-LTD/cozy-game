using CozyGachapon.Contracts;
using CozyGachapon.Foundation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CozyGachapon
{
    public sealed class StartupCurtainView : MonoBehaviour, ICurtainPresenter
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private GraphicRaycaster _raycaster;
        [SerializeField] private RectTransform _curtainRoot;
        [SerializeField] private GameObject _shopSign;
        [SerializeField] private GameObject _retryPanel;
        [SerializeField] private CanvasGroup _retryPanelGroup;
        [SerializeField] private TMP_Text _stepIdLabel;

        private RectTransform _canvasRect;
        private StartupCurtainController _controller;
        private ViewportCalculator _viewport;
        private FrameChangedEvent _frame;
        private float _fadeSeconds;
        private float _errorElapsed;
        private float _appliedScale;
        private float _appliedOffset;
        private bool _hasFrame;
        private bool _layoutDirty = true;
        private bool _errorShown;
        private bool _fallbackOnly;

        private void Awake()
        {
            _canvasRect = (RectTransform)_canvas.transform;
        }

        private void OnDestroy()
        {
            if (_viewport != null)
            {
                _viewport.OnFrameChanged -= HandleFrameChanged;
            }
        }

        public void Bind(StartupCurtainController controller, ViewportCalculator viewport, float fadeSeconds)
        {
            _controller = controller;
            _viewport = viewport;
            _fadeSeconds = fadeSeconds;

            if (_viewport == null)
            {
                return;
            }

            _viewport.OnFrameChanged += HandleFrameChanged;
            if (_viewport.HasFrame)
            {
                HandleFrameChanged(_viewport.CurrentFrame);
            }
        }

        public void ShowBootFailure(BootFailedEvent failure)
        {
            _stepIdLabel.text = failure.StepId;
        }

        public void ShowFallbackError(string stepId)
        {
            _stepIdLabel.text = stepId;
            _fallbackOnly = true;
            _fadeSeconds = 0f;
            EnterErrorState();
        }

        private void LateUpdate()
        {
            if (_fallbackOnly)
            {
                UpdateErrorFade();
                return;
            }

            if (_controller == null)
            {
                return;
            }

            switch (_controller.State)
            {
                case CurtainState.Covering:
                    ApplyLayout(0f);
                    break;
                case CurtainState.Revealing:
                case CurtainState.RevealFrozen:
                    ApplyLayout(CurtainLayoutMath.RevealOffset(_controller.RevealProgress, CurrentCanvasHeight()));
                    break;
                case CurtainState.Open:
                    CloseCurtain();
                    break;
                case CurtainState.ErrorShown:
                    ApplyLayout(0f);
                    EnterErrorState();
                    UpdateErrorFade();
                    break;
            }
        }

        private void HandleFrameChanged(FrameChangedEvent frame)
        {
            _frame = frame;
            _hasFrame = true;
            _layoutDirty = true;
        }

        private float CurrentCanvasHeight()
        {
            if (_hasFrame)
            {
                return CurtainLayoutMath.ToCanvasRect(_frame.FrameX, _frame.FrameY, _frame.FrameWidth, _frame.FrameHeight, _canvas.scaleFactor).height;
            }

            return _canvasRect.rect.height;
        }

        private void ApplyLayout(float offset)
        {
            float scale = _canvas.scaleFactor;
            bool changed = _layoutDirty || scale != _appliedScale || offset != _appliedOffset;
            if (!changed)
            {
                return;
            }

            _layoutDirty = false;
            _appliedScale = scale;
            _appliedOffset = offset;

            if (!_hasFrame)
            {
                _curtainRoot.anchoredPosition = new Vector2(0f, offset);
                return;
            }

            Rect rect = CurtainLayoutMath.ToCanvasRect(_frame.FrameX, _frame.FrameY, _frame.FrameWidth, _frame.FrameHeight, scale);
            _curtainRoot.anchorMin = Vector2.zero;
            _curtainRoot.anchorMax = Vector2.zero;
            _curtainRoot.pivot = Vector2.zero;
            _curtainRoot.sizeDelta = new Vector2(rect.width, rect.height);
            _curtainRoot.anchoredPosition = new Vector2(rect.x, rect.y + offset);
        }

        private void EnterErrorState()
        {
            if (_errorShown)
            {
                return;
            }

            _errorShown = true;
            _errorElapsed = 0f;
            SetRetryPanelAlpha(_fadeSeconds > 0f ? 0f : 1f);
            _shopSign.SetActive(false);
            _retryPanel.SetActive(true);
        }

        private void UpdateErrorFade()
        {
            if (!_errorShown || _retryPanelGroup.alpha >= 1f)
            {
                return;
            }

            _errorElapsed += Time.unscaledDeltaTime;
            SetRetryPanelAlpha(CurtainLayoutMath.FadeAlpha(_errorElapsed, _fadeSeconds));
        }

        private void SetRetryPanelAlpha(float alpha)
        {
            bool isFullyVisible = alpha >= 1f;
            _retryPanelGroup.alpha = alpha;
            _retryPanelGroup.interactable = isFullyVisible;
            _retryPanelGroup.blocksRaycasts = isFullyVisible;
        }

        private void CloseCurtain()
        {
            _canvas.enabled = false;
            _raycaster.enabled = false;
            enabled = false;
        }
    }
}
