# New-player name entry

The welcome screen loads Main Scene. A first-time player sees a stone-colored
"My name is..." popup over the village, with a name field and green Done button.
Done opens a confirmation view; Edit returns to the field; Confirm saves the name.
The camera stays locked until confirmation. The selected name then appears in a
small Chief nameplate. Returning players skip the popup, including when launching
Main Scene directly in the editor.

Names use 2–15 characters, with letters A–Z, numbers, spaces, hyphens, underscores,
and apostrophes. At least one letter or number is required. Leading/trailing spaces
are trimmed and repeated spaces are collapsed. The InputField also limits entry to
15 characters. Text formatting is disabled for player-provided text.

The profile stores the confirmed name locally with PlayerPrefs under
`Kingdoms.PlayerName.v1`. It does not create an online account or check name uniqueness.
Other game systems can read `Kingdoms.PlayerProfile.PlayerName` and
`Kingdoms.PlayerProfile.HasPlayerName`. No separate tutorial-completed flag can drift
out of sync with the stored name.

`NewPlayerOnboarding` is attached to Main Camera and references the existing camera
controller and fonts. It creates the popup and nameplate at runtime using the existing
WelcomePanel graphic. It uses InputSystemUIInputModule for UI input, handles safe areas
and the reported mobile keyboard area, and restores the camera's original enabled state.
The welcome/loading scene, ground material, and camera movement code are unchanged.

The visual reference is the Clash of Clans name-entry dialog. This implements the
name-entry portion of first-time onboarding, not its entire combat/building tutorial.
Supercell's general tutorial description: https://supercell.com/en/parents/

Validation: compile against the installed Unity assemblies; run an isolated Unity
test for invalid names, first-visit display, camera lock, edit/confirm, persistence
across scene reload, nameplate text, and returning-player skip. The isolated test
uses separate application preferences and a built-in-renderer ground stand-in.
Physical-phone keyboard and touch behavior still require device validation.
