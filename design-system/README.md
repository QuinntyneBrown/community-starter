# Community Starter design system

The design system translates the editorial energy, warm neutrals, vivid accents, strong type, and
rounded interaction language observed on FaithTech's public site into an original Community Starter
identity. It does not include FaithTech trademarks, copy, fonts, photography, or downloaded assets.

`tokens.css` and `base.css` are canonical. Marketing and Angular consume these files through their
build configurations. Feature styles use semantic `--cs-*` roles and do not introduce literal colors.

## Asset provenance

| Asset      | Source                               | Distribution                     |
| ---------- | ------------------------------------ | -------------------------------- |
| Typography | Platform font fallbacks; no download | Browser and operating system     |
| `logo.svg` | Original Community Starter artwork   | Repository and product artifacts |

The optional family names in the typography tokens allow a deployment to add reviewed, self-hosted
fonts later without changing component contracts. No third-party font request ships by default.
