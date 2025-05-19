# Djinn

Music library manager and Soulseek client using metadata from MusicBrainz, Last.fm, and Spotify.

## Downloading Music with `djinn download`

```sh
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

```sh
$ djinn download --release-id "c812852b-4e5c-441e-bdd3-62ec45a6c215"
```

Or the `--release-title` option.

```sh
$ djinn download --release-title "Wrong Way Up"
```

### Downloading an Artist's Discography

To download the complete discography of an artist use the `download` command with the `--artist-id` option.

```sh
$ djinn download --artist-id "ff95eb47-41c4-4f7f-a104-cdc30f02e872"
```

Or the `--artist-name` option.

```sh
$ djinn download --artist-name "Brian Eno"
```

## Display Library Statistics with `djinn stats`

You can view statistics about your music library by running:

```sh
$ djinn stats
Artists: 427, Albums: 2013, Tracks: 22761
```

## Check Your Library for Issues with `djinn check`

Checks that:

* All album directories contain a `.metadata.json` file.
* Album directories are named correctly based on the configured format
* Track files are named correctly based on the configured format
* All tracks referenced in the metadata file exist as audio files

## Download Missing Cover Art with `djinn covers`

```sh
$ djinn covers
Downloaded cover art for Velvet Teen, The - Elysium
Downloaded cover art for Yo-Yo Ma - Japanese Melodies
```

## Replace MP3 Files with FLAC Files with `djinn upgrade`

```sh
$ djinn upgrade
...
```

## Configuration

Djinn loads its configuration from `DJINN_CONFIG` or, if that isn't set, from `~/.config/djinn/config.json`.

### Music Library Organization

Djinn assumes that your music library follows a simple Artist/Album/Track structure.

You must set the `ArtistFormat`, `AlbumFormat`, and `TrackFormat` strings in your configuration file to correspond with the way you organize your music.

Given this configuration:

```
"ArtistFormat": "%S",
"AlbumFormat": "%Y %T",
"TrackFormat": "%n %t"
```

Djinn would expect your music to be organized as:

`Eno, Brian; Cale, John`/`1990 Wrong Way Up`/`01 Lay My Love.flac`

### Format Tokens

The configuration includes format tokens that define how artist, album, and track information should be displayed. The available tokens are as follows:

#### Artist Format Tokens

| Token | Description           | Example                |
|-------|-----------------------|------------------------|
| `%A`  | Artist Name(s)        | Brian Eno, John Cale   |
| `%S`  | Artist Sort Name(s)   | Eno, Brian; Cale, John |
| `%%`  | Literal '%' character | %                      |

#### Album Format Tokens

All artist tokens, plus:

| Token | Description | Example      |
|-------|-------------|--------------|
| `%T`  | Album Title | Wrong Way Up |
| `%Y`  | Album Year  | 1990         |

#### Track Format Tokens

All artist and album tokens, plus:

| Token | Description                            | Example     |
|-------|----------------------------------------|-------------|
| `%t`  | Track Title                            | Lay My Love |
| `%n`  | Track Number (formatted as two digits) | 01          |
| `%N`  | Total number of tracks in the album    | 10          |

### Configuration File Example

Here is an example of what the configuration file might look like:

```json
{
    "LibraryPath": "/home/kyle/Music",
    "FfmpegPath": "/usr/bin/ffmpeg",
    "FfprobePath": "/usr/bin/ffprobe",
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

## Viewing Configuration with `djinn config`

You can view the path of the loaded config file and the parsed values by running:

```sh
$ djinn config
Configuration loaded from /home/kyle/.config/music/music.json
Library path:       /home/kyle/Music
Ffmpeg path:        /usr/bin/ffmpeg
Ffprobe path:       /usr/bin/ffprobe
Last.fm API key:    XXX
Last.fm API secret: XXX
Spotify client ID:  XXX
Spotify secret:     XXX
Soulseek username:  XXX
Soulseek password:  XXX
```