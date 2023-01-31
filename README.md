# Wills Wacky Gamemodes

Provides a few different gamemodes currently. A couple more gamemodes and some general gamemode settings are planned.

<details>
<summary><h2>Change log</h2></summary>

### v 0.0.1
- Adjustments to the default values
- Pick Phase UI to display the current pick phase in Draft and Stud Draw.

----
### v 0.0.0
- Release
</details>

<details>
<summary><h2>Gamemodes</h2></summary>
<details>
<summary><h3>Stud Draw</h3></summary>

- Default Rounds: 3
- Default Points Per Round: 3
- Both Team and FFA Variant: true

In this gamemode, players draw all their cards before the start of gameplay. No further picks are recieved between rounds.

#### Options
---

- Cards Drawn: How many cards are drawn before the game starts.
</details>

<details>
<summary><h3>Rolling Cardbar</h3></summary>

- Default Rounds: 3
- Default Team Rounds: 5
- Default Points Per Round: 2
- Both Team and FFA Variant: true

In this gamemode, as players accrue cards, they lose their old ones, causing builds to change over time.

If using classes manager reborn, Force classes is advised to be off.

#### Options
---

- Maximum Cards: The maximum amount of cards a player can have before the cardbar starts rolling.
</details>

<details>
<summary><h3>Draft</h3></summary>

- Default Rounds: 2
- Default Points Per Round: 5
- Both Team and FFA Variant: true

In this gamemode, players draw a hand of cards and then pass them around to each other before fighting each other.

The default hand size for players is `Starting Picks + Extra Cards Drawn + 1`.

If a player would ever need to pick a card when they've run out, the game will generate a new set of hands for the players.

If using classes manager reborn, Force classes is advised to be off.

It is recommended to disable shuffle, distill knowledge, and other similar cards.

#### Options
---

- Starting Picks: The starting number of picks for the initial draft.
- Extra Cards Drawn: How many extra cards are drawn per draft.
- Can Pick Cards Each Round: Whether you get to pick cards each round. Picking on continues is disabled if true. Winners would not get to pick.
- Picks Per Round: How many picks you get each round.
- Can Pick Cards On Continue: Whether you can pick cards when you continue. Note that winners get to pick as well.
- Picks Per Continue: How many picks you get on a continue.
- Recalculate Continue Hand Size: Whether the hand size for a continue is recalculated based on the number of picks you get.
</details>
</details>