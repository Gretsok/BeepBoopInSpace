using Game.UITools.Screens;

namespace Game.Gameplay.Scores
{
    /// <summary>
    /// An announcement between two phases of play (new controls, new round) that uses its free space for the players'
    /// scores, hidden the rest of the time: the score-board element of its UXML (<see cref="ScoreBoard"/>).
    /// </summary>
    public class ScoreBannerScreen : BannerScreen
    {
        private ScoreBoard m_scoreBoard;

        protected override void HandleShown()
        {
            base.HandleShown();
            m_scoreBoard = ScoreBoard.Show(Root, gameObject);
        }

        protected override void HandleHidden()
        {
            m_scoreBoard = null;
            base.HandleHidden();
        }

        protected override void HandleUpdate()
        {
            base.HandleUpdate();
            m_scoreBoard?.Refresh();
        }
    }
}
