# Virtual manipulator  

A WPF desktop app that visualizes and animates a 7-segment robotic manipulator using a custom CPU software renderer.
The manipulator continuously solves inverse kinematics toward a target point (shown as a teapot)

Build: https://github.com/uvec3/manipulator/releases/tag/build

https://github.com/user-attachments/assets/771af182-2468-4bfe-a250-c1463562f877


## Controls

- **Play/Pause** - start or stop simulation updates
- **Simulation speed slider** - scales speed
- **Reset** - resets manipulator joint rotations to the default position
- **Aim sliders (X/Y/Z)** - move target directly
- **Keyboard target movement** - `W/A/S/D` (horizontal plane), `Q/E` (down/up)
- **Random position** - picks a random valid target position
- **Mouse drag on viewport** - orbit camera
- **Mouse wheel** - zoom camera distance

## Parameters

The length of each segment can be adjusted separatly

https://github.com/user-attachments/assets/00c04f95-5108-4e5a-af3f-863c97c6d4f9

## Rendering 
 All rendering logic is implemented completely from scratch using direct bitmap writing, without relying on any graphics APIs. The only external dependency is GlmSharp, which is used for vector algebra.

### Renderer features
- render polygons of triangles
- render lines
- load obj models
- ambient and diffuse lighting
- multiple light sources
- apply different linear transformations
- depth-aware rendering
 
### Graphics Settings

- Front/back/both face rendering
- Viewport resolution preset
- Background RGB sliders
- Lighting toggle and ambient/diffuse intensity
- Light position (X/Y/Z)
- Sphere detail level
- Fake normals toggle

https://github.com/user-attachments/assets/4885ec1c-7f5a-48a2-91c7-97dcb43d2361




