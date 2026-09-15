# Reference UI milestone

Implemented on 15 September 2026 using Coc.jpeg and Coc1.jpeg as layout references.

- Compact gold, elixir and gem bars with saved balances and capacity fills.
- Chief name and Town Hall badge open the profile; the badge represents Town Hall level, not player XP.
- Left navigation and bottom Attack/Shop controls.
- Stone profile frame, four main tabs, three village tabs and slate statistics panels.
- Profile name, building counts and Town Hall level come from the local save.
- Building inventory links to the working resource shop.
- Settings includes manual saving and camera instructions.
- Modal screens block village input and restore it on close.

## Not yet implemented

The Attack, clan, social, Builder Base and Clan Capital sections explain their availability; combat and networking are not implemented. No fake trophies, clan membership or troop collection are shown. Dark elixir and XP remain absent because those economies do not yet exist.

This is a layout and interaction milestone. Native geometric icons and existing building previews remain interim artwork. It is not a pixel-exact reproduction of the supplied screenshots. Detailed illustrated icons, full building models, terrain, troop portraits and animations require subsequent art work.

## Verification

The isolated Unity runner exercises profile opening, saved-name display, tab changes, camera locking and the inventory-to-shop transition alongside existing economy, placement, upgrade and reload checks. It captures 1600 x 702 village/profile previews. The installed Android APK must be rebuilt separately.

### Result for this pass

The first run passed 66 economy/progression assertions and runtime profile/navigation checks. Its screenshots exposed an old nameplate overlap and portrait/caption overlap; both were corrected in source. Subsequent runs compiled the corrections but timed out before completing the runtime checks, alongside the recurring UnityEditor.Search startup exception. The cause of the timeout is not conclusively established. Fresh corrected screenshots and a complete rerun remain outstanding; earlier screenshots do not represent the final layout. Test setup now flushes deleted test preferences before a domain reload.
