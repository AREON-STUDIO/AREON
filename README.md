
# 🚀 AREON
### Explore Mars. Survive the Mission. Discover Science.

<p align="center">
  <strong>An Educational Mars Survival & Exploration Game</strong><br/>
  Built for NASA Space Apps Challenge 2026
</p>

---

## 🌌 About AREON

**AREON** is an educational Mars-survival simulation where players take on the role of a junior astronaut operating a research habitat on **Arcadia Planitia, Mars**.

Designed primarily for children aged **7–12**, AREON combines exploration, resource management, scientific learning and interactive gameplay to introduce players to the challenges of surviving on Mars.

Across a three-Sol mission, players must maintain essential resources, prepare their habitat, respond to environmental hazards, collect scientific data and complete their final mission review.

Our goal is to make Mars science engaging through gameplay, experimentation and visible cause-and-effect interactions.

## 🎯 Mission Objective

Complete a three-Sol Martian mission by:

- Managing oxygen and essential supplies.
- Maintaining habitat power systems.
- Monitoring radiation levels.
- Responding to dust storms and environmental hazards.
- Exploring the Martian surface.
- Collecting and depositing scientific data orbs.
- Completing the final science assessment.

## 🎮 Gameplay

### The Three-Sol Campaign

| Mission | Main Objective |
|---|---|
| Sol 1 | Initial habitat checks, life-support inspection and preparation |
| Sol 2 | Monitor weather, clean solar panels and evaluate EVA safety |
| Sol 3 | Manage remaining resources and complete the final science mission |

### Core Gameplay Loop

1. Wake up inside the habitat.
2. Prepare the dome and inspect essential systems.
3. Exit through the airlock.
4. Explore the Martian surface and collect a data orb.
5. Return safely to the habitat.
6. Deposit collected scientific data.

Each Sol introduces new challenges and encourages informed survival decisions.

## 🪐 Core Game Systems

| System | Description |
|---|---|
| AIR | Monitor oxygen inventory and consumption |
| POWER | Generate, store and manage electrical energy |
| RAD | Monitor radiation exposure and shelter protection |
| FOOD & WATER | Manage essential supplies |
| WEATHER | Respond to environmental conditions and dust events |
| HABITAT | Maintain panels, shields and essential equipment |

### Scientific Gameplay

AREON uses a simplified but transparent Mars simulation model.

Key design principles:

- Arcadia Planitia is the designated game setting.
- Environmental inputs are intended to use location-specific modelling.
- Measured data, modelled estimates and gameplay assumptions are distinguished.
- Resource calculations use a centralized simulation clock.
- Scientific decisions are connected to visible gameplay consequences.

**Simulation rate:** 240× accelerated Martian time.

One real second advances four in-game minutes.

**Campaign duration:** Approximately 18.5 real minutes for three Martian Sols.

## 🧠 Educational Features

- Learn how astronauts manage resources on Mars.
- Understand solar energy and battery management.
- Explore radiation protection concepts.
- Discover how dust storms affect solar power.
- Learn about Mars environmental monitoring.
- Complete science-based challenges and assessments.

The game follows a child-friendly no-fail design with rescue, retries and supportive feedback rather than punitive game-over mechanics.

## 🖥️ Technology Stack

### Game Development
- Unity 6
- C#
- Unity UI / Canvas
- Unity Physics and gameplay systems

### Web Development
- React.js
- JavaScript
- HTML5
- CSS3

### Collaboration & Version Control
- Git
- GitHub
- GitHub Desktop

### Platform
- Primary target: Web browser (WebGL), subject to build compatibility.
- Development environment: Windows PC.
- Input: Keyboard and mouse.

## 🏗️ Project Structure

```text
AREON/
│
├── unity-game/
│   └── Unity Project
│
├── web-client/
│   └── React Web Application
│
├── docs/
│   ├── GDD/
│   ├── Research/
│   ├── Architecture/
│   └── API/
│
├── .gitignore
├── .gitattributes
└── README.md
```

## 👥 Team AREON

| Member | Responsibility |
|---|---|
| Unmesh | Lead, coordination, scientific consistency and integration, Unity UI development and in-game UI integration |
| Sudharsan | Unity level design, environment and scene setup |
| Dilli | Core gameplay mechanics and game logic |
| Meghna | Web frontend development and integration support |

## 📚 Scientific Foundation & References

AREON's scientific design draws on NASA research and Mars environmental modelling resources.

Key reference areas include:

- NASA life-support requirements.
- NASA OCHMO metabolic-load guidance.
- NASA MOXIE technology and mission results.
- NASA Curiosity Radiation Assessment Detector (RAD).
- NASA Mars surface power-generation research.
- NASA InSight power-generation and dust-storm reports.
- Mars Climate Database v6.2.
- TES / THEMIS / MCS dust optical-depth climatology.

**Scientific accuracy note:** NASA sources support the underlying Mars science and selected planning benchmarks. They do not independently validate every simplified gameplay rule. Arcadia weather, radiation and solar-power time series remain modelled or reconstructed until site-specific inputs and uncertainties are documented.

## 📖 Project Documentation

The official Game Design Document is maintained in:

`docs/GDD/AREON_Final_Game_Design_Document.pdf`

It covers:
- Game vision and scope.
- Three-Sol gameplay.
- Scientific simulation rules.
- Resource management.
- UI and player experience.
- Technical architecture.
- Team responsibilities.

## 🚀 Project Status

**Current stage:** MVP development and integration.

Planned MVP components:

- [ ] Astronaut movement and interaction.
- [ ] Habitat environment and exploration.
- [ ] AIR, POWER and RAD monitoring.
- [ ] Dust-storm response.
- [ ] Habitat maintenance.
- [ ] Three-Sol objective sequence.
- [ ] Data orb collection and deposit.
- [ ] Child-friendly gameplay HUD.
- [ ] Web Mission Control dashboard.
- [ ] Final mission log and science assessment.

## 🤝 Collaboration

AREON is developed collaboratively using Git and GitHub.

Development workflow:

1. Create a feature branch.
2. Implement and test your assigned feature.
3. Commit and push changes.
4. Open a Pull Request.
5. Review and integrate changes into the development branch.

Please coordinate before editing shared Unity scenes and prefabs.

## 📜 License

License and distribution terms will be defined by the project team.

---

<p align="center">
  <strong>AREON — Every Sol is a new discovery.</strong><br/>
  <em>Making Mars exploration an educational adventure.</em>
</p>
