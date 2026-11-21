# Migrer un écran uGUI vers UI Toolkit

Fait pour les six écrans du menu principal (commit « Migrating the main menu to UI Toolkit ») : s'en
servir de modèle (`git show a64c370 --stat`).

## Correspondances

| uGUI | UI Toolkit dans ce projet |
| --- | --- |
| Canvas Screen Space Overlay + CanvasScaler 1920×1080 (largeur) | `UIDocument` + `MainMenuPanelSettings` |
| RectTransform ancré | `position: absolute` + `left/top/right/bottom` en px ; centré : `mm-centered-row` ou `translate: -50% 0` |
| Horizontal/Vertical Layout Group, ContentSizeFitter | flex (`flex-direction`, `justify-content`, `align-items`) ; un élément absolu prend la taille de son contenu |
| GridLayoutGroup | conteneur `flex-direction: row; flex-wrap: wrap; justify-content: center` |
| Image (sprite, Preserve Aspect) | `background-image: url("/Assets/....png")` + `-unity-background-scale-mode: scale-to-fit` (`scale-and-crop` pour remplir) |
| Couleur d'Image | `background-color` ; teinte d'un sprite : `-unity-background-image-tint-color` |
| TextMeshProUGUI | `Label` ; police Magnetar héritée de `.mm-screen` (`-unity-font-definition: url(".../Magnetar.otf")`, une `Font` suffit) |
| Button.onClick (+ son par UnityEvent) | `BindButton(nom, action, audioPlayer)` |
| Transition ColorTint (normal / selected) | `.xxx` / `.xxx:focus` dans le USS |
| PopOnHover (DOTween, échelle 1,1) | `transition-property: scale` + `:focus { scale: 1.1 1.1 }` |
| SelectOnHover | rien : `AMainMenuScreen` focalise ce que la souris survole |
| `EventSystem.SetSelectedGameObject(x)` | `DefaultFocus = x` ou `FocusSilently(x)` |
| `Selectable.interactable` | `SetEnabled(bool)` + `:disabled` dans le USS |
| CanvasGroup.alpha | `opacity` via une classe (`EnableInClassList`) + `transition-property: opacity` |
| Afficher / cacher un bloc | `style.display = DisplayStyle.Flex / None` |
| ScrollRect + ScrollingToSelectedHandler | `ScrollView` + `ScrollIntoView` (`LevelSelectionScreen`) |
| Slider | `SliderInt fill="true"` + `BindVolume` (`MainMenuSettingsScreen`) |
| Prefab instancié par élément | gabarit UXML : `template.Instantiate().Q<Button>(...)` puis `Add` (`LevelWidget`), ou `<ui:Template>` + `<ui:Instance>` (`PlayerSlot`) |
| InputIndicationWidget (Image) | `InputIndicationView` sur un `VisualElement` |
| DOTween sur `transform` | DOTween sur une valeur appliquée à `style.scale` (`CharacterWidget.Pop`) |
| Canvas World Space, TextMeshPro 3D | à garder tel quel (textes dans la scène 3D, ex. portes du hub) |

## Étapes

1. **Relire l'existant** : `unity_yaml.py tree <prefab> --fields --rect` (mise en page, textes,
   sprites, couleurs des transitions, sons, UnityEvents) ; `overrides <scène>` pour ce que la scène
   change (tailles, textes, objets) ; les champs du script lus par les tests (`grep` dans `Assets/Tests`).
2. **UXML + USS** : un UXML à côté du script, ses styles dans une section du USS commun (ou un USS
   par domaine hors du menu), `meta` pour chaque nouveau fichier.
3. **Base** : dans le menu principal, hériter d'`AMainMenuScreen`. Ailleurs (pause, partie), extraire
   d'abord sa mécanique (focus, navigation, panel dans l'EventSystem, sons, Cancel) dans une classe
   de `Game.UITools` dont `AMainMenuScreen` hérite. À vérifier pour la pause : comportement des
   transitions USS avec `Time.timeScale = 0`, et le curseur que `PauseMenuPanel` montre/verrouille.
   Les réglages de volume de la pause : partager la logique de `BindVolume` et un gabarit UXML.
4. **Prefab** : `unity_yaml.py uitk-prefab <prefab> --uxml <uxml> --keep "<lecteurs audio, prefabs imbriqués à garder>"`.
   Il liste les champs qui pointaient vers des objets retirés : les remplacer par les nouveaux
   (`AudioPlayer`...) ou les supprimer avec le code. Puis `refs <prefab>` et `clean-overrides` sur
   chaque scène ou prefab qui l'utilise.
5. **Code** : `using UnityEngine.UIElements;` (attention : `Button`, `Image`, `Cursor`, `MouseButton`
   existent aussi ailleurs), éléments liés dans `HandleActivation`, plus aucun `UnityEngine.UI`.
6. **Tests** : `ScenarioDriver.Select(VisualElement)` remplace la navigation par `Selectable` ;
   ajouter l'écran au test des menus avec une capture.
7. **Vérifier** (section du SKILL.md) et regarder chaque capture.
