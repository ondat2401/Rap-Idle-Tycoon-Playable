using Amanotes.Core;
using Amanotes.MagicTiles3;
using DG.Tweening;
using UnityEngine;

public class ReviveScreen : AnimatedGUIBase
{
    [SerializeField] private RectTransform hand;

    [Header("Pointing Animation")]
    [SerializeField] private Vector2 basePositionOffset;
    [SerializeField] private Vector2 targetOffset;
    [SerializeField] private float pointDuration = 0.6f;
    [SerializeField] private Ease pointEase = Ease.InOutSine;

    private Camera _worldCamera;
    private Camera _uiCamera;
    private RectTransform _handParent;
    private Tweener _handTween;
    private Vector3 _handBasePosition;
    private Vector3 _handBaseScale;
    private Vector3 _targetWorldPosition;
    private Vector3 _baseLocalPosition;
    private Vector3 _targetLocalPosition;
    private bool _hasTarget;
    private bool _flipHorizontal;

    public override void OnScreenInitialize()
    {
        base.OnScreenInitialize();
        GUIManager.Instance.RegisterScreen(this);
        var canvas = GetComponentInParent<Canvas>();
        _uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        _handParent = hand != null ? hand.parent as RectTransform : null;
        if (hand == null) return;
        _handBasePosition = hand.localPosition;
        _handBaseScale = hand.localScale;
    }

    public void PointAt(Vector3 worldPosition, Camera worldCamera, bool flipHorizontal)
    {
        if (hand == null) return;

        _targetWorldPosition = worldPosition;
        _worldCamera = worldCamera;
        _flipHorizontal = flipHorizontal;
        _hasTarget = worldCamera != null;
        hand.gameObject.SetActive(false);

        if (State != ScreenState.Visible) return;
        StartPointingLoop();
    }

    public override void OnScreenShow()
    {
        base.OnScreenShow();
        StartPointingLoop();
        EventBus.Publish(new ReviveBGEvent{isRevive = true});
    }

    public override void OnScreenHide()
    {
        base.OnScreenHide();
        _handTween?.Pause();
        _hasTarget = false;
        if (hand == null) return;
        hand.localPosition = _handBasePosition;
        hand.localScale = _handBaseScale;
        hand.gameObject.SetActive(false);
        EventBus.Publish(new ReviveBGEvent{isRevive = false});
        
    }

    public override void OnScreenDestroy()
    {
        _handTween?.Kill();
        _handTween = null;
        base.OnScreenDestroy();
    }

    private void StartPointingLoop()
    {
        if (!UpdateHandTargetPosition())
        {
            _handTween?.Pause();
            if (hand != null) hand.gameObject.SetActive(false);
            return;
        }

        hand.localPosition = _baseLocalPosition;
        hand.localScale = _flipHorizontal
            ? new Vector3(-_handBaseScale.x, _handBaseScale.y, _handBaseScale.z)
            : _handBaseScale;
        hand.gameObject.SetActive(true);

        if (_handTween == null)
        {
            _handTween = hand
                .DOLocalMove(_targetLocalPosition, pointDuration)
                .SetEase(pointEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetAutoKill(false)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Pause();
        }
        else
        {
            _handTween.ChangeValues(_baseLocalPosition, _targetLocalPosition, pointDuration)
                .SetEase(pointEase);
        }

        _handTween.Restart();
    }

    private bool UpdateHandTargetPosition()
    {
        if (!_hasTarget || _worldCamera == null || _handParent == null) return false;

        var screenPosition = _worldCamera.WorldToScreenPoint(_targetWorldPosition);
        if (screenPosition.z < 0f) return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_handParent, screenPosition, _uiCamera, out var localPosition)) return false;

        var horizontalDirection = _flipHorizontal ? -1f : 1f;
        _baseLocalPosition = new Vector3(
            localPosition.x + basePositionOffset.x * horizontalDirection,
            localPosition.y + basePositionOffset.y,
            _handBasePosition.z);
        _targetLocalPosition = new Vector3(
            localPosition.x + targetOffset.x * horizontalDirection,
            localPosition.y + targetOffset.y,
            _handBasePosition.z);
        return true;
    }
}