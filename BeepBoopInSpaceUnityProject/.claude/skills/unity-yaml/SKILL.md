---
name: unity-yaml
description: Lire et modifier sans l'éditeur les fichiers YAML de Unity de Beep Boop dans l'espace (scènes .unity, prefabs, .asset, .meta) — arbre d'un prefab ou d'une scène avec composants et références résolues, surcharges des instances de prefab avec l'objet visé (prefabs imbriqués compris), qui référence un asset, GUID ↔ chemin, création des .meta de nouveaux fichiers, nettoyage des surcharges orphelines, conversion d'un prefab d'écran uGUI en écran UIDocument. À utiliser pour comprendre une scène ou un prefab avant d'y toucher, avant de supprimer, renommer ou déplacer un asset, pour créer un asset ou un .meta à la main, et pour toute modification de .unity/.prefab/.asset faite hors de l'éditeur.
---

# YAML de Unity sans l'éditeur

But : comprendre et modifier scènes, prefabs et assets en quelques secondes, sans ouvrir Unity ni
écrire un script jetable à chaque fois, et sans casser les références.

```bash
Y=".claude/skills/unity-yaml/scripts/unity_yaml.py"   # depuis BeepBoopInSpaceUnityProject/
```

| Commande | Rôle |
| --- | --- |
| `tree <asset> [--fields] [--rect]` | Hiérarchie : objets (inactifs signalés), composants (texte TMP, sprite, UXML d'un UIDocument, clip audio), instances de prefab. `--fields` : tous les champs sérialisés, références résolues (`→Objet/Composant`, `→fichier.asset`). `--rect` : ancres, position, taille des RectTransform. Marche aussi sur un `.asset` (ScriptableObject) |
| `overrides <asset> [--instance NOM]` | Surcharges de chaque instance de prefab : objet visé dans le prefab source (`Instance:Objet/Composant` pour un prefab imbriqué), propriété, valeur. `[MISSING id]` = la cible n'existe plus |
| `refs <chemin ou guid>` | Fichiers d'`Assets/` et de `ProjectSettings/` qui référencent l'asset, avec l'objet et le champ |
| `guid <guid ou chemin>` | Chemin d'un guid (Assets et packages), ou guid d'un asset |
| `meta <fichier>... [--guid G]` | Crée le `.meta` de fichiers neufs : dossier, `.cs`, `.uxml`, `.uss`, `.tss`, `.prefab`, `.unity`, `.asset`, `.mat`, textes. Affiche le guid à utiliser dans les références |
| `clean-overrides <asset>... [--dry-run]` | Retire les surcharges dont la cible n'existe plus (après avoir vidé un prefab) |
| `uitk-prefab <prefab> --uxml <f> [--keep NOMS] [--panel-settings <asset>] [--dry-run]` | Écran uGUI → écran UI Toolkit : retire Canvas, CanvasScaler, GraphicRaycaster, CanvasGroup et tous les enfants sauf `--keep` (objets ou prefabs imbriqués, par nom, remontés sous la racine, leurs composants uGUI retirés), ajoute un `UIDocument`. Liste les champs qui pointaient vers des objets retirés |

Le premier appel indexe les `.meta` des packages (~10 s, cache dans `Library/claude-guid-index.json`) ;
`Assets/` est relu à chaque appel.

## Règles

- **Éditeur fermé** pour écrire : un éditeur ouvert ne verrait pas le changement et pourrait
  réenregistrer sa version par-dessus. Lire est toujours possible.
- **Fins de ligne** : le dépôt stocke le YAML en LF (la copie de travail peut être en CRLF). L'outil
  écrit en LF. En Python, écrire avec `write_bytes` : `write_text` sous Windows transforme `\n` en
  `\r\n`. `sed -i` de Git Bash retire les `\r` des `.cs` (qui sont en CRLF) : passer par l'outil Edit.
- **Garder les fileID** que d'autres fichiers référencent : racine du prefab, son transform, les
  composants visés par la scène (`refs` et les objets `stripped` de la scène le disent). Un prefab
  réécrit garde ses fileID, la scène continue de pointer au bon endroit.
- **Ne pas changer la classe** d'un objet référencé (RectTransform `224` → Transform `4`) : la scène
  le référence comme `--- !u!224 ... stripped`.
- Un `.meta` voyage avec son fichier ; supprimer un asset = vérifier d'abord `refs` (les groupes
  Addressables et le code qui charge par nom ne sont pas vus).
- Après une modification : `unity_cli.py compile` (skill `unity-headless`) importe tout et signale
  les erreurs d'import, puis les tests concernés.

## Faire une modification que l'outil ne fait pas

Lire d'abord `tree --fields` et `overrides`, puis éditer les documents YAML (un document =
`--- !u!<classe> &<fileID>`). Formats utiles : [references/formats.md](references/formats.md)
(classes, fileID des scripts intégrés, UIDocument, PanelSettings, références, formule des fileID des
objets de prefabs imbriqués, bloc de surcharge). Pour un nouvel objet, prendre un fileID aléatoire
non utilisé dans le fichier ; ajouter un enfant à un prefab (ex. un `AudioPlayer`) : voir
« Ajouter un GameObject enfant » dans `formats.md`.

## Pièges

- Les objets d'un prefab imbriqué n'ont pas de document dans le fichier parent : leur fileID est
  `(fileID de l'instance ^ fileID dans le prefab source) & 0x7FFFFFFFFFFFFFFF`. `overrides` et
  `clean-overrides` le calculent ; ne pas les croire orphelins parce qu'on ne les trouve pas par grep.
- Une surcharge sur un **champ** disparu (cible présente) n'est pas signalée : Unity l'ignore. En
  retirant un champ sérialisé d'un script, supprimer aussi sa ligne dans les prefabs et assets, et
  ses surcharges (`propertyPath: <Champ>k__BackingField` ou `m_champ`) dans les scènes : `grep`,
  puis retirer chaque bloc `- target ... objectReference` entier.
- `m_IsActive: 0` en surcharge sur la racine d'une instance = objet inactif au chargement de la scène
  (c'est le cas des écrans du menu, activés par leur état).
