# 🌍 GodBox 

> An XR sandbox system where shaping physical sand alters a corresponding digital landscape in real time.

GodBox is an experimental **tangible XR world-building system** that merges **physical interaction with digital simulation**.

A depth camera scans the surface of a sandbox and converts the terrain into a **real-time digital mesh in Unity**. Users populate this terrain using a **creation catalogue**, placing and scaling objects to construct a living digital world that responds directly to the physical landscape.

The system explores how **tangible materials, spatial sensing, and XR interaction** can combine to create hybrid physical-digital environments.

GodBox is a **research prototype** developed to explore tangible play interaction and hybrid physical–digital environments rather than a finished consumer system.

---

# Introduction

GodBox was developed as part of the **Design for Emerging Technologies (DET)** course at **Extrality Lab, Stockholm University** by **Lorena Livadaru** and **Jacqueline Liddle**.

See more information about the project at:  
https://extralitylab.dsv.su.se/project/det/

The project investigates how **physical materials can act as interfaces for digital systems**.

---

## Problem

Traditional XR systems rely heavily on:

- controllers  
- menus  
- abstract interactions  

These approaches often reduce the sense of **embodied interaction with the environment**.

---

## Proposed Solution

GodBox introduces a **tangible sandbox interface** where shaping real sand directly modifies a digital landscape.

The system connects:

- physical terrain manipulation  
- real-time sensing  
- digital terrain generation  
- XR world building  

This creates an experience where users can **literally sculpt a world with their hands**.

---

# Design Process

The development of GodBox followed a design-research workflow typical for interaction design and XR prototyping.

---

## Brainstorming

Initial ideation explored ways to combine **tangible materials with XR environments**.

Key inspiration sources included:

- sandbox terrain modelling  
- projection-mapped sand tables  
- world-building mechanics from simulation games  

The core concept became **“sculpting a digital world through physical terrain manipulation.”**

---

## User Interaction Concept

The interaction model was designed around **three phases**.

### Phase 1 — Terrain Shaping

Users sculpt sand to create:

- mountains  
- valleys  
- basins  

The RealSense camera captures this terrain and generates a **digital mesh**.

---

### Phase 2 — World Building

Once the mesh is baked, users enter **Phase 2** and choose objects from the **Creation Catalogue UI**.

Examples include:

- houses  
- farms  
- environmental elements  

Some objects spawn additional entities such as:

- creatures  
- people  

---

### Phase 3 — World Observation

The environment becomes a populated interactive world responding to the terrain created by the participant.

The design emphasises **embodied interaction** rather than traditional controller-based VR.

---

## Demonstration Design

Because GodBox is an experimental prototype, the experience is structured as a **facilitated XR demonstration**.

Participants are introduced to the interaction before putting on the headset and are guided through phases of the experience to avoid confusion or technical breakdowns.

This structure helps maintain alignment between:

- participant expectations  
- system behaviour  
- facilitator instructions  

---

# System Description

GodBox integrates **physical sensing, terrain reconstruction, and XR interaction**.

---

## System Pipeline


Physical Sand
↓
Intel RealSense D435i
↓
Depth Frame Processing
↓
Terrain Mesh Generation (Unity)
↓
World Object Placement
↓
XR Interaction (Meta Quest)
↓
Projected World on Sandbox


This pipeline connects **tangible manipulation** with **digital world simulation**.

---

## Core Features

- Real-time **terrain reconstruction from depth sensing**
- **Interactive world-building tools** using XR menus
- **Hybrid physical-digital environment**
- **Projection mapping onto the sandbox**
- **Embodied interaction through physical materials**

---

# Hardware Setup

## Core Hardware

- **Intel RealSense D435i** depth camera  
- **Meta Quest 3 / Quest Pro** headset  
- **PC with dedicated GPU**  
- **Optoma Mini LED projector**

---

## Sandbox Setup

- Sandbox dimensions: **40 cm × 60 cm**
- Approximately **10 kg kinetic sand**
- Depth camera mounted **above the sandbox**

The sandbox functions both as:

- a **physical interaction surface**
- a **projection display surface**

---

# Software

## Development PC Specifications

- OS: Windows 11  
- CPU: AMD Ryzen 9 5950X (16-core)  
- RAM: 32 GB  
- GPU: Dedicated GPU recommended for XR rendering  

---

## Development Environment

Unity **6000.3.10f1**

---

## SDKs and Plugins

- Intel **RealSense SDK**
- **RealSense Unity Wrapper**
- **Meta XR SDK**
- **XR Interaction Toolkit**

---

## Additional Tools

- **Canva** — UI and visual asset design

---

# Installation

To install and run the project:

| Platform | Device | Requirements | Steps |
|----------|--------|-------------|------|
| Windows | Meta Quest | Unity 6000+, RealSense SDK | Clone repo → Open Unity project → Load scene |

---

## Setup Steps

### 1. Clone the Repository


git clone <repository-url>


---

### 2. Install Dependencies

Install the required SDKs:

- Intel RealSense SDK  
- Meta XR SDK  
- XR Interaction Toolkit  

---

### 3. Connect the RealSense Camera

Connect the **Intel RealSense D435i** to the PC via USB.

---

### 4. Open the Unity Project

Open the project using:


Unity 6000.3.10f1


---

### 5. Load the Main Scene


Scenes/GodBox_Main


---

### 6. Calibrate Depth Capture

Position the camera above the sandbox and adjust calibration parameters until terrain updates correctly.

---

# Usage

## Interaction Flow

### 1. Shape the Terrain

Participants sculpt the sand with their hands.

The depth camera captures the terrain and generates a **digital mesh**.

---

### 2. Select Objects

Users open the **Creation Catalogue UI** and select objects.

Examples include:

- houses  
- farms  
- environmental elements  

Some objects spawn additional entities such as creatures and people.

---

### 3. Place and Scale Objects

Objects can be **positioned and resized directly on the generated terrain**.

---

### 4. Observe the World

The terrain and placed objects form a **digital world reacting to the physical landscape**.

---

# Project Structure


Assets/
Scripts/
SandTopographyManager.cs
SandMeshBuilder.cs
SnapToSand.cs
CustomRsProcessingPipe.cs
GameStateManager.cs
UIPlatformSpawner.cs

  FarmSpawner.cs
  HouseSpawner.cs
  InsectSpawner.cs
  CreatureMover.cs
  ButterflyHover.cs

  PeopleApproachController.cs
  PersonAnimatorDriver.cs

  ActivateDisplays.cs
  FadeInTMPText.cs

Scenes/
GodBox_Main.unity

Prefabs/
Creatures
Buildings
TerrainObjects

RealSense/
RealSenseSDK
ProcessingPipeline

UI/
CreationCatalogue
InteractionMenus


---

# Known Limitations

- Depth sensing becomes unstable in **strong lighting conditions**
- Sand outside the **camera capture area** is ignored
- Camera calibration and projector alignment must be repeated if hardware is moved
- High terrain mesh density may reduce performance
- The experience requires **Quest Link** due to the computational load

GodBox is a **research prototype**, not a production system.

---

# References

Frameworks used in the demonstration design:

- **Petersson et al.** – Alignment framework for XR demonstrations  
- **Bobbe et al.** – Design principles for technology demonstrations  

These frameworks informed the **demo protocol, interaction phases, and facilitator alignment strategies**.

---

# Contributors

**Lorena Livadaru**  
Master's Student — Design for Creative and Immersive Technologies  

**Jacqueline Liddle**  
Master's Student — Design for Creative and Immersive Technologies  

Extrality Lab  
Stockholm University

---

# Credits

Special thanks to:

**Kipras Klimkevicius**  
Master's Student — Design for Creative and Immersive Technologies  
Technical assistance

**Luis Quintero**  
Assistant Professor in Digital Media and Data Science  
Stockholm University  
Guidance on RealSense integration

---

⭐ Developed at **Extrality Lab, Stockholm University**