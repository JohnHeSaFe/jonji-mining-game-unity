# Multiplayer Network Setup Guide
## Jonji Mining Game — Unity Transport (com.unity.transport)

---

## Prerequisites

1. Open **Window → Package Manager**.
2. Search for **"Unity Transport"** (`com.unity.transport`).
3. Install version **2.x** (if not already present). The scripts require UTP 2.x API.

---

## Phase 1 — Scene: Server

The server is a headless relay; it doesn't need players or a camera.

1. In **GameScene**, create an empty GameObject → name it **`NetworkManager`**.
2. Add the `NetworkServer` component to it.
3. Leave **Port** at `9000` (or change it everywhere consistently).
4. **Do NOT add NetworkClient or LocalPlayer to this object when running as server.**
   - In practice you run the server as a standalone build with `-server` args,
     OR you temporarily disable the `NetworkClient` component in-editor.

---

## Phase 2 — Scene: Each Client

Both players run an identical client build. Repeat these steps for each player machine.

### 2-a  NetworkManager object
1. Select (or create) the **`NetworkManager`** GameObject.
2. Add the `NetworkClient` component.
3. Set **Server IP**:
   - Same machine test → `127.0.0.1`
   - LAN test → the server machine's local IP (e.g. `192.168.1.10`)
4. Set **Server Port** → `9000`.

### 2-b  Scene references on NetworkClient
| Field | Assign |
|---|---|
| **Remote Player** | Drag the **opponent's** player GameObject (BluePlayer or RedPlayer) |
| **Tilemap** | Drag the shared mining Tilemap from the scene hierarchy |

### 2-c  Replace PlayerController with LocalPlayer
1. On **your own** player GameObject (e.g. BluePlayer for Client 1):
   - Remove (or disable) the old `PlayerController` component.
   - Add the `LocalPlayer` component.
2. Fill in all the same inspector fields as before:
   | Field | Value |
   |---|---|
   | Player ID | 1 (BluePlayer) or 2 (RedPlayer) |
   | Tilemap | same Tilemap reference |
   | Speed / Jump Force / etc. | same as before |
   | Sprite Up/Down/Left/Right | same sprites |
   | Ground Check | same child Transform |
   | Ground Layer | same LayerMask |
   | **Network Client** | drag the **NetworkClient** component from `NetworkManager` |
3. Controls are now fixed to **WASD + Space (jump) + F (mine)** for every player.
   - You can change `keyJump` and `keyMine` in the Inspector if needed.

### 2-d  Remote player: disable its input
The opponent's GameObject (e.g. RedPlayer on Client 1) should have:
- `LocalPlayer` component **removed** (position is driven by `NetworkClient`).
- `Rigidbody2D` set to **Kinematic** so the network can move it without physics fighting it.
- Collider can stay for visual purposes.

---

## Phase 3 — Local Testing (one machine, two processes)

### Option A — Editor + Standalone

1. **Build** the project (File → Build Settings → PC → Build).
2. In the **Editor**:
   - On `NetworkManager`, enable only `NetworkServer` (disable `NetworkClient`).
   - Press **Play** → the editor acts as the relay server.
3. **Launch the standalone build twice** — each is one client.
   - The first build = BluePlayer. The second = RedPlayer.
   - Both connect to `127.0.0.1:9000`.

### Option B — ParrelSync (recommended for rapid iteration)

1. Install **ParrelSync** from the Package Manager (Git URL: `https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync`).
2. **ClonesManager → Add new clone** — creates a second linked project.
3. Open one project as **Server** (NetworkServer only) and both clones as **Clients**.

---

## Packet Format Reference

| Byte | Type | Payload |
|---|---|---|
| `0` | Position | `float x`, `float y` |
| `1` | Mine | `int cellX`, `int cellY` |

---

## Common Pitfalls

| Problem | Fix |
|---|---|
| "Failed to bind on port 9000" | Another process is using that port; change `port` on `NetworkServer`. |
| Remote player teleports | Physics fighting the kinematic move — confirm Remote Player's Rigidbody is **Kinematic**. |
| Tiles not syncing | Confirm `tilemap` is assigned on `NetworkClient` AND the same Tilemap is assigned in `LocalPlayer`. |
| "Could not parse endpoint" | Check `serverIP` string — no spaces, valid IPv4. |
| High latency locally | Normal for loopback; set `networkTickRate` to 30 for smoother local tests. |
