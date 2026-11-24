# ConsoleBybel

Die Bybel in Afrikaans as ’n volskerm-terminaalprogram: lees, blaai en soek, sonder internet. Een
klein program, `lees-bybel`, met die hele teks binne-in.

**In English:** a full-screen terminal reader for the Afrikaans Bible, with offline search, for
Windows, Linux and the Raspberry Pi. One native program with the text built in; its interface is in
Afrikaans. Download it from [Releases](../../releases). The code is MIT.

### Lees
<img width="700" height="573" alt="image" src="https://github.com/user-attachments/assets/cbe0e378-7461-44b0-b572-97addbf8a2c9" />

### Soek
<img width="707" height="573" alt="image" src="https://github.com/user-attachments/assets/de7211ac-96d8-4eb2-8949-cf34607f71d8" />

### Hulp
<img width="702" height="573" alt="image" src="https://github.com/user-attachments/assets/80960948-b75c-481a-8d06-c3e77b1edc5c" />

## Aflaai

By [Releases](../../releases), een lêer vir elke soort rekenaar:

| Lêer | Vir |
|---|---|
| `lees-bybel-win-x64.zip` | Windows 10 of 11 |
| `lees-bybel-linux-x64.tar.gz` | Linux (Debian 12, Ubuntu 22.04 of nuwer) |
| `lees-bybel-linux-arm64.tar.gz` | Raspberry Pi, met die 64-bis Raspberry Pi OS (Bookworm of nuwer) |

**Windows:** pak die zip uit en maak `lees-bybel.exe` oop. Die program is nie digitaal onderteken
nie, so Windows sal waarsku. Kies *More info* en dan *Run anyway*.

**Linux en Raspberry Pi:**

```
tar -xzf lees-bybel-linux-x64.tar.gz
cd lees-bybel-linux-x64
./lees-bybel
```

In ’n terminaalvenster werk dit op Linux soos op Windows. Die kaal konsole van ’n Raspberry Pi
(sonder ’n lessenaar) het sy eie kleure in die program, maar dit is nog nie daar getoets nie.

## Gebruik

`lees-bybel` begin by Genesis; `lees-bybel psa` by die Psalms, `lees-bybel jhn` by Johannes.

| Sleutel | Wat dit doen |
|---|---|
| Op, Af | Een ry op of af |
| PgUp, PgDn, Spasie | Een bladsy op of af |
| Home, End | Begin of einde van die boek |
| `/` | Gaan na ’n teks (`joh 3:16`, `ps 23`), of soek (sonder syfers) |
| `b` | Kies ’n boek en hoofstuk |
| `f` | Soek, of terug na die laaste soektog |
| `v` `c` `h` `s` `k` | Versnommers, hoofstuknommers, opskrifte, statusbalk, kleur |
| `?` | Al die sleutels |
| `q` | Sluit |

In die soeklys: `b` kies boeke, `o` wissel gelykes tussen verwysings en Bybelorde.

## Soek

Tik die woorde wat jy onthou; die verse wat die beste pas, kom eerste. Verse met net ’n deel van die
woorde tel ook, en ander vorme van ’n woord word saam gevind (*broers* vind *broer*).

| Tik | Vind |
|---|---|
| `"in die begin"` | Die woorde so agtermekaar |
| `+liefde` | Net verse met die woord |
| `-vrees` | Net verse sonder die woord |
| `=man` | Net die hele woord, sonder ander vorme |
| `==Gees` | Ook hoofletters en aksente net so |
| `lewe*`, `*heid`, `ge*nes` | `*` staan vir enige letters |
| `vrees \| bang` | Die een of die ander |

## Bou dit self

Nodig: die [.NET 10 SDK](https://dotnet.microsoft.com/download), en op Linux `gcc` en
`zlib1g-dev`, of op Windows die C++-gereedskap van Visual Studio.

```
dotnet publish -c Release -r linux-x64
```

Of `-r linux-arm64` of `-r win-x64`, elk op sy eie soort rekenaar. Die program kom in
`bin/Release/net10.0/<soort>/publish`.

## Lisensie

Die kode: MIT ([LICENSE](LICENSE)).
