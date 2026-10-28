# Formats YAML utiles (Unity 6000.6)

## Documents

`--- !u!<classe> &<fileID>` puis le corps ; ` stripped` après le fileID = objet d'une instance de
prefab que le fichier référence (seulement `m_CorrespondingSourceObject` + `m_PrefabInstance`).

| Classe | Type | | Classe | Type |
| --- | --- | --- | --- | --- |
| 1 | GameObject | | 223 | Canvas |
| 4 | Transform | | 224 | RectTransform |
| 114 | MonoBehaviour (script, ScriptableObject) | | 225 | CanvasGroup |
| 222 | CanvasRenderer | | 1001 | PrefabInstance |
| 82 | AudioSource | | 1660057539 | SceneRoots (ordre des racines d'une scène) |

## Références

- Même fichier : `{fileID: 123}` ; aucun : `{fileID: 0}`.
- Autre asset : `{fileID: <objet>, guid: <guid du .meta>, type: 2}` (asset natif : `.asset`, `.mat`)
  ou `type: 3` (importé : prefab, texture, UXML, USS, audio...).
- Objet principal : ScriptableObject `11400000`, prefab `100100000`, matériau `2100000`,
  texture `2800000`, sprite d'une texture en mode Single `21300000`, audio `8300000`,
  police `.otf/.ttf` `12800000`, UXML `9197481963319205126`, USS `7433441132597879392`,
  thème TSS `-4733365628477956816`.
- Script : `m_Script: {fileID: 11500000, guid: <guid du .cs>, type: 3}`. Scripts du moteur :
  `guid: 0000000000000000e000000000000000, type: 0` avec `fileID` `19101` (PanelSettings) ou
  `19102` (UIDocument).

## Objets de prefabs imbriqués

fileID implicite = `(fileID de la PrefabInstance ^ fileID de l'objet dans le prefab source) & 0x7FFFFFFFFFFFFFFF`
(le bit de signe tombe : la source peut être le négatif correspondant). Une surcharge les vise par ce
fileID dans le prefab parent.

## Surcharge (dans `m_Modification.m_Modifications` d'une PrefabInstance)

```yaml
    - target: {fileID: <objet dans le prefab source>, guid: <guid du prefab source>, type: 3}
      propertyPath: m_IsActive
      value: 0
      objectReference: {fileID: 0}
```

## UIDocument (composant sur la racine d'un écran)

```yaml
--- !u!114 &<fileID>
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: <GameObject racine>}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 19102, guid: 0000000000000000e000000000000000, type: 0}
  m_Name: 
  m_EditorClassIdentifier: 
  m_PanelSettings: {fileID: 11400000, guid: <guid du PanelSettings>, type: 2}
  m_ParentUI: {fileID: 0}
  sourceAsset: {fileID: 9197481963319205126, guid: <guid de l'UXML>, type: 3}
  m_SortingOrder: 0
```

Ajouter son fileID à `m_Component` du GameObject. `uitk-prefab` le fait.

## PanelSettings

Copier `Assets/Game/UITools/Screens/UIPanelSettings.asset` (+ `meta` pour un nouveau guid) :
`m_ScaleMode: 2` = ScaleWithScreenSize, `m_ReferenceResolution`, `m_Match: 0` = suit la largeur,
`themeUss` = le TSS, `m_SortingOrder` (comparé aux autres panels).

## .meta

`unity_yaml.py meta` les écrit. Importeurs : UXML `ScriptedImporter` script `13804`, USS `12385`,
TSS `12388` (`guid: 0000000000000000e000000000000000, type: 0`), `.asset` `NativeFormatImporter`
(`mainObjectFileID: 11400000`), prefab `PrefabImporter`, scène et dossier `DefaultImporter`
(`folderAsset: yes` pour un dossier), `.cs` : deux lignes seulement (`fileFormatVersion`, `guid`)
dans cette version. Textures, modèles, sons : réglages d'import propres à chaque fichier, laisser
Unity créer le `.meta` (puis relire le guid avec `guid <chemin>`).

## Ajouter un GameObject enfant à un prefab

Fait pour les lecteurs audio de `CharacterSelectionScreen.prefab` (`ReadyAudioPlayer`...). Trois
documents, trois fileID neufs (aléatoires, absents du fichier) : `GO`, `RT`, `MB`.

```yaml
--- !u!1 &<GO>
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: <RT>}
  - component: {fileID: <MB>}
  m_Layer: 0
  m_Name: ReadyAudioPlayer
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &<RT>
RectTransform:            # le même type de transform que les frères (224 sous un RectTransform)
  ...                     # copier celui d'un frère, avec m_GameObject: {fileID: <GO>}
  m_Children: []
  m_Father: {fileID: <transform du parent>}
--- !u!114 &<MB>
MonoBehaviour:
  ...                     # m_GameObject: {fileID: <GO>}, m_Script du composant, ses champs
```

Puis ajouter `- {fileID: <RT>}` à `m_Children` du transform parent, et référencer `<MB>` dans le
champ du script qui l'utilise (`m_readyAudioPlayer: {fileID: <MB>}`). Vérifier avec
`tree <prefab> --fields` : l'objet apparaît sous son parent et le champ affiche `→ReadyAudioPlayer/AudioPlayer`.
Les instances du prefab dans les scènes le reçoivent sans rien toucher.
