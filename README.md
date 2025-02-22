# TextToSpeech

**TextToSpeech** is a mod that brings in-game text to life using an offline, high-quality text-to-speech engine—**Piper
**. When you or other players send chat messages, the mod converts that text to speech and plays it through your local
player's AudioSource, making it speak to you. It will also read out other text in the game like runes, intro text,
center screen messages, dreams, and npc text.

This mod is a work in progress and has room for improvement. If you have any suggestions or ideas, please feel free to
reach out to me on my discord.

This mod is *CLIENT SIDE ONLY*. It does not require server-side installation.

> **Note:** All TTS processing is done locally. The mod downloads only the necessary Piper files and voice models from
> the internet when needed (see [Piper VOICES.md](https://github.com/rhasspy/piper/blob/master/VOICES.md) for available
> voices) and does not transmit any personal data. Subsequent usage is offline after initial files are downloaded.
> Exception being, you configured the mod to use another voice model URL (it would download the model and config file
> from
> that URL instead once more).

---

## Features

- **Nice to know**
  If you use a language model that is built for a specific language and your game is set to that language, the TTS will
  read it normally. The language models are built for the language they are made for.

- **In-Game Text-to-Speech:**  
  Automatically converts chat messages and other text (Rune, Intro, Dream, Raven, Chat etc.) into spoken audio.

- **Local Player Voice:**  
  The TTS audio is played from your player’s AudioSource so it sounds like someone is speaking to you.

- **Automatic Piper Setup:**  
  On first launch, the mod downloads the Piper Windows release, extracts it, and downloads the selected voice model and
  configuration file automatically.  
  By default, it uses the *en_GB-alba-medium* voice (Woman Great Britain). (
  See [Piper VOICES.md](https://github.com/rhasspy/piper/blob/master/VOICES.md) for alternatives. To preview the voices,
  you can use the [Piper TTS Voice Samples](https://rhasspy.github.io/piper-samples).)

- **Fully Configurable Voice:**  
  The mod’s configuration file allows you to change the voice model URL. This means you can use any voice model
  available on the [Piper VOICES.md](https://github.com/rhasspy/piper/blob/master/VOICES.md)

- **Transparent Operation:**  
  Everything is open source and fully documented. If you have any questions or concerns, feel free to check the source
  or ask in my discord.

---

## What is Piper?

[Piper](https://github.com/rhasspy/piper) is an open-source neural text-to-speech engine that uses ONNX for efficient
local inference. It’s optimized for low-resource systems (like the Raspberry Pi) while still delivering high-quality
speech synthesis on modern desktops.  
This mod uses Piper to generate TTS audio:

- **Piper Executable:** Downloaded automatically from the official release.
- **Voice Model & Config:** The mod downloads both the `.onnx` model and its accompanying `.onnx.json` config file to
  ensure proper synthesis.

For a full list of available voices and links, refer
to [VOICES.md](https://github.com/rhasspy/piper/blob/master/VOICES.md)
or [Piper TTS Voice Samples](https://rhasspy.github.io/piper-samples) to preview the voices.

---

## Requirements

- **Valheim (obviously)** (ensure your version is compatible with this mod)
- **BepInEx** installed for your game (It is highly recommended to use a mod manager
  like [r2modman](https://thunderstore.io/c/valheim/p/ebkr/r2modman/), [Gale](https://thunderstore.io/c/valheim/p/Kesomannen/GaleModManager/),
  or if you don't mind ads and supporting the Thunderstore
  team [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager) to install BepInEx
  and manage your mods.)
- An Internet connection for the initial download of Piper and voice files (subsequent usage is offline unless
  configuration change to the URL requires a new download of a voice model).

---

## Installation

1. **Download:**  
   Get the latest version of TextToSpeech from Thunderstore.

2. **Place in Plugins:**  
   Drop the mod’s DLL into your BepInEx/plugins folder. (e.g., `BepInEx/plugins/TextToSpeech.dll`) or use a mod manager
   like mentioned above in requirements.

3. **Launch the Game:**  
   On first run, the mod will:

- Create a local folder for Piper files (within the mod’s directory).
- Download the Piper Windows ZIP release and extract it.
- Download the selected voice model and its configuration file.
- Apply Harmony patches that intercept chat and other messages.

4. **Enjoy:**  
   As you chat or view text, the mod will automatically synthesize and play speech using your local player's
   AudioSource.

---

## Configuration

All settings are stored in `Azumatt.TextToSpeech.cfg` (located in your BepInEx/config folder). You can change these by
manually going to the configuration file directly and opening it with a compatible editor, use the mod manager of
choice's configuration editor, or use the most preferred method which is one of the Configuration
Manager's ([Offical BepInEx Configuration Manager](https://thunderstore.io/c/valheim/p/Azumatt/Official_BepInEx_ConfigurationManager/)
or [Azus UnOfficial Config Manager](https://thunderstore.io/c/valheim/p/Azumatt/Azus_UnOfficial_ConfigManager/))

Key settings include:

- **Voice Model URL:**  
  The URLs for downloading the voice model and its configuration file can be modified if you prefer a different voice.
  The configuration file for the voice will download automatically
  For example, the default uses:
    - Voice Model:
      `https://huggingface.co/rhasspy/piper-voices/resolve/v1.0.0/en/en_GB/alba/medium/en_GB-alba-medium.onnx`
        - To make sure you get the right url, right-click on the model link you wish to use for the language of choice
          and then copy link address. Paste that directly into the configuration value for Voice Model URL. The
          `?download=true` is automatically stripped out in the code. So do not worry about that. If you are live
          updating this value, please give it a few seconds to update, the speed is determined by your internet speed
          and the size of the language model.
          
          ![](https://github.com/AzumattDev/TextToSpeech/blob/master/Thunderstore/model_download.png?raw=true)
      
- **Skip Self In Chat:**  
  If enabled, the mod will not play TTS for your own chat messages. This is useful if you want to hear others but not
  yourself.

- **TTS Playback Options:**  
  The mod always plays TTS via the local player's AudioSource or a one-shot audio source on target transforms/objects.

---

## How It Works

1. **Piper Setup:**  
   Upon startup, the mod checks for the Piper executable, voice model, and configuration file. If any are missing, it
   downloads and extracts them automatically.

2. **Process Management:**  
   The mod launches Piper processes on-demand to generate WAV files from text.

3. **Intercepting Text:**  
   Using Harmony patches, the mod intercepts:

- **TextViewer.ShowText:** All types (Rune, Intro, Raven, etc.) are captured and formatted into a TTS message.
- **Chat.SetNpcText:** NPC chat messages are converted.
- **MessageHud.ShowMessage:** Player messages are intercepted to produce TTS.
- **Terminal.AddString:** Chat messages are intercepted to produce TTS like "PlayerName in chat said: [message]".
- **DreamTexts.GetRandomDreamText:** Dream texts are intercepted to produce TTS.

In each case, the text is combined with the topic (or player name) and then passed to Piper for synthesis.

4. **Audio Playback:**  
   Piper outputs a WAV file, which the mod converts into a Unity AudioClip (via an included WaveToAudioClip). The audio
   is
   then played through the local player’s AudioSource. The wav file is temporarily created for reading and is deleted
   after.

---

## Transparency and Data Usage

- **Local Processing:**  
  All speech synthesis happens locally—no text or audio is sent to external servers.

- **Automatic Downloads:**  
  The mod downloads only the Piper executable, voice model, and configuration file from official sources.  
  Refer to [Piper VOICES.md](https://github.com/rhasspy/piper/blob/master/VOICES.md) for details.

- **Open Source:**  
  The full source code is available on [GitHub](https://github.com/AzumattDev/TextToSpeech). You can inspect, modify, or
  contribute to the project.

---

## Troubleshooting

- **Piper Errors:**  
  Check the mod’s log file in `BepInEx/LogOutput.log` for any Piper error output. Informational messages from Piper are logged as
  debug (a setting in BepInEx.cfg must be on to see these in the LogOutput file).  
  If you see repeated errors, ensure your internet connection was available on first launch so that all files were
  downloaded correctly.

For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/qhr2dWNEYq)
<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>

---

## Credits

- **Azumatt:** Original mod author.
- **Piper:** The open-source TTS engine by Rhasspy.
    - GitHub: [https://github.com/rhasspy/piper](https://github.com/rhasspy/piper)
    - Voices: [VOICES.md](https://github.com/rhasspy/piper/blob/master/VOICES.md)
- **BepInEx & Harmony:** The modding frameworks that make this mod possible.

Mod Icon: <a href="https://www.flaticon.com/free-icons/text-to-speech" title="text to speech icons">Text to speech icons
created by Freepik - Flaticon</a>
<a href="https://www.flaticon.com/free-icon/text-to-speech_9568738?term=text+to+speech&page=1&position=32&origin=tag&related_id=9568738" title="text to speech icons">
Direct link to this mod icon created by Freepik - Flaticon</a>

---

## License

This mod is released under the [MIT License](https://github.com/AzumattDev/TextToSpeech/blob/master/LICENSE.txt). Please review the voice models’ licenses, as some may have
additional restrictions (should you attempt to redistribute them).