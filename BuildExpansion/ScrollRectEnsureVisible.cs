using UnityEngine;
using UnityEngine.UI;

// All credit for this goes to jschieck : https://forum.unity.com/threads/scrollrect-scroll-to-a-gameobject-position.473214/

namespace BuildExpansion
{
    [RequireComponent(typeof(ScrollRect))]
    public class ScrollRectEnsureVisible : MonoBehaviour
    {
        public RectTransform maskTransform;

        private PreventClickDragScrollRect mScrollRect;
        private RectTransform mScrollTransform;
        private RectTransform mContent;

        public void CenterOnItem(RectTransform target)
        {
            // Item is here
            var itemCenterPositionInScroll = GetWorldPointInWidget(mScrollTransform, GetWidgetWorldPoint(target));
            // But must be here
            var targetPositionInScroll = GetWorldPointInWidget(mScrollTransform, GetWidgetWorldPoint(maskTransform));
            // So it has to move this distance
            var difference = targetPositionInScroll - itemCenterPositionInScroll;
            difference.z = 0f;

            //clear axis data that is not enabled in the scrollrect
            if (!mScrollRect.horizontal)
            {
                difference.x = 0f;
            }
            if (!mScrollRect.vertical)
            {
                difference.y = 0f;
            }

            var normalizedDifference = new Vector2(
                difference.x / (mContent.rect.size.x - mScrollTransform.rect.size.x),
                difference.y / (mContent.rect.size.y - mScrollTransform.rect.size.y));

            var newNormalizedPosition = mScrollRect.normalizedPosition - normalizedDifference;
            if (mScrollRect.movementType != ScrollRect.MovementType.Unrestricted)
            {
                newNormalizedPosition.x = Mathf.Clamp01(newNormalizedPosition.x);
                newNormalizedPosition.y = Mathf.Clamp01(newNormalizedPosition.y);
            }

            mScrollRect.normalizedPosition = newNormalizedPosition;
        }
        private void Awake()
        {
            mScrollRect = GetComponent<PreventClickDragScrollRect>();
            mScrollTransform = mScrollRect.transform as RectTransform;
            mContent = mScrollRect.content;
            Reset();
        }
        private void Reset()
        {
            if (maskTransform == null)
            {
                var mask = GetComponentInChildren<Mask>(true);
                if (mask)
                {
                    maskTransform = mask.rectTransform;
                }
                if (maskTransform == null)
                {
                    var mask2D = GetComponentInChildren<RectMask2D>(true);
                    if (mask2D)
                    {
                        maskTransform = mask2D.rectTransform;
                    }
                }
            }
        }
        private Vector3 GetWidgetWorldPoint(RectTransform target)
        {
            //pivot position + item size has to be included
            var pivotOffset = new Vector3(
                (0.5f - target.pivot.x) * target.rect.size.x,
                (0.5f - target.pivot.y) * target.rect.size.y,
                0f);
            var localPosition = target.localPosition + pivotOffset;
            return target.parent.TransformPoint(localPosition);
        }
        private Vector3 GetWorldPointInWidget(RectTransform target, Vector3 worldPoint)
        {
            return target.InverseTransformPoint(worldPoint);
        }
    }
}
