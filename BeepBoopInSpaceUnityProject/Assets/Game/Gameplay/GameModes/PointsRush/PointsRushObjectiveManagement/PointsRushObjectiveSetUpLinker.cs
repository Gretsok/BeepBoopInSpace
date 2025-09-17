using Game.Gameplay.Flows.NewRoundAnnouncement;
using Game.Gameplay.GameModes.PointsRush.ObjectiveManagement;
using UnityEngine;

namespace Game.Gameplay.GameModes.PointsRush.PointsRushObjectiveManagement
{
    [RequireComponent(typeof(SetUpNewRoundState))]
    public class PointsRushObjectiveSetUpLinker : MonoBehaviour
    {
        private SetUpNewRoundState m_state;

        private void Awake()
        {
            m_state = GetComponent<SetUpNewRoundState>();
            m_state.OnTimeToSetUpDependencies += HandleTimeToSetUpDependencies;
        }

        private void HandleTimeToSetUpDependencies()
        {
            PointsRushObjectiveManager.Instance.SetUp();
        }
    }
}
