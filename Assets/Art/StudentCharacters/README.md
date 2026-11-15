# Student deckhand characters

The creator, student profile/dashboard portrait, and ship player instantiate the same two clothed prefabs from `Assets/Resources/StudentCharacters/Catalog.asset`.

## Included art

Quaternius free Standard downloads, obtained 15 November 2026:

- [Universal Base Characters](https://quaternius.itch.io/universal-base-characters): heads, eyes, eyebrows, four hair shapes and textures.
- [Modular Character Outfits – Fantasy](https://quaternius.itch.io/modular-character-outfits-fantasy): Male/Female Peasant outfits, used as a fixed linen-and-leather deckhand uniform.
- [Universal Animation Library](https://quaternius.itch.io/universal-animation-library): idle, walk, jog and airborne animation clips.

The licence files shipped inside these specific Standard archives explicitly place their contents under CC0 1.0. Exact included licence text is retained in `Licenses/`; source archive paths and SHA-256 hashes for imported files are in `Sources.json`. No paid pack or external runtime plugin is required.

## Assembly

`StudentCharacterAssetTools.Build()` configures Humanoid rigs, clips the base body to its clothed head, preserves the outfit's submeshes, rebinds the head and hairstyles to the outfit skeleton, and creates the two prefabs and shared locomotion controller. The male outfit's exposed-arm submesh keeps a separate skin material slot; changing skin never changes clothing. Generated assets are committed so teammates do not need to rebuild them.

Familiars and outfit selection are intentionally outside this feature. Both body types have one fixed uniform; all hairstyles are available with either body.
