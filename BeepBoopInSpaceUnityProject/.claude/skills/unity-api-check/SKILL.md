---
name: unity-api-check
description: Vérifier une API de Unity dans la version exacte qu'utilise Beep Boop dans l'espace (6000.6.7f1, plus récente que ce que le modèle connaît) — existence et documentation d'un type ou d'un membre (docs XML livrées avec l'éditeur), source C# des packages (uGUI dont l'intégration UI Toolkit/EventSystem, Input System, Cinemachine, URP...) dans Library/PackageCache, et code du moteur via UnityCsReference. À utiliser avant d'écrire du code sur une API dont on n'est pas sûr (UI Toolkit, Input System, nouveautés Unity 6), quand quick-compile signale un membre absent ou obsolète, ou pour comprendre un comportement de Unity (focus, navigation, EventSystem, événements) au lieu de le deviner.
---

# Vérifier une API de Unity

But : ne pas écrire de code sur une API supposée. La version du projet peut avoir ajouté, renommé
ou rendu obsolète ce que l'on croit connaître (ex. : `PanelRenderer` apparu à côté de `UIDocument`,
`FindObjectsByType(..., FindObjectsSortMode)` obsolète en 6000.6).

```bash
A=".claude/skills/unity-api-check/scripts/unity_api.py"   # depuis BeepBoopInSpaceUnityProject/
python $A find 'FocusController\.IgnoreEvent|PanelSettings\.sortingOrder'   # membres par regex (< 1 s)
python $A type BaseSlider                  # un type et tous ses membres documentés
python $A source PanelEventHandler         # où un type est déclaré dans les packages (< 1 s, ~15 s si le fichier n'a pas son nom)
python $A version
```

## Ordre de vérification

1. **Existe-t-il dans cette version ?** `find` / `type` lisent les docs XML de l'éditeur installé
   (version de `ProjectSettings/ProjectVersion.txt`). Listé = existe. Absent = pas documenté : peut
   être interne ou inexistant.
2. **Comment se comporte-t-il ?** Si le type est dans un package, `source` donne le fichier : le
   lire (ex. `PanelEventHandler`, `InputSystemUIInputModule`, `UIToolkitInteroperabilityBridge`,
   `CinemachineBrain`). Sinon c'est du code moteur : `https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/master/<chemin>`
   (ex. `Modules/UIElements/Core/GameObjects/UIDocument.cs`, `Modules/UIElements/Core/Controls/BaseSlider.cs`) ;
   la branche `master` peut être plus récente que le projet.
3. **Le code compile-t-il sans warning ?** `unity_cli.py quick-compile` (skill `unity-headless`)
   tranche : membre absent, obsolète (`CS0618`), ambigu (`MouseButton` existe dans
   `UnityEngine.InputSystem.LowLevel` et `UnityEngine.UIElements`).

## Comportements déjà vérifiés (6000.6.7f1)

- `UIDocument` : désactiver son GameObject détruit l'arbre ; le réactiver reclone l'UXML.
- UI Toolkit + EventSystem : la navigation (Move/Submit/Cancel) va au GameObject sélectionné de
  l'EventSystem ; pour un panel, c'est un GameObject créé par `UIToolkitInteroperabilityBridge`
  une frame après l'apparition du panel (sous l'EventSystem, nommé comme le PanelSettings).
- `NavigationMoveEvent` : la navigation directionnelle native ne cherche que droit devant.
  `FocusController.IgnoreEvent(evt)` + `StopPropagation()` l'empêchent de déplacer le focus.
- `BaseSlider` : gauche/droite ne changent la valeur qu'après un Submit (mode « movable »).
- `VisualElement.rect` est interne : `new Rect(Vector2.zero, element.layout.size)`.
- Input System en batchmode : clavier et souris ignorés tant que `InputSystem.settings.backgroundBehavior`
  n'est pas `IgnoreFocus` (et, dans l'éditeur, `editorInputBehaviorInPlayMode` à
  `AllDeviceInputAlwaysGoesToGameView`). `MouseState.WithButton` modifie la structure appelée.
- `UIDocument` a `[DefaultExecutionOrder(-100)]` : son `OnEnable` (qui reclone l'arbre) passe avant
  celui des scripts du même GameObject, qui y trouvent donc l'arbre (`AUIToolkitScreen`). Un
  UIDocument enfant du GameObject d'un autre UIDocument s'attache à l'arbre du parent (`SetupFromHierarchy`).
- `UIDocument.sortingOrder` ordonne les documents d'un même PanelSettings (le plus grand devant).
- USS : `@import url("/Assets/...");` en tête de fichier marche (chemin du projet) ; pas de
  `text-transform` (aucun `IStyle.textTransform`) : capitales écrites dans l'UXML ou `ToUpperInvariant`.
- UXML : `<ui:Template src="../chemin.uxml">` relatif, et `<ui:AttributeOverrides element-name="label"
  text="..." />` dans un `<ui:Instance>` pour changer un attribut d'un élément du gabarit.
- `RuntimePanelUtils.CameraTransformWorldToPanel(panel, position, camera)` : un point du monde en
  coordonnées du panel (le « +1 » au-dessus d'une case).
- Input System, clavier : seule Entrée a l'usage `Submit` (Espace ne valide pas l'UI) ; Échap a
  l'usage `Cancel`.
- Les panels UI Toolkit et les canvas uGUI « Screen Space - Overlay » sont triés ensemble par
  ordre de tri (`PanelSettings.sortingOrder` / `Canvas.sortingOrder`) d'après la doc et les forums :
  **pas vérifié à l'écran** dans ce projet.
