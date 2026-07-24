# BME AUT brand assets

The source assets behind the portal's look: the official **BME AUT** identity pack, and the **eduID**
logo used on the login button. `ahk-frontend` derives its palette and its rendered images from here.

This folder holds the design masters, which are not shipped. The web-ready copies the app actually
loads live in `../public/` (see the table at the end).

## What is here

| Folder | Contents |
|---|---|
| `BME-AUT logo english/` | Full wordmark, "Department of Automation and Applied Informatics" |
| `BME-AUT logo hungarian/` | Full wordmark in Hungarian, plus `TeamsLogo.png` |
| `BME-AUT logo only/` | The `BME /AUT` mark with no wordmark — language-neutral despite the `-hun` suffix |
| `eduID/` | The eduID (Hungarian federation) colour logo, rectangular and square |

Each wordmark comes in `COLOR`, `BLACK` and `WHITE`, as **PNG** (3645×690, for screen) and **EPS**
(vector print masters, ~480 KB each). The EPS files are the only scalable and re-colourable form —
keep them. `COLOR_CMYK` is for process printing, `COLOR_Pantone202` for spot.

## Colours

| Value | What it is | Where it came from |
|---|---|---|
| `#900028` | The logo's own crimson | Exact pixel value sampled from `BME_AUT_logo_COLOR-eng.png` |
| `PANTONE 202 CVC`<br>CMYK `0 / 1 / 0.6510 / 0.4706` | Print specification of that crimson | `%%CMYKCustomColor` header inside `BME_AUT_logo_COLOR_Pantone202-*.eps` |
| `#a4001e` | Heading + accent crimson used on screen | `h1..h6 { color:#a4001e }` in aut.bme.hu's stylesheet |
| `#88000f` | Active/hover navigation marker | `#mainNavBar a:hover { border-bottom:3px solid #88000f }`, same source |
| `#801b1b` / `#ffcfcf` / `#e5a3a3` | Error text / wash / rule | `.errorBox`, `.critical`, same source |
| `#074371` | Body link navy — **not** crimson | `a[href] { color:#074371 }`, same source |
| `#dbd9c0` | Table-header parchment | `.gridViewHeader`, same source |

Type on aut.bme.hu: `Georgia, "Times New Roman", Serif` at **normal weight** for every heading,
`Verdana, Arial, Helvetica, sans-serif` for body.

## eduID

The login button follows the [eduID brand](https://eduid.hu/hu/depo/). The official button bakes in a
Hungarian label ("Belépés"), so instead of shipping that PNG the login screen renders its own button
from the eduID colour logo plus an English "Login" — the deviation the brand guide permits. Sampled
brand blues: logo `#4070B8` / navy `#203954` / light `#66AADF`; login-button azure `#0068AD`.

## Using the logo on the web

`ahk-frontend/public/` holds **derived copies**, not the originals — edit them here and re-copy, do
not edit them there:

| `ahk-frontend/public/` | Copied from | Used by |
|---|---|---|
| `bme-aut-logo-white.png` | `BME-AUT logo english/BME_AUT_logo_WHITE-eng.png` | Shell topbar (dark band) |
| `bme-aut-logo.png` | `BME-AUT logo english/BME_AUT_logo_COLOR-eng.png` | Login screen (light background) |
| `bme-aut-mark.png` | `BME-AUT logo only/BME_AUT_logo_only_COLOR-hun.png` | Reserved — wordless mark |
| `eduid-logo.png` | `eduID/eduid_logo_color_rect.png` | eduID login button |

The BME AUT PNGs are 5.28:1, so give them an explicit `height` and `width: auto`, and `alt` text
naming the department. The English wordmark is used because the portal's interface text is English.

**Known gap**: there is no square favicon. The wordless mark is 2.86:1 and becomes an unreadable
strip at 32px. Producing one needs a square crop from the EPS in a vector editor.
