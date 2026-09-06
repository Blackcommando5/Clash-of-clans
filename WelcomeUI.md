# Kingdoms welcome UI

Open `Assets/Scenes/WelcomeScene.unity` and press Play. The welcome screen loads
`Main Scene` asynchronously, displays normalized scene-loading progress, and fades
into the village automatically. A minimum three-second display avoids a startup flash.

Build scene order: WelcomeScene, Main Scene, then the previously enabled SampleScene.
The existing village scene and camera controller are not modified.

All 23 UI objects are serialized, editable Unity Canvas objects. The background is a
RawImage. The title uses the existing Bangers font, with gold face, bevel, and shadow
layers. The loading bar uses native rounded gradient geometry rather than stretched
bitmap borders. Safe-area anchors, a CanvasScaler, best-fit text, and a cover crop
adapt the screen to different display sizes. The tips only describe existing camera
controls and the village theme.

Edit `Welcome Canvas` > `WelcomeScreen` to change the destination scene, minimum
display time, and tips. Change text and layout directly in the hierarchy. Background:
`Assets/UI/Welcome/KingdomsWelcome.png`. Graphic rendering: `WelcomePanel.cs`.
There are no sign-in or multiplayer services in this screen.

Art was created with the built-in imagegen tool. The full generation prompt is saved
in `Assets/UI/Welcome/BackgroundPrompt.txt`. Visual layout reference: the classic
Clash of Clans loading screen, with a Kingdoms title and newly generated art.
Reference: https://www.mobygames.com/game/59402/clash-of-clans/screenshots/ipad/783144/

Validation passed: compilation against the installed Unity assemblies, serialized
reference checks, a real Unity 1920x1080 UI render, and automatic asynchronous scene
activation in an isolated uGUI project using a minimal destination scene. See
WelcomePreview.png in the project root. The open Kingdoms editor session was not
controlled. Physical-phone safe areas and the complete URP village transition still
need device/project Play Mode validation.
