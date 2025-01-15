# Djinn

<p align="center">
    <img alt="The Djinn" src="assets/icon.png">
</p>

Djinn is a command-line application for downloading music using Soulseek, with metadata from MusicBrainz, Last.fm, and Spotify.

## Downloading Music with `djinn download`

```
$ djinn download --help
Description:
  Download one or more releases

Usage:
  djinn download [options]

Options:
  --release-id <release-id>        Release ID
  --release-title <release-title>  Release title
  --artist-id <artist-id>          Artist ID
  --artist-name <artist-name>      Artist name
  --replace                        Replace existing album(s)
  --file-types <file-types>        File types to download [default: .flac|.mp3]
  --verbose                        Verbose output
  --no-progress                    Disable realtime download progress
  --year <year>                    Release year
  --strip-existing-metadata        Strip existing metadata from downloaded files [default: True]
  -?, -h, --help                   Show help and usage information
```

### Downloading a Single Release

To download a specific release from MusicBrainz, use the `download` command with the `--release-id` option.

```
djinn download --release-id "fb70321f-78df-30ff-92de-21a4bc9ca16c"
```

Or the `--release-title` option.

```
djinn download --release-title "Abbey Road"
```

If the release contains more than one artist credit, you must specify the artist as well. Use the `--artist-id` option.

```
djinn download --release-title "Abbey Road" --artist-id "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d"
```

Or the `--artist-name` option.

```
djinn download --release-title "Abbey Road" --artist-name "The Beatles"
```

### Downloading an Artist's Discography

To download the complete discography of an artist use the `download` command with the `--artist-id` option. 

```
djinn download --artist-id "78307112-b93f-451b-9da8-09cdb2c64d14"
```

Or the `--artist-name` option.

```
djinn download --artist-name "The Beatles"
```

## Music Library Organization

Djinn assumes that your music library follows a simple Artist/Album/Track structure.

You must set the `ArtistFormat`, `AlbumFormat`, and `TrackFormat` strings in your configuration file to correspond with the way you organize your music.

Given this configuration:

```
"ArtistFormat": "%S",
"AlbumFormat": "%Y %T",
"TrackFormat": "%n %t"
```

Djinn would expect your music to be organized as:

`Beatles, The`/`1969 Abbey Road`/`01 Come Together.flac`

This pattern directly reflects the provided format strings. Modify these strings in your configuration file to match your specific naming convention.

## Configuration

Djinn loads its configuration from `DJINN_CONFIG` or, if that isn't set, from `~/.config/djinn/djinn.json`.

### Configuration File Example

Here is an example of what the configuration file might look like:

```
{
    "LibraryPath": "/home/kyle/Music",
    "FfmpegPath": "/usr/bin/ffmpeg",
    "LastFmApiKey": "XXX",
    "LastFmApiSecret": "XXX",
    "SpotifyClientId": "XXX",
    "SpotifyClientSecret": "XXX",
    "SoulseekUsername": "XXX",
    "SoulseekPassword": "XXX",
    "ArtistFormat": "%S",
    "AlbumFormat": "%Y %T",
    "TrackFormat": "%n %t"
}
```

### Format Tokens

The configuration includes format tokens that define how artist, album, and track information should be displayed. The available tokens are as follows:

#### Artist Format Tokens

| Token | Description           | Example      |
| ----- | --------------------- | ------------ |
| `%A`  | Artist Name           | The Beatles  |
| `%S`  | Artist Sort Name      | Beatles, The |
| `%%`  | Literal '%' character | %            |

#### Album Format Tokens

All artist tokens, plus:

| Token | Description | Example   |
| ----- | ----------- | --------- |
| `%T`  | Album Title | Yesterday |
| `%Y`  | Album Year  | 1965      |

#### Track Format Tokens

All artist and album tokens, plus:

| Token | Description                            | Example   |
| ----- | -------------------------------------- | --------- |
| `%t`  | Track Title                            | Yesterday |
| `%n`  | Track Number (formatted as two digits) | 01        |
| `%N`  | Total number of tracks in the album    | 14        |

### Viewing Configuration with `djinn config`

You can view the path of the loaded config file and the parsed values by running:

```
$ djinn config
Configuration loaded from /home/kyle/.config/djinn/djinn.json
Library path:       /home/kyle/Music
Ffmpeg path:        /usr/bin/ffmpeg
Last.fm API key:    XXX
Last.fm API secret: XXX
Spotify client ID:  XXX
Spotify secret:     XXX
Soulseek username:  XXX
Soulseek password:  XXX
```

This command provides a quick way to ensure that Djinn is configured correctly and is reading from the intended configuration file.

### Display Library Statistics with `djinn stats`

You can view statistics about your music library by running:

```
$ djinn stats
Artists: 427, Albums: 2013, Tracks: 22761
```

### Download Missing Cover Art with `djinn covers`

```
$ djinn covers
Downloaded cover art for Velvet Teen, The - Elysium
Downloaded cover art for Yo‐Yo Ma - Japanese Melodies
```

### Replace MP3 Files with FLAC Files with `djinn upgrade`

```
$ djinn upgrade
...
```
