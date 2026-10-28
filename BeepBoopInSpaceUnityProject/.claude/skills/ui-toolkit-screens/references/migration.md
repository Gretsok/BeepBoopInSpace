# Migrer un écran uGUI vers UI Toolkit

Fait pour les six écrans du menu principal (commit « Migrating the main menu to UI Toolkit ») : s'en
servir de modèle (`git show a64c370 --stat`).

## Correspondances

| uGUI | UI Toolkit dans ce projet |
| --- | --- |
| Canvas Screen Space Overlay + CanvasScaler 1920×1080 (largeur) | `UIDocument` + `UIPanelSettings` (`UITools/Screens`), le même pour tous les écrans |
| RectTransform ancré | `position: absolute` + `left/top/right/bottom` en px ; centré : `ui-centered-row` ou `translate: -50% 0` |
| Horizontal/Vertical Layout Group, ContentSizeFitter | flex (`flex-direction`, `justify-content`, `align-items`) ; un élément absolu prend la taille de son contenu |
| GridLayoutGroup | conteneur `flex-direction: row; flex-wrap: wrap; justify-content: center` |
| Image (sprite, Preserve Aspect) | `background-image: url("/Assets/....png")` + `-unity-background-scale-mode: scale-to-fit` (`scale-and-crop` pour remplir) |
| Couleur d'Image | `background-color` ; teinte d'un sprite : `-unity-background-image-tint-color` |
| TextMeshProUGUI | `Label` ; police Boogaloo héritée de `.ui-screen` (`-unity-font-definition: url(".../Boogaloo-Regular.ttf")`, une `Font` suffit) |
| Button.onClick (+ son par UnityEvent) | `BindButton(nom, action, audioPlayer)` |
| Transition ColorTint (normal / selected) | `.xxx` / `.xxx:focus` dans le USS |
| PopOnHover (DOTween, échelle 1,1) | `transition-property: scale` + `:focus { scale: 1.1 1.1 }` |
| SelectOnHover | rien : `AUIToolkitScreen` focalise ce que la souris survole |
| `EventSystem.SetSelectedGameObject(x)` | `DefaultFocus = x` ou `FocusSilently(x)` |
| `Selectable.interactable` | `SetEnabled(bool)` + `:disabled` dans le USS |
| CanvasGroup.alpha | `opacity` via une classe (`EnableInClassList`) + `transition-property: opacity` |
| Afficher / cacher un bloc | `style.display = DisplayStyle.Flex / None` |
| ScrollRect + ScrollingToSelectedHandler | `ScrollView` + `ScrollIntoView` (`LevelSelectionScreen`) |
| Slider | `SliderInt fill="true"` ; un volume : gabarit `VolumeRow.uxml` + `VolumeRow` (paramètres, pause) |
| Prefab instancié par élément | gabarit UXML : `template.Instantiate().Q<Button>(...)` puis `Add` (`LevelWidget`), ou `<ui:Template>` + `<ui:Instance>` (`PlayerSlot`) |
| InputIndicationWidget (Image) | `InputIndicationView` sur un `VisualElement` |
| DOTween sur `transform` | `UITools/UIToolkitTweens` (`PunchScale`, `TweenTranslate`...) sur le style de l'élément ; voir [motion.md](motion.md) |
| Canvas World Space, TextMeshPro 3D | à garder tel quel (textes dans la scène 3D, ex. portes du hub) |

## Étapes

1. **Relire l'existant** : `unity_yaml.py tree <prefab> --fields --rect` (mise en page, textes,
   sprites, couleurs des transitions, sons, UnityEvents) ; `overrides <scène>` pour ce que la scène
   change (tailles, textes, objets) ; les champs du script lus par les tests (`grep` dans `Assets/Tests`).
2. **UXML + USS** : un UXML à côté du script, ses styles dans une section de `MainMenu.uss` (menu)
   ou de `Screens.uss` (ailleurs), avec les composants de `Common.uss` (`ui-*`) ; `meta` pour chaque
   nouveau fichier.
3. **Base** : dans le menu principal, hériter d'`AMainMenuScreen`. Ailleurs (partie, pause,
   chargement, tournoi), d'`AUIToolkitScreen` (`UITools/Screens`), qui suit l'activation du
   GameObject. Les tweens de `UIToolkitTweens` tournent en temps non mis à l'échelle : la pause
   (`Time.timeScale = 0`) s'ouvre et se ferme avec eux.
4. **Prefab** : `unity_yaml.py uitk-prefab <prefab> --uxml <uxml> --keep "<lecteurs audio, prefabs imbriqués à garder>"`.
   Il liste les champs qui pointaient vers des objets retirés : les remplacer par les nouveaux
   (`AudioPlayer`, gabarit UXML, indication d'input...) ou les supprimer avec le code. Un champ ajouté
   au YAML se place après `m_EditorClassIdentifier`, qui n'est pas toujours vide : relire avec
   `tree --fields`. Régler `m_SortingOrder` de l'UIDocument. Puis `refs <prefab>` et `clean-overrides`
   sur chaque scène ou prefab qui l'utilise.
5. **Code** : `using UnityEngine.UIElements;` (attention : `Button`, `Image`, `Cursor`, `MouseButton`
   existent aussi ailleurs), éléments liés dans `HandleActivation`, plus aucun `UnityEngine.UI`.
6. **Tests** : `ScenarioDriver.Select(VisualElement)` remplace la navigation par `Selectable` ;
   ajouter l'écran au test des menus avec une capture (écrans de partie :
   `GameScreens_WorkWithGamepadsAndKeyboard`, `CaptureGameScreen` dans une autre scène que le menu).
   Un bouton validé par le Submit du joueur principal passe par le focus : ne plus écouter Submit à
   côté via `NavigationAuthorityManager` (double requête au flow).
7. **Vérifier** (section du SKILL.md) et regarder chaque capture.
