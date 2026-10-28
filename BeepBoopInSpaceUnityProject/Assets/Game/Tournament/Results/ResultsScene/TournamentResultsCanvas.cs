using System;
using System.Collections.Generic;
using System.Linq;
using Game.Global;
using Game.Global.InputIndicationsManagement;
using Game.UITools.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Tournament.Results.ResultsScene
{
    /// <summary>
    /// The tournament's scores between two games. OK takes the focus once the results are set up: the main player's
    /// Submit (A, Cross or Enter) presses it, as does a click.
    /// </summary>
    public class TournamentResultsCanvas : AUIToolkitScreen
    {
        [SerializeField]
        private VisualTreeAsset m_resultLineTemplate;
        [SerializeField]
        private InputIndicationActionDataAsset m_submitIndicationAction;

        private Button m_okButton;
        private Action m_quitCallback;

        public void SetUp(Action quitCallback)
        {
            m_quitCallback = quitCallback;
            Refresh();
        }

        protected override void HandleShown()
        {
            m_okButton = BindButton("ok-button", HandleOkButtonClicked);
            AddIndication("ok-glyph", m_submitIndicationAction);
            Refresh();
        }

        protected override void HandleHidden()
        {
            m_okButton = null;
        }

        private void Refresh()
        {
            if (!IsShown || m_quitCallback == null)
                return;

            var container = Query<VisualElement>("player-lines");
            container.Clear();
            var lines = new List<TournamentPlayerResultLine>();
            foreach (var player in GlobalContext.Instance.PlayerManager.Players)
            {
                var line = new TournamentPlayerResultLine(m_resultLineTemplate, player);
                container.Add(line.Element);
                lines.Add(line);
            }
            var bestScore = lines.Count > 0 ? lines.Max(line => line.Score) : 0;
            foreach (var line in lines.Where(line => bestScore > 0 && line.Score == bestScore))
                line.Highlight();

            DefaultFocus = m_okButton;
            FocusSilently(m_okButton);
        }

        private void HandleOkButtonClicked()
        {
            var quitCallback = m_quitCallback;
            // Once only: the next scene loads.
            m_quitCallback = null;
            DefaultFocus = null;
            m_okButton.SetEnabled(false);
            quitCallback?.Invoke();
        }
    }
}
