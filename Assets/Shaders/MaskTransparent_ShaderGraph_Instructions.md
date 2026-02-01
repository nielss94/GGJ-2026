# Mask Transparent — Shader Graph version

This matches the behaviour of `Unlit/MaskTransparent`: **mask texture** (black = transparent, white = opaque) and **tint colour** on a plane.

## 1. Create the graph

- **Right‑click** in Project → **Create** → **Shader Graph** → **HDRP** → **Unlit Shader Graph**
- Name it e.g. `MaskTransparentGraph` and save it in `Assets/Shaders/` (or next to the existing `MaskTransparent.shader`)

## 2. Graph settings (transparent)

- Select the **Graph** (click empty space or the “HDRP” target).
- In **Graph Inspector**:
  - **Surface Type**: **Transparent**
  - **Blending Mode**: **Alpha** (or Alpha Blend)
  - Leave **Alpha Clipping** off unless you want a hard cutout.

## 3. Add properties

In **Blackboard** (left panel):

1. **+** → **Texture 2D**
   - Name: `MaskTex`
   - Reference: `_MaskTex`
   - Default: leave empty or white; you’ll assign the mask in the material.

2. **+** → **Color**
   - Name: `Color`
   - Reference: `_Color`
   - Default: white (1, 1, 1, 1).

## 4. Fragment graph (node setup)

In the **Fragment** context, add and wire nodes as below. You can use **Space** to open the node menu and search by name.

### 4.1 UV and sample mask

- **UV** node (search “UV”) → **Sample Texture 2D**
  - **Texture 2D** input: drag **MaskTex** from Blackboard.
- **Sample Texture 2D** outputs: **RGBA** (vector4).

### 4.2 Luminance from mask (black → 0, white → 1)

- **Split** node (search “Split”): input = **RGBA** from Sample Texture 2D.
  - You get **R**, **G**, **B**, **A**.
- **Dot Product** (search “Dot”):
  - **A**: use a **Vector 3** node with **(0.299, 0.587, 0.114)**.
  - **B**: **Combine** R, G, B from the Split into a **Vector 3** (order R, G, B).
- Dot output = luminance (single float).

### 4.3 Final alpha

- **Multiply** (search “Multiply”):
  - **A**: luminance from Dot Product.
  - **B**: **A** from the Split (mask texture alpha).
- This is your **alpha** (black = 0, white = 1, and texture alpha can cut out).

### 4.4 Base colour

- **Multiply**:
  - **A**: **Color** from Blackboard (vector4 or colour).
  - **B**: use **(1, 1, 1, 1)** if you only want tint; or use **RGBA** from Sample Texture 2D if you want mask colour × tint.
- For “tint only” (like the code shader): use **Color** directly as base colour and only use the mask for alpha.

### 4.5 Connect to master

- **Unlit Master** (or “Fragment” stack):
  - **Base Color**: **Color** from Blackboard (or the result of “tint × mask” if you prefer).
  - **Alpha**: the **Multiply** output (luminance × mask A).

## 5. Summary (minimal setup)

- **Base Color** ← **Color** (Blackboard).
- **Alpha** ← **Multiply**( **Dot**( **Vector3(0.299, 0.587, 0.114)**, **RGB** from **Split**( **Sample Texture 2D**( **UV**, **MaskTex** ) ) ), **A** from same Split ).

## 6. Save and use

- **Save** the graph (Ctrl/Cmd + S).
- Create a **Material**, set its shader to your new **Mask Transparent Graph**.
- Assign **Mask Tex** (your black/white or grayscale texture) and **Color** (tint). Black areas will be transparent; white opaque.

This reproduces the same behaviour as `Assets/Shaders/MaskTransparent.shader` (mask luminance × mask alpha as alpha, tint as base colour).
