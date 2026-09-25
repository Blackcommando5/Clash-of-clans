# Combat rules 6 fixtures

`CombatRules6.json` contains synthetic validation data, captured in the isolated Unity 6000.3.13f1 project on 25 September 2026, before changing the combat code from commit `8f04e7c`. It contains no player save or credentials.

Each of the four authored layouts has a tick-70 campaign checkpoint and its original completed replay/result. The army is eight Raiders, four Archers and four Tanks. Tanks deploy at X -600/-200/200/600, Z -1050 at tick zero; Raiders use cycling lanes at tick zero; Archers use cycling lanes at tick 10. No further commands are issued.

`CrowdSeparationValidation` uses the recorded hashes as an independent compatibility baseline. Keep these fixtures fixed: regenerating them with modified combat code would hide a compatibility regression. Rules-6 replays and resumed attacks must retain their engine version when recorded or saved again.
