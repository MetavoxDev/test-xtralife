using UnityEngine;

namespace RedRunner.UI
{
    public class UIScreen : MonoBehaviour
	{
        [SerializeField]
        internal UIScreenInfo ScreenInfo = default;
        [SerializeField]
        protected Animator m_Animator = null;
		[SerializeField]
		protected CanvasGroup m_CanvasGroup = null;

        public bool IsOpen { get; set; }

        public virtual void UpdateScreenStatus(bool open)
        {
            m_Animator.SetBool("Open", open);
            m_CanvasGroup.interactable = open;
            m_CanvasGroup.blocksRaycasts = open;
            IsOpen = open;
        }
	}

}