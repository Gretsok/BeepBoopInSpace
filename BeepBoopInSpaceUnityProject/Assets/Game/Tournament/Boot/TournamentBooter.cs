using UnityEngine;

namespace Game.Tournament.Boot
{
    public class TournamentBooter : MonoBehaviour
    {
        private void Start()
        {
            TournamentContext.RegisterPostInitializationCallback(context =>
            {
                context.FlowManager.StartTournament();
            });
        }
    }
}
