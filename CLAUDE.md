# CLAUDE.md — Skin in the Game

Lees ook `SPEC-architectuur.md`. Dat is de bron van waarheid voor ontwerp en bouwvolgorde.

## Over de gebruiker

- Heeft nog nooit geprogrammeerd. Leg uit in gewoon Nederlands, zonder jargon, en kort.
- Werkt een paar uur per week. Houd elke taak klein genoeg voor één avond.
- Doet zelf alles in de Unity-editor (scenes, prefabs, componenten koppelen). Geef daarvoor een genummerde klik-voor-klik-lijst.
- Communiceer in het Nederlands. Code, commentaar, bestandsnamen en alle tekst in de game zijn in het Engels.

## Project

- Unity 6 LTS, Universal Render Pipeline. Update de Unity-versie niet zonder te vragen.
- Netcode for GameObjects, Unity Transport, Unity Relay, Unity Authentication (anoniem), Vivox.
- 2 tot 4 spelers, host op eigen pc, verbinden via joincode.
- Alleen gratis packages en assets. Voeg nooit een betaald asset of package toe.

## Architectuurregels

1. **Core is pure C#.** Alles in `Assets/_Project/Scripts/Core` gebruikt geen `UnityEngine`. De assembly heeft "No Engine References" aan. Spelregels, geld, inzetten, lichaamsdelen en minigame-logica horen hier.
2. **Afhankelijkheden lopen naar binnen.** Presentation kent Game, Game kent Core en Services, Core kent niemand.
3. **De host beslist over geld en uitkomsten.** Clients sturen alleen verzoeken via RPC. Een client verandert nooit zelf gedeelde toestand.
4. **Beweging is van de speler zelf.** Positie en kijkrichting zijn owner-authoritative.
5. **Geen `UnityEngine.Random` in spellogica.** Gebruik altijd `SeededRng`.
6. **Getallen in `GameBalance`.** Geen losse magic numbers voor quota, inzetten of tijden.
7. **Services achter een interface.** `INetSession`, `IVoiceService`, `ISaveService`. Game-code roept nooit direct Relay, Vivox of bestanden aan.
8. **Visuele effecten zijn lokaal.** Wiebel, blur, animaties en geluid worden niet gesynchroniseerd, ze reageren op gesynchroniseerde toestand.
9. **Geen bloed of gore.** Lichaamsdelen verdwijnen cartoonesk.

## Code-stijl

- Eén klasse per bestand, bestanden kort houden (liefst onder de 200 regels).
- Namen in het Engels, `PascalCase` voor typen en methodes, `_camelCase` voor private velden.
- Namespace per laag: `SITG.Core`, `SITG.Game`, `SITG.Presentation`, `SITG.Services`.
- Kort commentaar bij alles wat niet vanzelf spreekt, gericht op iemand die leert lezen.

## Werkwijze per sessie

1. Vraag welke stap uit de bouwvolgorde we doen, of lees het uit de opdracht.
2. Leg in maximaal vijf zinnen uit wat je gaat doen.
3. Schrijf de code. Schrijf bij elke wijziging in Core ook EditMode-tests en draai ze.
4. Geef een klik-voor-klik-lijst voor wat de gebruiker in de Unity-editor moet doen.
5. Vertel precies hoe de gebruiker test dat het werkt (wat hij moet zien).
6. Na bevestiging dat het werkt: commit met een duidelijke Engelse message en push.

## Als iets niet werkt

- Vraag de gebruiker om de volledige foutmelding uit de Unity Console te plakken.
- Los één probleem tegelijk op.
- Bouw nooit verder op een stap die nog niet werkt.

## Buiten de MVP (niet bouwen tenzij gevraagd)

Pit Boss, valsspelen, pandjeshuis, charms, Steam, menselijke roulette, menselijke pachinko, extra lichaamsdelen.

## Status

- **Klaar:** Core-laag (alle spelregels) met 41 EditMode-tests. Gebouwd en getest met C# 9 op netstandard2.1, gelijk aan Unity 6.
  - `SeededRng`, `GameBalance`, `BodyParts`, `Stake`, `PlayerState`, `RunState`
  - `EconomyService`, `StakeService`, `RoundSettler`
  - `HigherLowerGame` (met `IDeck`, `InfiniteDeck`)
  - `SideBetBook`
  - `HouseRuleDefinition`, `HouseRuleSet`, `DefaultHouseRules`
  - `RunController` (start, avonden, afrekenen, opnieuw joinen, hervatten)
- **Afwijking van de SPEC:** `IMinigame` bevat alleen gedeelde dingen (id, fase, events). Spel-specifieke acties staan op de concrete klasse, bijvoorbeeld `HigherLowerGame.MakeGuess`.
- **Let op bij opslaan:** `RunState` gebruikt `ulong` en `List<T>`. Test of Unity's `JsonUtility` dit goed opslaat; zo niet, gebruik het gratis package `com.unity.nuget.newtonsoft-json`.
- **Volgende:** stap 2 (rondlopen) en stap 4 (eerste tafel die `HigherLowerGame` en `RunController` gebruikt).
