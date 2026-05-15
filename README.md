# PAS — Simulare de Curse Auto 3D în Unity

Joc de curse auto 3D dezvoltat în motorul Unity (versiunea 6000.4.2f1),
cu accent pe simularea realistă a fizicii vehiculelor și pe inteligența
artificială a oponenților. Realizat ca proiect pentru cursul de
*Proiectarea Avansată a Sistemelor* (PAS).

## Prezentare Generală

Proiectul abordează trei provocări fundamentale din domeniul dezvoltării
jocurilor:

- **Fizica vehiculului jucătorului** — model de conducere realist bazat
  pe componenta `WheelCollider` din Unity, cu suport pentru tracțiune
  integrală (AWD), frânare, coasting (rulare liberă), control al
  tracțiunii și asistență la aderență laterală.
- **Inteligența artificială a oponenților** — sistem de navigare bazat
  pe waypoint-uri (puncte de referință), care permite vehiculelor
  controlate de calculator să parcurgă autonom traseul de curse, cu
  adaptare dinamică a vitezei prin zone de boost și încetinire.
- **Generarea procedurală a traseului** — construcția algoritmică a unui
  circuit de curse în formă de octogon regulat, complet cu segmente de
  drum, pereți interiori și exteriori, și texturi procedurale.

Sunt incluse și sisteme auxiliare: numărătoare inversă la start,
contorizare tururi, meniu de pauză și ecran de sfârșit al cursei.

## Structura Proiectului

- `Assets/Scripts/` — logica jucătorului (`SimplePlayerCar`, `CameraFollow`)
- `Assets/Scripts/OpponentCarAI/` — AI-ul oponenților (`OpponentCar`,
  `OpponentCarWaypoints`, `Waypoint`, `BoostPad`, `SpeedBreakers`)
- `Assets/Scripts/UI/` — sistemele de interfață (`LapSystem`,
  `MissionEndUI`, `PauseMenu`, `RaceCountdown`)
- `Assets/Scripts/Editor/` — instrumente pentru editorul Unity
  (`WaypointEditor`, `WaypointManagerWindow`)
- `Assets/Editor/` — scripturi de configurare automată
  (`AutoSetupScene`, `AutoSpawnRoad`)
- `paper/` — sursele LaTeX și PDF-ul compilat al lucrării

## Cerințe

- Unity **6000.4.2f1** (Universal Render Pipeline + New Input System)

## Lucrare

O descriere completă -- metodologie, modelul fizic, proiectarea AI și
rezultate -- este disponibilă în `paper/` (surse LaTeX și PDF compilat).

## Autori

Vasile Daria-Gabriela, Turcu Filip-Ianis, Borozan George,
Pitica Nicola, Gheorghieș Petruț-Rareș, Voicu Ioan-Vladut,
Popescu Tiberiu-Andrei.
