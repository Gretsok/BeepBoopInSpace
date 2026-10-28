using System;
using System.Linq;
using DG.Tweening;
using Game.Characters;
using Game.Global.InputIndicationsManagement;
using Game.Global.PlayerManagement;
using Game.UITools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Game.MainMenu.CharacterManagement
{
    /// <summary>
    /// A player's circle in the character selection: waits for a player to join, then shows the character the player
    /// points at in the grid, until the player confirms it (ready). Drives the slot element of the screen's UI.
    /// </summary>
    public class CharacterWidget : MonoBehaviour
    {
        private const string k_JoinClass = "cs-slot--join";
        private const string k_SelectingClass = "cs-slot--selecting";
        private const string k_ReadyClass = "cs-slot--ready";
        private const int k_BurstParticleCount = 12;
        // The preview's top in its slot, as in the USS, and its size relative to the grid's icons.
        private const float k_PreviewTop = 85f;
        private const float k_IconToPreviewScale = 2.4f;

        public CharacterDataAsset CharacterDataAsset { get; private set; }

        [SerializeField]
        private AudioSource m_playerPopAudioSource;

        // The button that joins, shown in the empty slot next to the players.
        [SerializeField]
        private InputIndicationActionDataAsset m_joinIndicationAction;

        private VisualElement m_view;
        private VisualElement m_slot;
        private VisualElement m_burst;
        private VisualElement m_preview;
        private VisualElement m_badge;
        private Label m_badgeLabel;
        private InputIndicationView m_joinIndication;
        private Texture2D m_missingPreview;

        public AbstractPlayer Player { get; private set; }
        public int SlotIndex { get; private set; }
        public bool IsReady { get; private set; }

        /// <summary>The slot invites the next player to join.</summary>
        public bool IsJoinSlot { get; private set; }

        public void SetPlayer(AbstractPlayer player)
        {
            Player = player;
        }

        /// <summary>
        /// Binds the widget to its slot element. The screen clones a new visual tree each time it opens.
        /// <paramref name="missingPreview"/> stands in for a character without a picture or an icon.
        /// </summary>
        public void SetView(VisualElement view, int slotIndex, Texture2D missingPreview)
        {
            m_joinIndication?.Dispose();

            m_view = view;
            SlotIndex = slotIndex;
            m_missingPreview = missingPreview;
            m_slot = view.Q("slot");
            m_burst = view.Q("burst");
            m_preview = view.Q("preview");
            m_badge = view.Q("badge");
            m_badgeLabel = view.Q<Label>("badge-label");
            m_joinIndication = new InputIndicationView(view.Q("join-glyph"), m_joinIndicationAction) { ShowsLikelyDeviceWithoutPlayer = true };
            view.RegisterCallback<GeometryChangedEvent>(_ => AlignFiligrees());
            RefreshView();
        }

        // The circles show the background's pattern: its texture covers the screen, wherever the circle is.
        private void AlignFiligrees()
        {
            m_view.Query(className: "cs-filigree").ForEach(filigree =>
            {
                // Layout positions, not world bounds: these ignore the slot's ongoing scale and rotation tweens.
                var position = Vector2.zero;
                for (var element = filigree.hierarchy.parent; element != null; element = element.hierarchy.parent)
                    position += element.layout.position;
                filigree.style.left = -position.x;
                filigree.style.top = -position.y;
            });
        }

        /// <summary>The join button follows the main player's device, or the likeliest one before anyone joined.</summary>
        public void SetJoinIndicationPlayer(AbstractPlayer mainPlayer)
        {
            m_joinIndication?.SetPlayer(mainPlayer);
        }

        public bool CanPlay => IsActivated && IsReady;

        private CharacterBankManager m_characterBankManager;

        private void Awake()
        {
            CharacterBankManager.RegisterPostInitializationCallback(manager =>
            {
                m_characterBankManager = manager;
            });
            Deactivate(true);
        }

        private void OnDestroy()
        {
            m_joinIndication?.Dispose();
        }

        public bool IsActivated { get; private set; } = false;
        public event Action<CharacterWidget> OnActivated;
        public event Action<CharacterWidget> OnDeactivated;
        public event Action<CharacterWidget> OnReadyChanged;
        /// <summary>The player tried to get ready with a character another player has chosen.</summary>
        public event Action<CharacterWidget> OnRefused;

        public void Activate()
        {
            if (IsActivated)
                return;

            CharacterDataAsset = m_characterBankManager.GetFirstAvailableCharacterData();
            m_characterBankManager.NotifyAssociation(this, CharacterDataAsset);

            IsActivated = true;
            IsReady = false;
            IsJoinSlot = false;
            if (Player)
                Player.SetCharacterData(CharacterDataAsset);
            RefreshView();
            PlayAppear();
            OnActivated?.Invoke(this);
        }

        public void Deactivate(bool force = false)
        {
            if (!IsActivated && !force)
                return;

            CharacterDataAsset = null;
            if (m_characterBankManager)
                m_characterBankManager.UnregisterWidget(this);

            IsActivated = false;
            IsReady = false;
            Player = null;
            RefreshView();
            OnDeactivated?.Invoke(this);
        }

        /// <summary>Shows or hides the invitation to join in a slot without a player.</summary>
        public void SetJoinSlot(bool isJoinSlot)
        {
            if (IsActivated || IsJoinSlot == isJoinSlot)
                return;
            IsJoinSlot = isJoinSlot;
            RefreshView();
            if (isJoinSlot)
                PlayAppear();
        }

        private void RefreshView()
        {
            if (m_view == null)
                return;
            m_view.style.display = IsActivated || IsJoinSlot ? DisplayStyle.Flex : DisplayStyle.None;
            m_slot.EnableInClassList(k_JoinClass, !IsActivated && IsJoinSlot);
            m_slot.EnableInClassList(k_SelectingClass, IsActivated && !IsReady);
            m_slot.EnableInClassList(k_ReadyClass, IsActivated && IsReady);
            m_badgeLabel.text = IsActivated ? $"J{SlotIndex + 1}" : "+";

            var color = CharacterDataAsset ? CharacterDataAsset.SelectionColor : Color.white;
            // Colors come from the character: inline, so that the USS transitions still play when they change.
            m_view.Query(className: "cs-slot__tinted").ForEach(element => element.style.unityBackgroundImageTintColor = color);
            m_badge.style.unityBackgroundImageTintColor = IsReady ? color : StyleKeyword.Null;

            var hasPreview = CharacterDataAsset && CharacterDataAsset.SelectionPreview;
            var icon = CharacterDataAsset && CharacterDataAsset.SelectionIcon ? CharacterDataAsset.SelectionIcon : null;
            var preview = hasPreview ? CharacterDataAsset.SelectionPreview : icon ? icon : m_missingPreview;
            m_preview.style.backgroundImage = preview ? new StyleBackground(preview) : StyleKeyword.Null;
            // The icon standing in for the preview is drawn 2.4 times its grid size: lower it as much.
            m_preview.style.top = !hasPreview && icon ? k_PreviewTop + CharacterDataAsset.SelectionIconOffset * k_IconToPreviewScale : StyleKeyword.Null;
        }

        /// <summary>Points at another character, while not ready.</summary>
        public void Hover(CharacterDataAsset characterDataAsset)
        {
            if (!IsActivated || IsReady || characterDataAsset == null || characterDataAsset == CharacterDataAsset)
                return;
            CharacterDataAsset = characterDataAsset;
            m_characterBankManager.NotifyAssociation(this, CharacterDataAsset);
            Player.SetCharacterData(CharacterDataAsset);
            RefreshView();

            m_preview.KillTweens();
            m_preview.style.scale = new Scale(new Vector3(0.8f, 0.8f, 1f));
            m_preview.TweenScale(1f, 0.25f).SetEase(Ease.OutBack).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            m_onCharacterDataUpdated?.Invoke();
            OnCharacterDataUpdated?.Invoke();
        }

        /// <summary>Confirms the character, unless another player already has. Returns whether the player is ready.</summary>
        public bool TrySetReady()
        {
            if (!IsActivated || IsReady)
                return IsReady;
            if (m_characterBankManager.IsCharacterAlreadyTakenByAnotherWidget(CharacterDataAsset, this))
            {
                PlayRefusal();
                OnRefused?.Invoke(this);
                return false;
            }
            IsReady = true;
            Player.SetCharacterData(CharacterDataAsset);
            RefreshView();
            PlayReady();
            OnReadyChanged?.Invoke(this);
            return true;
        }

        public void CancelReady()
        {
            if (!IsActivated || !IsReady)
                return;
            IsReady = false;
            RefreshView();
            ClearBurst();
            m_preview.KillTweens();
            m_slot.KillTweens();
            m_slot.PunchScale(-0.06f, 0.25f, 4).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            OnReadyChanged?.Invoke(this);
        }

        public void Pop()
        {
            if (!IsActivated)
                return;
            m_preview.KillTweens();
            m_preview.PunchScale(0.15f, 0.3f).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            m_playerPopAudioSource.Play();
        }

        /// <summary>Everybody is ready: a last bounce before the game starts, once the last stamp has landed.</summary>
        public void PlayLaunch(float delay)
        {
            if (!IsActivated)
                return;
            DOVirtual.DelayedCall(delay, () =>
            {
                m_slot.KillTweens();
                m_slot.PunchScale(0.08f, 0.4f, 3).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }, ignoreTimeScale: true).SetTarget(m_slot).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        #region Motion

        private void PlayAppear()
        {
            if (m_slot == null)
                return;
            m_slot.KillTweens();
            m_slot.style.scale = new Scale(new Vector3(0.6f, 0.6f, 1f));
            m_slot.style.opacity = 0f;
            m_slot.TweenOpacity(1f, 0.2f).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            m_slot.TweenScale(1f, 0.35f).SetEase(Ease.OutBack).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(m_slot.ResetTransform);
        }

        // The circle is stamped with the character's color: the USS transitions swap the rings, and these tweens add
        // the jump of the robot, the punch of the badge, a flash, a shockwave and a burst of confetti.
        private void PlayReady()
        {
            var color = CharacterDataAsset.SelectionColor;
            ClearBurst();

            m_preview.KillTweens();
            DOTween.Sequence()
                .Append(m_preview.TweenTranslate(new Vector2(0f, -46f), 0.16f).SetEase(Ease.OutQuad))
                .Join(m_preview.TweenScale(new Vector2(0.92f, 1.1f), 0.16f).SetEase(Ease.OutQuad))
                .Append(m_preview.TweenTranslate(Vector2.zero, 0.22f).SetEase(Ease.InQuad))
                .Join(m_preview.TweenScale(Vector2.one, 0.22f))
                .Append(m_preview.TweenScale(new Vector2(1.12f, 0.88f), 0.08f).SetEase(Ease.OutQuad))
                .Append(m_preview.TweenScale(Vector2.one, 0.3f).SetEase(Ease.OutElastic))
                .SetTarget(m_preview).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(m_preview.ResetTransform);

            m_badge.KillTweens();
            m_badge.PunchScale(0.45f, 0.45f, 5).SetDelay(0.08f).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            m_slot.KillTweens();
            m_slot.PunchScale(0.06f, 0.35f, 4).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            // When the stamp lands.
            var flash = new VisualElement { pickingMode = PickingMode.Ignore };
            flash.AddToClassList("cs-slot__flash");
            flash.style.opacity = 0f;
            m_burst.Add(flash);
            DOTween.Sequence()
                .AppendInterval(0.1f)
                .Append(flash.TweenOpacity(0.55f, 0.05f))
                .Append(flash.TweenOpacity(0f, 0.3f).SetEase(Ease.OutQuad))
                .SetTarget(flash).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(flash.RemoveFromHierarchy);

            var shockwave = new VisualElement { pickingMode = PickingMode.Ignore };
            shockwave.AddToClassList("cs-slot__shockwave");
            shockwave.style.unityBackgroundImageTintColor = color;
            m_burst.Add(shockwave);
            shockwave.TweenScale(1.45f, 0.55f).SetEase(Ease.OutCubic).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            shockwave.TweenOpacity(0f, 0.55f).SetEase(Ease.InQuad).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnKill(shockwave.RemoveFromHierarchy);

            for (int i = 0; i < k_BurstParticleCount; i++)
            {
                var particle = new VisualElement { pickingMode = PickingMode.Ignore };
                particle.AddToClassList("cs-slot__particle");
                particle.style.unityBackgroundImageTintColor = i % 3 == 0 ? Color.white : color;
                var size = UnityEngine.Random.Range(12f, 24f);
                particle.style.width = size;
                particle.style.height = size;
                particle.style.marginLeft = -size / 2f;
                particle.style.marginTop = -size / 2f;
                m_burst.Add(particle);

                var angle = (i + UnityEngine.Random.Range(-0.3f, 0.3f)) * Mathf.PI * 2f / k_BurstParticleCount;
                var distance = UnityEngine.Random.Range(230f, 290f);
                var duration = UnityEngine.Random.Range(0.5f, 0.7f);
                particle.TweenTranslate(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance, duration)
                    .SetEase(Ease.OutCubic).SetLink(gameObject, LinkBehaviour.KillOnDisable);
                particle.TweenScale(0.2f, duration).SetEase(Ease.InQuad).SetLink(gameObject, LinkBehaviour.KillOnDisable);
                particle.TweenOpacity(0f, duration).SetEase(Ease.InCubic).SetLink(gameObject, LinkBehaviour.KillOnDisable)
                    .OnKill(particle.RemoveFromHierarchy);
            }
        }

        // Whatever an interrupted burst left behind.
        private void ClearBurst()
        {
            // Killing a tween removes its element: kill from a copy of the children.
            foreach (var element in m_burst.Children().ToList())
                DOTween.Kill(element);
            m_burst.Clear();
        }

        private void PlayRefusal()
        {
            m_slot.KillTweens();
            m_slot.ShakeX(18f, 0.4f).SetLink(gameObject, LinkBehaviour.KillOnDisable).OnComplete(m_slot.ResetTransform);
        }

        #endregion

        [SerializeField]
        private UnityEvent m_onCharacterDataUpdated;
        public Action OnCharacterDataUpdated;
    }
}
