# Motion design d'un écran UI Toolkit

Exemple complet : la validation d'un personnage (`CharacterWidget.PlayReady`, transitions
`.cs-slot--ready` de `MainMenu.uss`). Deux outils, à ne pas mélanger sur une même propriété d'un
même élément.

## Transitions USS : les changements d'état

Un état = une classe (`EnableInClassList`), le USS dit l'aspect de chaque état et `transition-*`
anime le passage. Pour : apparitions, disparitions, changements de couleur ou de taille qui
dépendent d'un état stable.

- Durées et courbes différentes à l'aller et au retour : les mettre dans la règle de l'état d'arrivée
  (`.cs-slot--ready .cs-slot__ready-layer { transition-duration: 0.5s; ease-out-back }`, retour en
  0,15 s `ease-in-sine` dans la règle de base). Effet « tampon » : départ `scale: 1.35` + `rotate: -45deg`
  + `opacity: 0`, arrivée en `ease-out-back`.
- Couleurs qui viennent des données (couleur d'un personnage) : les poser **en style inline** ; un
  changement inline déclenche aussi la transition. La teinte d'un sprite
  (`-unity-background-image-tint-color`) se transitionne, alpha compris.
- Une transition ne part que d'un état déjà calculé : un élément créé et changé dans la même frame
  saute à l'arrivée. Créer les éléments à l'ouverture de l'écran, puis changer leurs classes.
- `transform-origin: center bottom` pour un écrasement qui garde les pieds au sol.

## Tweens DOTween : les gestes ponctuels

`Assets/Game/UITools/UIToolkitTweens.cs` : `TweenScale`, `PunchScale`, `TweenTranslate`, `ShakeX`,
`TweenRotate`, `TweenOpacity`, `ResetTransform`, `KillTweens`. Pour : punch, tremblement, saut,
séquences, particules — ce que le USS ne sait pas enchaîner.

- Un tween écrit le **style inline**, qui l'emporte sur le USS : tweener un élément que le USS
  n'anime pas (un enveloppe, l'image, un élément temporaire), et rendre la main en fin de geste
  (`OnComplete(element.ResetTransform)`), sinon l'état suivant du USS ne s'applique plus.
- Tous les tweens ciblent leur élément (`KillTweens` les arrête et remet le transform), tournent en
  temps non mis à l'échelle et prennent `.SetLink(gameObject, LinkBehaviour.KillOnDisable)` pour
  mourir avec l'écran.
- Éléments temporaires (flash, onde de choc, confettis) : les retirer dans **`OnKill`**, pas
  `OnComplete`, et vider leur conteneur au geste suivant (`ClearBurst`) : un tween interrompu ne
  laisse ainsi aucun débris à l'écran. Tuer depuis une copie de la liste d'enfants (le `OnKill` les
  retire pendant l'itération).
- Un geste différé qui en remplace un autre : `DOVirtual.DelayedCall(delay, () => { KillTweens();
  ... })` plutôt que `KillTweens()` tout de suite (on couperait le geste en cours).
- Ne pas déclarer `OnEnable`, `OnDisable` ni `Update` dans un écran (base `AUIToolkitScreen`) : la base a les
  siens en `private`, Unity n'appellerait que celui de l'écran. Un délai : `DOVirtual.DelayedCall`.

## Vérifier une animation : la planche de film

Une capture ne montre que l'état final. Pour voir le geste, un test **temporaire** enregistre des
images du panel pendant l'animation et les monte en planche (5 colonnes, une image toutes les
~60 ms) dans `Logs/screenshots/`. Le retirer ensuite (fichier et `.meta`), et supprimer un
`Assets/InitTestScene<guid>.unity` resté d'un run interrompu.

```csharp
// Assets/Tests/PlayMode/TmpMotionFilmstrip.cs, à supprimer après usage.
[UnityTest, Timeout(5 * 60 * 1000)]
public IEnumerator TmpFilmstrip()
{
    LogAssert.ignoreFailingMessages = true;
    var driver = new ScenarioDriver();
    driver.AddGamepads();
    try
    {
        if (!IsOnMainMenuHome)
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        yield return driver.WaitUntil("the home screen", () => IsOnMainMenuHome && FocusedElement != null);
        StartRenderingToTextures();
        yield return RealSeconds(1f);           // sans ce délai, le Submit suivant se perd
        // ... amener l'écran juste avant le geste ...
        var ui = (RenderTexture)typeof(ScenarioDriver)
            .GetField("s_uiTexture", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        var frames = new List<(float time, Texture2D image)>();
        var start = Time.realtimeSinceStartup;
        Set(driver.Gamepads[0].buttonNorth, 1f); // déclenche le geste
        while (Time.realtimeSinceStartup - start < 0.9f)
        {
            yield return null;                    // jamais WaitForEndOfFrame : il ne revient pas en batchmode
            if (frames.Count == 2)
                Set(driver.Gamepads[0].buttonNorth, 0f);
            RenderTexture.active = ui;
            var image = new Texture2D(600, 620, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(420, 1080 - 740, 600, 620), 0, 0); // y de la texture vers le haut
            image.Apply();
            RenderTexture.active = null;
            frames.Add((Time.realtimeSinceStartup - start, image));
        }
        // ... choisir 15 images à intervalles réguliers, les coller dans une Texture2D, EncodeToPNG ...
    }
    finally
    {
        StopRenderingToTextures();
        driver.RemoveGamepads();
    }
}
```

`unity_cli.py test --filter TmpFilmstrip --name filmstrip`, puis lire la planche. Ce qu'on y a
vérifié : le tampon qui tourne en arrivant, le flash à l'impact, le saut du robot, le punch de la
pastille, l'onde de choc et les confettis qui s'éteignent, puis plus aucun reste à l'écran.
