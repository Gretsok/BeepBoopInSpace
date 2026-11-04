---
name: scripted-edits
description: Modifier beaucoup de fichiers de Beep Boop dans l'espace par script sans casser leurs fins de ligne ni leur BOM — remplacements exacts tolérants aux espaces de fin de ligne (textedit.py), conversion CRLF/LF des nouveaux .cs, contrôle des fins de ligne avant un commit, mise en index d'une partie seulement d'un fichier, et pièges des outils (Write qui retire les espaces de fin, heredoc bloqué par un cat, Edit qui exige un Read). À utiliser dès qu'un changement touche plus de trois ou quatre fichiers, qu'on écrit un script Python qui modifie du C#, de l'USS, de l'UXML ou un skill, qu'un remplacement « n'est pas trouvé » alors que le texte a l'air identique, que git diff montre un fichier modifié en entier, ou qu'il faut commiter séparément deux sujets mêlés dans un fichier.
---

# Modifications par script

But : modifier des dizaines de fichiers d'un coup, vite, sans réécrire leurs fins de ligne et sans
remplacement raté en silence. Vécu le 04/11/2026 (jeu en ligne : ~50 fichiers C#, USS, UXML, YAML).

## Quel outil

| Cas | Outil |
| --- | --- |
| Un ou deux fichiers déjà lus dans la session | Outil **Edit** (garde les fins de ligne ; exige un Read du fichier avant) |
| Plusieurs fichiers, ou des fichiers pas encore lus | Un script Python écrit avec **Write**, qui appelle `textedit.edit` |
| Nouveau fichier | **Write**, puis `textedit.py eol --fix` (un `.cs` neuf en CRLF, comme la plupart du projet) |
| Scènes, prefabs, `.asset`, `.meta` | Skill `unity-yaml` (`set-field`, `add-child`, `add-instance`...), pas de remplacement texte |

## textedit.py

```python
# script.py, écrit avec Write, lancé depuis le dossier du projet Unity : python script.py
import sys; sys.path.insert(0, ".claude/skills/scripted-edits/scripts")
from textedit import edit
edit("Assets/Game/Gameplay/Timer/TimerManager.cs", [
    ("""        public void ResetTimer()""",
     """        public void SetTimeLeft(float timeLeft)
        {
            TimeLeft = timeLeft;
        }

        public void ResetTimer()"""),
])
```

- Chaque ancien texte doit être trouvé **une seule fois** (sinon arrêt, rien n'est écrit dans ce
  fichier) ; `count=0` remplace partout. Les paires s'appliquent dans l'ordre : une paire voit le texte
  déjà modifié par les précédentes.
- **Les lignes du motif tolèrent les espaces de fin** : l'outil Write les retire du script, les
  fichiers du projet en ont (lignes vides indentées des `.cs`). Sans ça, « Not found » sur un texte
  identique à l'œil.
- Le fichier garde son BOM et ses fins de ligne (CRLF ou LF), quoi qu'écrive le script.
- Un échec arrête le script : les fichiers des appels précédents sont déjà modifiés. Relancer après
  correction sans rejouer ce qui est passé (ou découper en un script par fichier).

```bash
T=.claude/skills/scripted-edits/scripts/textedit.py
python $T eol            # fichiers modifiés ou nouveaux dont les fins de ligne diffèrent de HEAD
python $T eol --fix      # les remet (nouveaux .cs en CRLF, le reste en LF)
python $T stage <chemin> <fichier de contenu>   # indexe ce contenu pour <chemin>, copie de travail intacte
```

`stage` sert à commiter un sujet d'un fichier qui en mêle deux : construire le contenu voulu (HEAD +
les changements du sujet, `git show HEAD:./<chemin>`), l'indexer, commiter, puis le reste du fichier
part dans le commit suivant (fait pour le test du hub et ceux du jeu en ligne). Vérifier avec
`git diff --cached -- <chemin>`.

## Pièges des outils

- **Un `cat > fichier` avant un heredoc qui appartient à une autre commande attend l'entrée
  standard jusqu'au délai** (2 min perdues). Écrire les fichiers avec Write ; dans Bash, un heredoc
  seulement pour la commande qu'il alimente (`python - <<'EOF' ... EOF`).
- **Plusieurs lignes dans un appel Bash ne s'arrêtent pas au premier échec** : `a && b` sur une ligne,
  mais la ligne suivante s'exécute quand même. Une étape qui dépend d'une autre : sur la même ligne
  avec `&&`, ou dans le même script Python.
- Dans une chaîne Python d'un heredoc, `\\n` et `\n` ne donnent pas la même chose selon le niveau
  d'échappement voulu dans le fichier cible : quand le motif contient des séquences d'échappement
  (code Python ou C# qui écrit `\n`), passer par l'outil Edit ou par un fichier de script écrit avec
  Write, relu avant de le lancer.
- `sed -i` de Git Bash retire les `\r` d'un `.cs` : il convient seulement à un fichier LF.
- Un fichier entièrement réécrit avec Write passe en LF : `eol` le signale, `eol --fix` le remet.
- Un fichier modifié pendant la session par un autre outil (Python, Unity) doit être relu avant
  l'outil Edit (« changed on disk ») ; `textedit` n'a pas cette contrainte.
- Après une série de modifications de C# : `unity_cli.py quick-compile` (skill `unity-headless`)
  avant toute autre étape.
