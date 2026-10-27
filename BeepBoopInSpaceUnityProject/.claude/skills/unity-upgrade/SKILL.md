---
name: unity-upgrade
description: Mettre à jour la version de Unity ou les packages de Beep Boop dans l'espace et corriger tout ce que ça casse — packages du registre, dépendances git (UniTask, NaughtyAttributes), packages OpenUPM (GameAnalytics), code tiers dans Assets (Pinwheel Contour, DOTween), API obsolètes de Unity 6, URP, .meta à ré-écrire. À utiliser quand l'utilisateur dit « j'ai changé la version de Unity », « passe en 6.x », « mets à jour les packages », quand le projet s'ouvre en Safe Mode ou avec des erreurs après une mise à jour, ou quand une erreur de compilation vient de Library/PackageCache.
---

# Mettre à jour Unity ou les packages

But : un projet qui compile sans erreur ni warning et dont les scénarios passent, dans la
nouvelle version. Procédure suivie pour le passage de 6000.3.9f1 à 6000.6.7f1 (commit
`6d654f3`, « Upgrading to Unity 6000.6.7f1 »).

Outil commun : `python .claude/skills/unity-headless/scripts/unity_cli.py <commande>`.

## 1. Faire l'état des lieux

```bash
cat ProjectSettings/ProjectVersion.txt
git diff --stat; git diff Packages/manifest.json
python .claude/skills/unity-headless/scripts/unity_cli.py status
```

- La version voulue doit être installée (Unity Hub) : `status` liste celles qui le sont.
- `ProjectVersion.txt` n'est ré-écrit qu'à la première ouverture dans la nouvelle version. Pour la
  faire en headless avant : `UNITY_EXE="C:/Program Files/Unity/Hub/Editor/<version>/Editor/Unity.exe"`
  devant la commande `compile` (Unity met le projet à jour sans demander en batchmode).

## 2. Récupérer et classer les erreurs

`compile`, puis `log-errors` sur son log (ou sur `Logs/Editor.log` si l'utilisateur a déjà ouvert le
projet). Classer par origine :

| Origine (chemin de l'erreur) | Correction |
| --- | --- |
| `Library/PackageCache/<package>@...` | Mettre le package à jour (étape 3). Ne jamais modifier le cache, il est régénéré |
| `Assets/PinwheelStudio`, `Assets/Plugins/...` (code tiers) | Patch minimal (étape 4) |
| `Assets/Game/...`, `Assets/Tests/...` | Corriger le code du projet (étape 5) |
| « Serialized file ... below the supported minimum » | Ré-écrire le `.meta` au format attendu (étape 6) |

Une erreur dans un assembly cache les erreurs des assemblies qui en dépendent : refaire `compile`
après chaque série de corrections, de nouvelles erreurs peuvent apparaître.

## 3. Packages

- **Registre Unity / OpenUPM** : chercher une version plus récente qui corrige l'erreur.
  ```bash
  curl -s https://package.openupm.com/com.gameanalytics.sdk | python -c "import sys,json; d=json.load(sys.stdin); print(d['dist-tags'], sorted(d['versions'])[-5:])"
  curl -s https://packages.unity.com/com.unity.addressables | python -c "import sys,json; print(json.load(sys.stdin)['dist-tags'])"
  ```
  Lire le changelog (breaking changes en version majeure) et vérifier que l'API utilisée par le
  projet n'a pas changé (`grep` des usages dans `Assets/`). Changer la version dans
  `Packages/manifest.json`.
- **Dépendances git sans version figée** (`com.cysharp.unitask`, `com.dbrizov.naughtyattributes`) :
  le commit est figé par `Packages/packages-lock.json`. Vérifier que la branche suivie contient le
  correctif (API GitHub `repos/<owner>/<repo>/commits?path=<fichier>`), puis **supprimer l'entrée
  du package dans `packages-lock.json`** : Unity récupère le dernier commit. `gh` n'est pas
  installé, utiliser `curl https://api.github.com/...` et `raw.githubusercontent.com`.
- Garder le format JSON existant de `packages-lock.json` (indentation 2, pas de ré-ordonnancement).

## 4. Code tiers dans Assets

Patch minimal et réversible : entourer l'ancien code de `#if !UNITY_6000_6_OR_NEWER` (adapter la
version), avec un commentaire qui dit pourquoi, plutôt que le supprimer. Préférer la mise à jour
de l'asset depuis l'Asset Store si l'utilisateur l'a : la proposer, sans la faire soi-même.

## 5. API Unity obsolètes déjà rencontrées

| Ancienne API (erreur CS0619 / warning CS0618) | Remplacement |
| --- | --- |
| `Object.GetInstanceID()`, `EditorUtility.InstanceIDToObject(int)` | `GetEntityId()`, `EditorUtility.EntityIdToObject` |
| `AssetPreview.IsLoadingAssetPreview(int)` | `IsLoadingAssetPreview(EntityId)` |
| `EditorApplication.hierarchyWindowItemOnGUI` | `hierarchyWindowItemByEntityIdOnGUI` |
| `TreeView`, `TreeViewItem`, `TreeViewState` | `TreeView<int>` etc. (alias `using` sous `#if UNITY_6000_2_OR_NEWER`) |
| `FindObjectsByType<T>(..., FindObjectsSortMode)` | `FindObjectsByType<T>(FindObjectsInactive)` |
| `FindFirstObjectByType<T>()` | `FindAnyObjectByType<T>()` |
| `VolumeComponent.displayName = ...` | attribut `[DisplayInfo(name = ...)]` sur la classe |
| URP `ScriptableRenderPass.OnCameraSetup` / `Execute(ScriptableRenderContext, ref RenderingData)` | supprimés en URP 17.6 : seul `RecordRenderGraph` existe |

Vérifier une API dans la nouvelle version plutôt que de deviner : le code des packages est dans
`Library/PackageCache/`, la doc XML du moteur dans
`C:/Program Files/Unity/Hub/Editor/<version>/Editor/Data/Managed/UnityEngine/UnityEngine.CoreModule.xml`.

## 6. Fichiers `.meta` trop anciens

Exemple rencontré : les DLL DOTween avaient un `PluginImporter` en `serializedVersion: 1`. Les
ré-écrire au format actuel en gardant le même `guid` et les mêmes réglages de plateformes (prendre
un `.meta` récent du `Library/PackageCache` comme modèle). Le dépôt stocke les `.meta` en LF :
garder LF pour que le diff ne montre que le vrai changement (`git ls-files --eol <fichier>`).

## 7. Vérifier

1. `quick-compile` : 0 erreur, **0 warning dans Assets** (corriger aussi les nouveaux warnings
   d'obsolescence : ils deviennent des erreurs à la version suivante).
2. `compile` : 0 erreur.
3. Tests de scénario complets (skill `unity-scenario-tests`).
4. Si l'utilisateur partage des builds : un build de contrôle (skill `unity-build`).

## 8. Commit

Un commit « Upgrading to Unity <version> » qui liste les corrections. Il est normal qu'il touche des
centaines de fichiers : à la première ouverture, Unity ré-écrit les `.meta` des textures
(nouveaux champs), les `ProjectSettings/*.asset` (nouveaux réglages, par ex. Android min SDK 25 →
26) et régénère les classes d'input (`PauseControls.cs`). Ne pas les annuler.
