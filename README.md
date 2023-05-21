# Flood SAVE (Simulation And Visualisation Engine)
#### COMPSCI 715 Group 5

## Software Description
This project provides a method for simulating heavy rainfall events over terrains of 10-20km<sup>2</sup>.
Currently available for simulation are three suburbs in Auckland, New Zealand:
- Orakei
- Penrose
- Auckland CBD

The features of this engine include:
- Altering the rate at which soil and man-made drainage remove water from the surface.
- Simulate the absorption capacity of man-made drainage and grass during rain events.
- Alter the capacity of soil and man-made drains, or disable it all together to assume drain blockage.
- Recreate historical rain events using Auckland rain data from 1960 to 2022 or define a set rainfall in millimeters.
- If a set rainfall is used periods of rain and no rain can be set.
- Altering the level of detail of the water simulation (**WARNING** resolutions below 25 are unlikely to perform well).
- Toggle and select an area to simulate in a higher resolution to gain more detail about a section of the terrain (**Experimental**).
- Ability to see the severity of the flood with a rising water level and colour coding.
- Move freely through the terrain with the use of *WASD* for direction, *Shift* for speed and the mouse to look around. Pressing *P* pauses camera movement.

## Installation
The files on this gihub page currently correspond to a Unity project. Subsequently, the directory this project is cloned into should be able to be opened using Unity.
Installation has been tested on Unity editor version `2021.3.11f1` other versions may work but no guarantee can be made.
Once cloned and opened in Unity, a scene can be chosen from the `Scenes` directory. The simulation parameters are available in the `ColumnManager` game object.

***Note:*** For best simulation performance, ensure the Unity editor is in release mode, not debug mode.

Within the `Assets/Scripts/PythonScripts/` directory are data and Python files used to process the rain, drainage and terrain. These are not required to run the Unity project but are used as part of the process to get the data in an efficient format for the Unity editor.

## Contributors
The contributors to this project are given in alphabetical order with Github page links:
- [Liam Brydon](https://github.com/limmooo)
- [Jack Lobb](https://github.com/jlob275)
- [Minxing Miao](https://github.com/AlexMiao7)
- [Joshua Monigatti](https://github.com/JoshuaMoni)
- [Daniel Shen](https://github.com/Shenmister)