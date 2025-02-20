using Game.Gameplay.CharactersManagement;
using TMPro;
using UnityEngine;

namespace Game.Gameplay.Flows.Results
{
    public class ResultsPanel : MonoBehaviour
    {
        [SerializeField] 
        private TMP_Text m_text;

        private void OnEnable()
        {
            var charactersManager = CharactersManager.Instance;

            string content = "";

            foreach (var pawn in charactersManager.CharacterPawns)
            {
                content += $"{pawn.CharacterData.Name} : {pawn.Score} points\n";
            }
            m_text.text = content;
        }
    }
}