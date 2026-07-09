using UnityEngine;
using UnityEngine.EventSystems;

namespace Tessera.UI
{
    /// <summary>PlayButton 클릭 Collider의 Pointer Click을 PlayButton3DView로 전달하는 Relay다.</summary>
    public class PlayButtonClickRelay3D : MonoBehaviour, IPointerClickHandler
    {
        private PlayButton3DView owner;

        /// <summary>Relay 소유 Play 버튼을 연결한다.</summary>
        public void Bind(PlayButton3DView playButton)
        {
            owner = playButton;
        }

        /// <summary>Relay 소유 Play 버튼 연결을 해제한다.</summary>
        public void Unbind(PlayButton3DView playButton)
        {
            if (owner == playButton)
                owner = null;
        }

        /// <summary>Pointer Click 이벤트를 Play 버튼 View로 전달한다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            owner?.NotifyClicked(eventData);
        }
    }
}
