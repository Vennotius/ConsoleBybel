namespace LeesBybel;

// The search's scores, settings and word lists, as CrossReferences.org's Bible search has them.

public static class Scores
{
    public const int WholeExact = 100;   // whole word, diacritics as typed
    public const int WholeFolded = 70;   // whole word, diacritics differ
    public const int SubExact = 40;      // substring, diacritics as typed
    public const int SubFolded = 25;     // substring, diacritics differ
    public const int CaseBonus = 5;      // exact case, only for terms containing a CaseWords word
    public const int NearBonus = 50;     // the query words a verse has close together (whole words, any order)
    public const int NearGap = 2;        // at most this many other words between neighbouring query words
    public const double NearCurve = 2.8;   // a group of n steps scores (b^n - 1) / (b - 1) pairs
    public const double NearAgainst = 0.67; // a step against the typed order counts this share of a step
    public const double NearCap = 6;     // the groups together count at most this many pairs
    // "order": the near bonus graded by the gaps between the query words (see GapScore); a step against the
    // typed order counts NearAgainst of a step.
    public const string Proximity = "order";
    public const double WeightFloor = 0.3;   // auto: the commonest word still weighs this much (rarest = 1)
    public const double MinCoverage = 0.5;   // auto: a partial match must cover this share of the query's weight
    // Rank B: occurrences of a word beyond the times it was typed; the k-th adds its score × this^k
    // (0.1: 100 + 10 + 1 ...). 0: they add nothing.
    public const double RepeatFactor = 0;
    // Equal scores: blend70: the average of in70 (70% the in percentile, 30% the out) and the
    // percentile of the links among the results. "none" = Bible order.
    public const string Tiebreak = "blend70";
    // Result order: "score": partial matches interleave with full ones; coverage is only the MinCoverage floor.
    public const string Order = "score";
    // A pair whose verses hold the typed words against the typed order (a word only the first verse
    // has typed after a word only the second has) loses up to 1 - this share of its score, in proportion to the
    // share of such cross pairs.
    public const double PairOrderFloor = 0.85;
    // Mark colours: each typed word gets one of Shades colours by its weight rank in the query;
    // a word weighing under GreyUnder (or past the last colour) is grey.
    public const int Shades = 6;
    public const double GreyUnder = 0.45;

    // Word forms (books/forms.txt): a typed word also finds its
    // listed forms (and a typed form its base) as whole words, scored by relation, always under the exact word.
    // Relations not listed here (false_friend) are not used.
    public static readonly Dictionary<string, int> FormScores = new()
    {
        ["plural"] = 40, ["adj_inflected"] = 40, ["short_form"] = 90,
        ["comparative"] = 40, ["superlative"] = 40, ["diminutive"] = 40,
    };
    // Related words (books/related.txt): scored like a form, but a hit counts only inside a
    // group with a whole-word match of another query word (at most NearGap words between, no . ? ! ; between).
    public static readonly Dictionary<string, int> RelatedScores = new() { ["related"] = 40 };

    // Words where case carries meaning (HERE vs Here, Hy vs hy, God vs god ...).
    public static readonly HashSet<string> CaseWords =
        new(StringComparer.OrdinalIgnoreCase) { "HERE", "Hy", "Hom", "God", "Gees", "Geeste",
            "Ek", "My", "U", "Myself", "Uself", "Naam", "Heilige", "Koning", "Engel", "Lam" };

    // Words that match only as whole words, keyed and listed folded: a hit inside
    // another word counts only when that word is listed, because there the word really is meant.
    // A hyphen is a word break, so "fontein-oë" and "mens-oë" are whole-word hits already.
    // One-letter words ("u", "o") are whole words only too, with no exceptions.
    // An entry ending in * lists every word starting with it ("onder*": onderhou, onderste ...);
    // one starting with ! takes a word out again ("!afek": a name, not af).
    // "*" alone lists every word holding it, so ["*", "!x"] means every word except x.
    // A word holding the key twice counts one hit, the first; brackets name another ("geen[een]": the second).
    public static readonly Dictionary<string, HashSet<string>> WholeWordOnly = new()
    {
        ["oe"] = ["oediens", "oenskou"],    // oë = eyes; not toe, hoe, broeders, vermoë
        ["die"] = ["hierdie", "daardie", "diegene"],              // not dienaars, dienende, diere
        // van = from, of; not ontvang, evangelie, gevangenis, vang, nor the times vandag, vannag ...
        ["van"] = ["daarvan", "waarvan", "hiervan", "daarvandaan", "hiervandaan", "waarvandaan",
            "vanwee", "vandat", "vandaan", "vandaar", "vanwaar", "vanself", "vanaf", "vanmekaar", "vantevore"],
        ["in"] = ["hierin", "waarin", "daarin", "tussenin"],      // not vind, gesind, kinders; "binne-in" is whole
        // every af- word (afgekom, afgode, afstand ...) and compounds holding one; not staf, straf, graf, Asaf
        ["af"] = ["af*", "vanaf", "vooraf", "agteraf", "veraf", "voorafgaande", "leerafdeling", "leerafdelings",
            "silwerafgode", "verafgelee", "verafsku", "onafgebroke",
            "!afek", "!afeka", "!afik", "!afiag", "!afarsegaiete", "!afarsatgiete", "!afarsiete"],
        ["eie"] = ["eiewys", "eiesinnig", "eiesinnige", "eiesinnigheid"],   // not verskeie, boeie, goeie
        // reg = law, justice, a right; not regeer, geregtigheid, regverdig, opreg, regter, regs, regmaak, spysgereg
        ["reg"] = ["regte", "onreg",
            "regsaak", "regsake", "regspraak", "regspreek", "reggespreek", "regbank", "regbanke", "regstryd",
            "regsinsetting", "regsgeding", "regsaal", "regsaanspraak", "regsaansprake", "regsleer",
            "regsverbreking", "regsvergadering",
            "eersgeboortereg", "lossingsreg", "burgerreg", "voorregte",
            "gereg", "geregsdienaar", "geregsdienaars", "geregsdae", "geregstye"],
        // na = after; not naam, nasie, nag, the near sense (naby, nader, naaste, byna), nor names (Jona, Hanna)
        ["na"] = ["daarna", "agterna", "hierna", "waarna",
            "agternagejaag", "agternagesit", "agternajaag", "agternagehoereer", "agternageloop",
            "nadat", "nageslag", "nageslagte", "nakomelinge", "nakomeling", "nakomelingskap", "nakroos",
            "nalaat", "nagelaat", "nalatig", "nalatigheid", "navolg", "navolgers", "navolger", "nagevolg",
            "najaag", "nagejaag", "naspeur", "nagespeur", "nadink", "nagedink", "nagedagtenis",
            "nabestaande", "nabestaandes", "nasoek", "nagesoek", "navraag", "nagevors", "nastreef",
            "nagestreef", "nadoen", "nawandel", "nagee", "nagegee", "nageboorte", "nageveg", "nameet",
            "nasomer", "natrossies", "naderhand", "namate", "nadeel", "benadeel", "nadruk", "nadruklik"],
        // not sonder, honderd, wonder, donder, besonder, verwonder, bewonder, veronderstel
        ["onder"] = ["onder*", "daaronder", "hieronder", "waaronder", "sononder", "lewensonderhoud"],
        // sonder = without; not sonsondergang, lewensonderhoud, besonder, afsonder, uitsondering
        ["sonder"] = ["waarsonder"],
        // of = or, whether; asof and ofskoon hold it; not hoof, offer, stof, profeet, lof, geloof, belofte
        ["of"] = ["asof", "ofskoon"],
        // en = and; no compound holds it (not doen, ken, teen, een, sien, engel, enige)
        ["en"] = [],
        // een = one, also in "together" / "into one" words; not teen, geen, steen, seën, heen, gemeente, weens
        ["een"] = ["een*", "elkeen", "geen[een]", "enigeen", "iedereen",
                   "byeen*", "ooreen*", "aaneen*", "ineen*", "agtereen*"],
        // aan = on, to; every aan- word and aan after a word; not gaan, staan, slaan, baan, maand, Kanaän, Jordaan
        ["aan"] = ["aan*", "waaraan", "daaraan", "hieraan", "vooraan", "onderaan", "agteraan", "langsaan", "naasaan",
                   "onaangeroerd", "regsaanspraak", "regsaansprake", "eerstelingsaandeel", "buitaandeel", "skrikaanjaers"],
        // ster = star; Sewe-ster needs no entry (a hyphen is a word break); not sterf, sterwe, sterk,
        // duisternis, luister, suster, priester, yster, or the -ster of gebiedster, deurwagster
        ["ster"] = ["sterre*", "morester", "moresterre", "dwaalsterre", "stergod"],
        // is: only "dis" (dit is) holds it; not Israel, huis, Christus, kennis, duisend
        ["is"] = ["dis"],
        // ek: no word holds it; not spreek, gekom, plek, trek, soek, mekaar
        ["ek"] = [],
        // as = if, than, when; ash: asof, asook and the ash words; not was, vas, pas, dwaas,
        // gras, asem, Asa, Asaf, Assirië
        ["as"] = ["asof", "asook", "ashoop", "ashope", "asplek", "aspoort"],
        // vol = full; not volk, volke, volg, volgens, gevolg, vervolg, agtervolg, bevolking, volstruis
        // (volgehou and volgemaak start with volg, so they are listed exactly)
        ["vol"] = ["volle", "voller",
            "volbring*", "volkome*", "volheid", "voltooi*", "volmaak*", "voleind*", "volop", "volhard*",
            "voldoen*", "volgehou", "volgemaak", "volgroeid", "volled*", "volleerd", "volwasse*", "voltallig",
            "volmaan", "volmaking", "voltrek", "volstrek", "volmag", "voloffer",
            "handvol", "handvolle", "handevol", "mandjiesvol", "moeitevolle", "hoopvolle", "waardevolle",
            "angsvolle", "vreugdevolle", "troosvolle", "eervolle", "vredevolle", "kragsvolheid"],
        // sal = shall; no word holds it (not psalm, salf, salwing, Salomo, Absalom, Jerusalem)
        ["sal"] = [],
        // eet = eat, also vreet (easily misremembered as eet); not weet, vergeet, profeet, meet, sweet, heet
        ["eet"] = ["geeet", "opeet", "opgeeet", "afeet", "afgeeet", "saameet", "eetware", "eetlus", "eetsaal",
            "vreet", "weggevreet"],
        // wee = woe (weë: labour pains; "my weë" = ways is the same whole word); not wees, twee, weet, weer,
        // vanweë, weens, skaduwee, weduwee, beweeg, week. Left out, not certain:
        // weeklaag, weeklag, geweeklaag, weeklagte (lament, from wee + klaag).
        ["wee"] = ["barenswee"],
        // weer = again, against, ward off, weather: the weer- words, onweer, stormweer, afweer;
        // not sweer, besweer, weerskante, weerlig, weerga, geweer, beweer, rinneweerder
        ["weer"] = ["weer*", "onweer*", "stormweer", "afweer", "afgeweer",
            "!weerskante", "!weerlig", "!weerligte", "!weerligstraal", "!weerga"],
        // nee = no; no word holds it (not neem, wanneer, neer, genees, geeneen, sneeu, Netáneël)
        ["nee"] = [],
        // eer = honour (eerlik: honest, from eer); not eers, eerste, eersgebore, eerder, eergister,
        // eertyds (eer = before), meer, weer, leer, heer, keer, wanneer, bekeer, begeer, Fariseërs
        ["eer"] = ["geeer*", "hooggeeerd", "ongeeerd", "onteer*", "oneer", "oneerbiedig", "oneerlike", "vereer",
            "eerbied*", "eerbaar", "eerbewyse", "eerbetoning", "eervolle", "eerlose", "eerlik*"],
        // neer = down: every neer- word, terneer; not wanneer, meneer, oneer, redeneer, verordineer, ruïneer
        ["neer"] = ["neer*", "terneer*"],
        // my; not vermy
        ["my"] = ["myne"],
        // ons; not gons
        ["ons"] = ["onse"],
        // oor; not hoor, verloor, vermoor
        ["oor"] = ["hieroor", "daaroor", "waaroor"],
        // ken = know; not skend, geskend, teken, reken, geskenk, gelykenis, stukkend, skarlaken, telkens,
        // the -kende forms (blinkende), nor the names Kenas, Kenan, Keniete, Kenchreë
        ["ken"] = ["kennis", "kenne", "kenners", "kenlike", "geken", "erken", "erkentenis", "herken", "onherkenbaar",
            "onkenbaar", "voorkennis", "bekend", "bekende", "bekendes", "onbekend", "onbekende", "onbekendes",
            "ontken", "beken", "verken", "verkenners"],
        // op = on, up: every op- word except the open family (that is oop); not loop, hoop, koop, doop, oop,
        // kop, top, trop, klop, stop, knop, hisop, koper, puinhope
        ["op"] = ["op*", "!open", "!ope", "!opening", "!openbaar", "!openbare", "!openbaarder", "!openbaarmaking",
            "!openbaring", "!openbaringe", "!openlik", "!openlike",
            "daarop", "waarop", "hierop", "hardop", "volop", "sonop", "regop", "regopstaande", "agterop", "gelykop",
            "boonop", "sonsopgang", "handoplegging", "hoofopsigter", "hoofopsigters", "veeopsigters", "volksoploop"],
        // lei = lead; not klein, plein, vleis, paleis, klei, pleit, pleister, vlei, voleindig, allerlei,
        // beleid, geleidelik, aanleiding
        ["lei"] = ["lei*", "gelei", "uitgelei", "uitlei", "weggelei", "weglei", "ingelei", "inlei", "opgelei", "aflei",
            "teruglei", "natgelei", "begelei", "begeleiding", "waterleiding", "musiekleier",
            "verlei", "mislei", "verleiding", "misleiding", "verleier", "verleiers", "verleidende"],
        // oop = open; not loop, hoop, koop, doop, stroop, knoop
        ["oop"] = ["oop*"],
        // om = around, in order to; not hom, kom, boom, oom, droom, stroom, the -dom words, Salomo, Edom, Sodom,
        // stom, krom, blom, bruidegom, nor omstandighede, Omri, Omar, Omega
        ["om"] = ["om*", "!omri", "!omar", "!omega", "!omstandighede", "!omstandig",
            "daarom", "waarom", "hierom", "rondom", "rondomheen", "daaromheen", "onderom",
            "daaromtrent", "hieromtrent", "waaromtrent"],
        // te = to, too, and its fused forms ten, ter (lists don't chain: not tensy, terwyl, terdeë);
        // not teen, eerste, verlate, terug, tent, tempel, teken, tel, the -te forms, teë- (teestander)
        ["te"] = ["ten", "ter", "tegemoet*", "tevore", "tevergeefs", "tesame", "tegelyk", "tegelykertyd", "tevrede",
            "tewens", "tekort", "teweeg*", "tereg"],
        // ten = te + den (ten goede, ten volle); not tent, buite(n), stene, Egiptenaars, gedagtenis, woesteny
        ["ten"] = ["tensy", "tentoongestel"],
        // ter = te + der (ter wille, ter harte); not terug, tereg, terg, priester, water, luister, sterk, sterwe
        ["ter"] = ["terdee", "terneer*", "terwyl"],
        // tel = count; not stel, vertel, mantel, wortel, skottel, apostel, sleutel, telkens, Egipteland,
        // eersteling, -telik, -teloos, the names Telem, Telassar, Telaim, Telag
        ["tel"] = ["getel", "opgetel", "optel", "afgetel", "voorgetel", "geteldes", "getelde", "afgetelde",
            "telling", "teller", "volkstelling", "optelling", "ontelbaar", "ontelbare", "ontelbares"],
        // nie = not: only moenie (moet nie); not niemand, the niet words (nietig, verniet, vernietig, deugniet),
        // knie(l), nier, manier, banier, geniet, verniel, kroniek, the names (Daniel, Macedonie, the -niete)
        ["nie"] = ["moenie"],
        // se/se^ (folded alike): the say words; not the waarse^ family (a different concept),
        // nor mense, Moses, self, see, sewe, oorblyfsel, uself, seders
        ["se"] = ["gese", "aangese", "toegese", "voorgese", "ontse", "hoorse"],
        // see = sea; not seen/geseend (seën), seel/verseel (seël), seer, seep, fariseers, sadduseers, kabseel
        ["see"] = ["skelfsee", "soutsee", "seestrand", "seekus", "seevaarders", "seediere", "kopersee", "seemeeu",
            "seebodem", "kinnerotsee", "kinneretsee", "seedier", "seekoei", "seemonster", "seemonsters", "seetong",
            "seekant", "seeliede", "seegras", "seereis"],
        // nier = kidney (never a whole word in the AOV); not manier, banier, skarniere, tuinier, Sidoniers
        ["nier"] = ["niere", "niervet"],
        // seer = sore, pain; not fariseers, sadduseers, waarseers, trotseer, seereis, Seera
        ["seer"] = ["seermaak", "hartseer"],
        // so: the so words only; not soos, sodat, sowel, sodra, somaar, sodoende, asof,
        // sou, sonder, soek, sonde, son, sommige, sorg, soort, Sodom
        ["so"] = ["soveel", "solank", "sover", "sogenaamde", "sodanige", "sowaar"],
        // lig = light (noun and weight), lift; not the heilig family, salig, veilig, the -willig and -vallig words,
        // plig, stellig, voordelig, the liggaam family (body), inlig, inligting
        ["lig"] = ["ligte", "ligter", "ligstraal", "ligstrale", "ligglans", "ligtende", "morelig", "sonlig", "lewenslig",
            "hemelligte", "helderligte", "weerlig", "weerligte", "weerligstraal",
            "oplig", "opgelig", "aflig", "afgelig", "gelig",
            "verlig", "verligting", "verligte", "ligsinnig", "ligsinnige", "liggroen",
            "ligging", "omliggende", "liggewapende"],
        // ou = old; not jou, nou, vrou, sou, hou, goud, bou, trou, hout, sout, skouer,
        // nor ouers, ouderling(e), verouder(d)(e), voorouers
        ["ou"] = ["oud", "oue", "ouer", "oues", "oueres", "ouds", "oudste", "oudstes", "ouderdom", "oudheid",
            "oudsher", "oudwyfse", "jaaroud", "driejaaroud"],
        // he = hê (have) and its verb forms, het too (people mix up hê and het; a partial hit scores lower);
        // not here, hele, hemel, heilig, the -heid words, the -hebber nouns, hetsy, Sohet
        ["he"] = ["het", "liefhe", "liefhet", "aanhe"],
        // le = lê: every word holding lê, also uitlê (interpret), weerlê, belê, verlê;
        // not hulle, julle, hele, alle, lewe, leer, Leviete, geblêr
        ["le"] = ["gele", "neergele", "opgele", "neerle", "uitgele", "ople", "leplek", "leplekke", "uitle", "uitleer",
            "voorle", "voorgele", "afle", "afgele", "verle", "weggele", "wegle", "weerle", "regle", "reggele",
            "aangele", "aanle", "ingele", "blootgele", "blootle", "bele", "bedleend", "oople", "toegele", "toele",
            "vasgele", "leegleers"],
        // el = the measure; "El-Paran", "Bet-El" are whole words already (hyphen); not Israel, hemel, siel, elke, elf
        ["el"] = ["elle"],
        // al = all, already, even; not sal, val, maal, getal, dal, altaar, alarm, als, alsem, alewee, alef, alfa,
        // albaste, names, nor heeltemal, the aller- words, oral, nogal, almagtige
        ["al"] = ["almal", "alles", "alle", "alleen", "alleenlik", "allenig", "alleenwyse",
            "altyd", "altyddeur", "altydblywende", "altoos", "aldeur", "aldae", "altwee", "albei", "alkante", "altesaam",
            "alhoewel", "alreeds", "alte", "lankal", "veral", "algemeen", "algemene", "alledaagse", "altemit"],
        // ag = the interjection, heed, esteem: the esteem/heed words; not veragter (ver+agter, Heb 12:15), dag, mag,
        // geslag, nag, krag, wag, agter, the agt- words, bedag(saam), aandag, names
        ["ag"] = ["verag*", "!veragter", "geag", "minag", "minagting", "agting", "hooggeag", "hooggeagte", "agbare",
            "veronagsaam"],
        // kon = could, no exceptions; not koning and its family, verkondig and the kondig words, verskoning, skone,
        // oorkonde, names (Ikonium, Konanja, Darkon, Eltekon, Jarkon, Rakkon)
        ["kon"] = [],
        // son = sun: the sun words; not the sonde, sonder and gesond families, persone, names (Nagson, Simson, Gerson ...)
        ["son"] = ["sonop", "sonpilare", "sonsondergang", "sonlig", "sononder", "sonnewyser", "sonsopgang", "sonnetjies"],
        // uur = hour; not vuur, stuur, muur, suur, huur, duur, natuur, skuur, buurman and their families
        ["uur"] = ["halfuur"],
        // ses = six: the six words (listed, not ses*: Sesan, Sesbassar, Sesai, Sesag are names);
        // not Moses, mensesiele, menseseun, mensestem, melaatses, names (Joses, Raamses, Mesesabeel ...)
        ["ses"] = ["sestig", "seshonderd", "sesde", "sestien", "sestigduisend", "sesduisend", "sestienduisend",
            "sestiende", "seshonderdduisend", "seshonderdste", "seshonderden"],
        // bo = above, with boonop, bowendien, onderstebo, bomenslike, bonatuurlike; not verbond, bou,
        // gebooie, gebore, boek, gebod, boodskap, boom, boos, boog, verborge, bode(s) (messenger), the bok, bors, bos words, names
        ["bo"] = ["daarbo", "hierbo", "boonste", "boontoe", "bokant", "bowe", "bovertrek", "bovertrekke",
            "boonop", "bowendien", "onderstebo", "bomenslike", "bonatuurlike"],
        // rou = mourning (and raw, only whole): the mourning words; not the vrou and trou families, berou (regret),
        // jaaroud, verouder, voorouers, winkbroue
        ["rou"] = ["roukleed", "rouklere", "rouklag", "rouklaag", "gerouklaag", "rouklaers", "rougewaad", "rougewade",
            "gerou", "roubedryf", "routyd"],
        // oos = east: every word starting with oos (ooste, oostekant, oostewind, oostelik(e), oosland, oosplein,
        // oospoort); not soos, the boos and troos words, the -loos words, kroos, bloos, altoos, rooster
        ["oos"] = ["oos*"],
        // ry = ride, row: the ride and row words; not the ryk, skryf, kry, prys, vry, dryf, stryd, gryp, kryg words,
        // the -ery nouns, ryp, rys
        ["ry"] = ["rye", "gery", "weggery", "uitgery", "ryperde", "rytuig"],
        // ly = suffer: the suffering words, with lydsaamheid; not medelyde, medelye, medelydend,
        // the bly, lyk, lyn, gelyk, lyf, bely, olyf, lys, slyt, slyp, splyt, gly, vlyt words
        ["ly"] = ["gely", "lyde", "lyding", "hongerly", "hongerlyers", "lydensworsteling", "lydsaamheid"],
        // ver = far: the far words, with verreweg; not verder, verdere, sover, the ver- prefix
        // words, the regverdig family, tevergeefs, the uitverkies words, toevertrou, bloedverwant, omver, geverf, Ahasveros
        ["ver"] = ["verte", "verste", "verre", "veraf", "verafgelee", "verreweg"],
        // kan = can (and jug, a few times): the jug words; not the kant and skande families, kandelaar,
        // sprinkane, skans, kanselier, kaneel, kanaal, kans, ledekant, musikante, kanker, names (Kanaan, Elkana ...)
        ["kan"] = ["kanne", "oliekan", "oliekanne", "waterkan", "klipkanne", "drankofferkanne"],
        // hul, jul = their/them, your/you: the long forms, as with my/myne and ons/onse; not the hulp words, hulde,
        // gehul, omhul(sel) (wrap), Hulda, Halhul, Julius, Julia
        ["hul"] = ["hulle", "hulleself", "hulself"],
        ["jul"] = ["julle", "julleself", "julself"],
        // wa = wagon, chariot: the wagon words; not wat, was, want, waar, swaard, water, wag ..., names (Ahawa, Tikwa ...)
        ["wa"] = ["waens", "strydwa", "strydwaens", "ysterwaens", "tentwaens", "pronkwaens", "oorwinningswaens",
            "afspoelwaentjie", "afspoelwaentjies"],
        // ai = the town Ai, no exceptions; not baie, Efraim, draai, paaie, saai, Sinai ...
        ["ai"] = [],
        // eli = the priest Eli (and "Eli, Eli", Matt 27:46), no exceptions; not evangelie,
        // vreemdeling, dadelik, geliefde, the -elik words, names (Elia, Elisa, Eliab, Elieser ...)
        ["eli"] = [],
        // kap = hew, chop: the hew words; not the -skap words, skape, geskape, Kapernaum, kapiteel, Kappadocie,
        // Dekapolis, kappe (beams), kapperkruid
        ["kap"] = ["afgekap", "afgekapte", "gekap", "gekapte", "ongekapte", "omgekap", "omkap", "uitgekap", "uitgekapte",
            "uitkap", "afkap", "neergekap", "ingekap", "wegkap", "reggekap", "skoonkap", "klipkappers", "houtkappers",
            "houtkapper"],
        // hel = hell: helse; not hele, helde, held, help, helfte, helder, omhels, helm, names (Asahel, Helon ...)
        ["hel"] = ["helse"],
        // preek = preach: gepreek; not the spreek words (spreek, gespreek, toegespreek, spreekwoord ...)
        ["preek"] = ["gepreek"],
        // agt = eight: the eight words, with tagtig; not the agter family, geslagte, magtig, gedagte, kragte,
        // nagte, pragtig, wagter, verwagting, veragting, sagte, klagte, the -agtig words
        ["agt"] = ["agtste", "agttien", "agttiende", "agttienduisend", "agthonderd", "agthonderdduisend", "agtduisend",
            "tagtig", "tagtigduisend", "tagtigste"],
        // ui = onion, never a whole word in the AOV: uie (Num 11:5); not uit, huis, luister, duisend, uil ...
        ["ui"] = ["uie"],
        // lee (leë) = empty: the empty words; not leeglopers, leeglêers, leer, leër, vlees, alleen, leef,
        // leeu, kleed, lees, pleeg, leen, verleë, geleë, names (Besaleël ...)
        ["lee"] = ["leeg", "leeggemaak", "leegmaak", "leegskud", "leegheid"],
        // oes = harvest: the harvest words; not woestyn, moes, verwoes, woes, boesem, toesig, koester, the toe- words,
        // roes, verroes, aalmoes, kalmoes, smeltkroes, gekoes, Joëser
        ["oes"] = ["oestyd", "oeste", "oesfees", "oesdag", "afoes", "afgeoes", "geoes", "geoeste", "koringoes",
            "koringoestyd", "wynoes"],
        // held = hero: the hero words; not helder, helderder, verhelder, helderligte, kristalhelder, Heldai
        ["held"] = ["helde", "heldekrag", "heldedade", "heldemoed"],
        // sing: the singing words; not omsingel(d), singenot (sin + genot), the -sing nouns (verlossing, genesing ...)
        ["sing"] = ["psalmsing", "gesing", "besing", "singende", "lofsing"],
        // ring: the ring words, with omring; not bring and its forms, vergadering, koring, horing, doring,
        // toring, gering, spring, dring, kring, the -ering nouns
        ["ring"] = ["ringe", "seelring", "silwerringe", "vingerringe", "oorringe", "voetringe", "koperringe", "neusring",
            "neusringe", "ringmuur", "ringmure", "omring"],
        // hond, honde = dog(s): the dog words; not honderd and the -honderd words
        ["hond"] = ["honde", "hondjies", "wildehonde", "hondeprys", "hondskop", "herdershonde"],
        ["honde"] = ["wildehonde", "hondeprys", "herdershonde"],
        // ester = Esther, no exceptions; not the priester family, gesterwe, gesterf, meester, verwoester, koester, morester, Beestera
        ["ester"] = [],
        // vang = catch: the catch words, with bevang(e); not the captive family (gevangenis, gevangene,
        // gevange, gevangenskap ...), ontvang, evangelie, omvang, plaasvervanger
        ["vang"] = ["gevang", "vangnet", "vangnette", "vangkuil", "vangkuile", "vangs", "vangtoue", "vangstrikke", "opvang",
            "opgevang", "visvang", "visvanger", "voelvanger", "voelvangers", "lammervanger", "bevang", "bevange"],
        // rug = back: rugge, rugbaar; not terug and the terug- words, the vrug family, gerug(te), berugte,
        // luidrugtigheid, names (Barug, Serug, Rugama)
        ["rug"] = ["rugge", "rugbaar"],
        // lag = laugh: the laugh words; not the slag family (geslag, nageslag, slagoffer ...), the klag family,
        // belagging, vlag, names (Lagis, Siklag, Selag, Metusalag ...)
        ["lag"] = ["gelag", "uitgelag", "toegelag"],
        // elf = eleven: the eleven words; not self and the -self words, selfs, dieselfde, helfte, Skelfsee, hemelgewelf,
        // gedelf, Filadelfia
        ["elf"] = ["elfde", "elfhonderd", "elfhonderdduisend"],
        // dig = close, tight: digte (dense) and verdigte, toegedig; not the -dig words (verkondig, regverdig, sondig,
        // nodig ...) nor the poetry words (gedig, gedigte, digters, spreukdigters)
        ["dig"] = ["digte", "verdigte", "toegedig"],
        // wy = dedicate: the dedication words; not terwyl, wysheid, wyn, wyse, bewys, verwyder, wyk, wyd (wide) ...
        ["wy"] = ["wyding", "inwyding", "tempelwyding", "toewyding", "wydingsram", "wydingsoffer", "wydingsmandjie",
            "inwydingsgawe", "wygawes", "gewy", "gewyde", "ongewyde", "ongewydes", "ingewy", "inwy", "toewy", "toegewy"],
        // uil = owl: the owl words; not kuil, huil, skuil, bruilof, vuil, muil, ruil, Aquila ...
        ["uil"] = ["uile", "naguil", "steenuil", "steunuil", "silweruil"],
        // orde = order: the order words and verordening(e); not woorde, oordeel, noorde, Mordegai, moord, gordel,
        // vorder, slagorde(s) ...
        ["orde"] = ["wanorde", "volgorde", "ampsorde", "priesterordes", "ordelik", "onordelik", "onordelikes",
            "wanordelikheid", "ordening", "ordeninge", "reinigingsordening", "diensordeninge", "verordening",
            "verordeninge", "verorden"],
        // win = win, gain: the gain words and the oorwin family; not twintig, wind, wingerd, winter, the -wing nouns,
        // vuilgewinsoeker(s) ...
        ["win"] = ["gewin", "wins", "handelswins", "winsbejag", "winsgewend", "oorwin", "oorwins", "oorwinning",
            "oorwinninge", "oorwinnaar", "oorwinnaars", "oorwinningswaens"],
        // mee = with, along (mee te deel): the mee- verbs and waarmee, daarmee, hiermee; not meer, gemeente, meester,
        // meel, meet, smeek, kameel ...
        ["mee"] = ["meegedeel", "meedeel", "meegesleep", "meegaan", "meeneem", "meewerk", "meevoer", "waarmee",
            "daarmee", "hiermee"],
        // arm = arm and poor: the poor words and the limb words; not waarmee, daarmee, klaarmaak, barmhartig, warm,
        // alarm, marmer, Karmel, Armoni, Armageddon ...
        ["arm"] = ["arme", "armes", "armoede", "armoedige", "verarm", "arms", "linkerarm", "regterarm", "vegtersarm",
            "omarm", "armholtes", "armband", "armbande", "armkettings"],
        // kop = head: the head words, koppig(e) and wagkop; not koper and the koper- words, skop, koppel, verkoper,
        // loskoping, omkopery, gelykop, Nikopolis ...
        ["kop"] = ["koppe", "leeukoppe", "koppenent", "kaalkop", "hondskop", "eselskop", "kopstuk", "koppig",
            "koppige", "wagkop"],
        // keer = time, turn: the turn words, baiekeer, eenkeer; not bekeer, verkeerd(e), verkeer, skeer and the
        // skeer- words, gekskeer, makeer ...
        ["keer"] = ["terugkeer", "teruggekeer", "omgekeer", "omgekeerd", "omkeer", "afkeer", "afgekeer", "inkeer",
            "uitgekeer", "uitkeer", "toegekeer", "gekeer", "baiekeer", "eenkeer"],
        // res = the rest: no word holding it means that; not Nebukadresar, dienares, sipres, godvresend, bres ...
        ["res"] = [],
        // eeu = age, era: eeue; not leeu, skreeu, sneeu, Hebreeus, seemeeu ...
        ["eeu"] = ["eeue"],
        // lug = sky, air: luggie; not vlug and the vlug words, deurlugtige, Mallug(i)
        ["lug"] = ["luggie"],
        // wond = wound: the wound words; not wonder and the wonder words, opgewonde(nheid)
        ["wond"] = ["wonde", "gewond", "gewondes", "gewonde", "verwonding", "pylwond", "deurwond", "brandwonde",
            "swaardwond"],
        // vel = skin, hide: the hide words and vervel (moult); not bevel and the bevel words, veld and the veld words,
        // vellings, vervelens
        ["vel"] = ["velle", "robbevel", "robbevelle", "ramsvelle", "skaapvelle", "bokvelle", "vervel"],
        // ete = meal: the meal words; not the -iete peoples, profete, voete, beter, getel, meteens, gewete, eter(s) ...
        ["ete"] = ["middagete", "aandete", "etenstyd"],
        // min = few, little: the few/less words; not Benjamin and the names, bemin and the love words, the -ming
        // nouns, vermink, minag, ministers ...
        ["min"] = ["minder", "mindere", "minste", "verminder", "ewemin"],
        // ja = yes: no word holding it means that; not jaar, Jakob, Benjamin, Jesaja, jag, jammer, jaloers ...
        ["ja"] = [],
        // eers = first, only then: every eers word (eerste, eersgeborene, eersteling ...); not leërskare, heers,
        // the -eërs peoples (Fariseërs, Chaldeërs), leers, the neer- and weer- words, waarseërs
        ["eers"] = ["eers*"],
        // stig = found, edify: stigting, gestig; not bevestig, vestig, sestig, ooreenkomstig, haastig, rustig,
        // listig and the other -stig adjectives
        ["stig"] = ["stigting", "gestig"],
        // aas = carrion: aasvoël(s); not naaste, dwaas, laaste, blaas, haastig, verbaas, plaas, the Paas- words, names
        ["aas"] = ["aasvoel", "aasvoels"],
        // sin = mind, sense, desire: the mind/sense words, the -sinnig words, versin(sels), sinnebeeld(e); not the
        // gesind words, sing, sinagoge, Sinai, huisgesin, omsingel, sink, the -sing nouns, enigsins, names
        ["sin"] = ["sinne", "onsin", "waansin", "weersin", "besinning", "sinlik", "sinlike", "sinnelike", "sinsverwarring",
            "singenot", "kranksinnig", "kranksinnige", "kranksinniges", "kranksinnigheid", "ligsinnig", "ligsinnige",
            "onsinnig", "onsinnige", "onsinniges", "eiesinnig", "eiesinnige", "eiesinnigheid", "diepsinnig", "uitsinnig",
            "uitsinnigheid", "waansinnig", "waansinnige", "waansinnigheid", "geheimsinnige",
            "versin", "versinsel", "versinsels", "sinnebeeld", "sinnebeelde"],
        // eens = even, once: meteens, opeens; not eensgesind, eenstemmig, eensaam, weens, gemeenskap, reëns, jeëns
        ["eens"] = ["meteens", "opeens"],
        // reën = rain: every reën word, stortreën(s), lentereëns, gereën; not the ooreen- words, iedereen, agtereenvolgens
        ["reen"] = ["reen*", "stortreen", "stortreens", "lentereens", "gereen"],
        // oue = the old one: oues, ouer, oueres; not ouers, voorouers, vroue, goue, skouer, vertroue, getroue, kloue, toue
        ["oue"] = ["oues", "ouer", "oueres"],
        // oog = eye: every oog word (ooglede, oogappel, oogwink, ooggetuies), regteroog; not the hoog, droog, boog words,
        // landvoogde, soog, loog, gedoog
        ["oog"] = ["oog*", "regteroog"],
        // ewig = forever, eternal: every ewig word (ewigheid, ewige, ewigdurend); not gewig, wederstrewig, hewig,
        // droewig, stewig, vrygewig
        ["ewig"] = ["ewig*"],
        // man = man, husband: the man words; not niemand, iemand, Manasse, Haman and the names, mantel, kommando,
        // mandjie, manier, manna, vermaning, amandel, diamant, verkleurmannetjie
        ["man"] = ["manne", "mans", "mannetjie", "mannin", "jongman", "jongmanne", "jonkman", "manskappe",
            "manspersoon", "manspersone", "manlik", "manlike", "manslag", "mansklere", "krygsman", "krygsmanne",
            "hoofman", "hoofmanne", "voorman", "raadsman", "raadsmanne", "vryman", "vrymanne", "ambagsman", "ambagsmanne",
            "leidsman", "vegsmanne", "stuurman", "stuurmanne", "koopman", "koopmans", "koopmansland", "buurman",
            "timmerman", "werksman", "ontman", "ontmande", "ontmandes"],
        // wil = will, want: the will words; not wild and the wild words, wildernis, the wilger (willow) words, Hawila
        ["wil"] = ["wille", "ontwil", "beswil", "gewil", "willekeur", "willig", "gewillig", "gewillige", "gewilligheid",
            "gewilliglik", "onwillig", "vrywillig", "vrywillige", "inwillig", "ingewillig", "welwillendheid",
            "goedwilligheid", "bereidwilligheid", "kwaadwillige", "kwaadwilligheid", "moedswillig", "moedswillige",
            "moedswilliges", "moedswilligheid"],
        // rots = rock: every rots word, steenrots, steenbokrotse; not trots and the trots words, Kinnerotsee
        ["rots"] = ["rots*", "steenrots", "steenbokrotse"],
        // gewer = giver: raadgewer(s), wetgewer; not gewerp, gewerk and their compounds, gewerf
        ["gewer"] = ["raadgewer", "raadgewers", "wetgewer"],
        // weet = know: geweet; not sweet (sweat)
        ["weet"] = ["geweet"],
        // gee = give: the give words; not vergeef, geen, gees, geëet, vergeet, tevergeefs, regeer, begeer, begeef,
        // the geë- words (geëer, geëis, geëindig ...), geel
        ["gee"] = ["gegee", "oorgegee", "oorgee", "teruggee", "teruggegee", "afgee", "afgegee", "uitgee", "uitgegee",
            "aangee", "aangegee", "opgee", "opgegee", "prysgee", "prysgegee", "ingee", "ingegee", "saamgee", "padgee",
            "padgegee", "weggee", "weggegee", "voorgee", "bygegee", "nagee", "nagegee", "omgee", "toegegee", "raadgeefster"],
        // uit = out: every uit word and the out compounds; not buit, buite, sluit, spruit, skuit, ruiter, fluit, kluit,
        // luit, tuit, snuit, stuit and their words
        ["uit"] = ["uit*", "daaruit", "waaruit", "hieruit", "vooruit", "vooruitgegaan", "vooruitgang", "agteruit",
            "agteruitwyk", "agteruitgewyk", "agteruitgegaan", "agteruitloop", "reguit", "reguitstraat", "ronduit",
            "onuitbluslike", "onuitspreeklike", "onuitputlik", "onuitvoerbaar", "geuit"],
        // leef = live: the live words; not kleef and the kleef (cling) words
        ["leef"] = ["geleef", "leeftyd", "afgeleef", "leefwyse", "opgeleef"],
        // eter = eater (and the town Eter): eters, menseter, kaalvreter(s); not beter, geterg, Jeter, Geter, soeter,
        // the -vergieter and -vergeter words, geldskieter
        ["eter"] = ["eters", "menseter", "kaalvreter", "kaalvreters"],
        // eters = eaters: kaalvreters; not beters, beterskap, godvergeters
        ["eters"] = ["kaalvreters"],
        // las = burden: laste, lasdraers, lasdier(e), belas, oorlas; not belasting (tax), lastig, the laster (slander)
        // words, gelas (commanded), Lasarus, Silas, waterplas, vlas, glas, opgeblase
        ["las"] = ["laste", "lasdraers", "lasdier", "lasdiere", "belas", "oorlas"],
        // goede (mostly "ten goede"): no exceptions; not the goedertieren words, goedere
        ["goede"] = [],
        // sak = bag and sink: the bag words and the sink words; not Isak, Rabsake, Sakkur, Sisak, Sasak, Sakkai,
        // Sakkut, sake, regsake, insake (from saak)
        ["sak"] = ["sakke", "reissak", "leersak", "leersakke", "doedelsak", "wynsakke", "geldsak", "geldsakke",
            "watersakke", "melksak", "slingersak", "handsakkies", "afsak", "afgesak", "wegsak", "weggesak", "gesak",
            "insak", "ingesak", "inmekaarsak", "inmekaargesak", "toegesak", "versak", "afsaksel"],
        // koue = cold: no exceptions; not skouer and the skouer words, aanskouers, herkouer
        ["koue"] = [],
        // buit = plunder: the plunder words; not buite and the buite words (buitekant, buitenste, buiten, buitentoe,
        // buitengewoon, daarbuite ...)
        ["buit"] = ["buitmaak", "buitgemaak", "gebuit", "uitgebuit", "uitbuit", "buitgoed", "buitaandeel", "roofbuit"],
        // loof = praise: geloofd, and geloof (also its past participle: "het die HERE geloof", about 12 verses, though
        // mostly faith); not beloof, verloof(de), geoorloof, kloof, gekloof, ongeloof, the longer geloof- words
        ["loof"] = ["geloofd", "geloof"],
        // raad = counsel: every raad- word, geraadpleeg, the beraadslaag words; not the sieraad and draad words,
        // dageraad, the voorraad words, huisraad, verraad
        ["raad"] = ["raad*", "geraadpleeg", "beraadslaag", "beraadslaging"],
        // kuil = pit: the pit words; not skuil and the skuil words (skuilplek, skuiling, geskuil, wegskuil ...)
        ["kuil"] = ["kuile", "kuiltjies", "vangkuil", "vangkuile", "leeukuil", "waterkuile", "grafkuil", "miskuil"],
        // heer = lord, master: huisheer, opperheer, veldheer(staf), gasheer, geneesheer; not the heerlik and verheerlik
        // words, the heers words (rule), the beheer words
        ["heer"] = ["huisheer", "opperheer", "veldheer", "veldheerstaf", "gasheer", "geneesheer"],
        // wyn = wine: every wyn- word (wynruit too), soetwyn; not verdwyn(ing), the kwyn words
        ["wyn"] = ["wyn*", "soetwyn"],
        // mag = may, power: the power words; not the smag words (versmag, gesmag ...), the names (Magir, Magdalena,
        // Magli, Magpela, Magog, Temag, Armageddon ...)
        ["mag"] = ["magtige", "almagtige", "magtig", "leermag", "maghebbers", "magtiges", "magte", "magteloos",
            "magtiger", "maghebber", "onmagtig", "volmag", "leermagte", "oormag", "wondermag", "magsgebied",
            "magsopenbaring", "ryksmag", "gemagtig", "magtelose", "onmagtige", "grootmagtig", "krygsmag",
            "troepemag", "magtigste", "bemagtig"],
        // ewige = eternal: no exceptions; not wederstrewige(s), hewige, droewige
        ["ewige"] = [],
        // wat = what, which: no exceptions; not water and the water words, watter, aswat
        ["wat"] = [],
        // was = was, wash: the wash words; not dwase, gewasse (crops), oewergewas, volwasse(nes)
        ["was"] = ["gewas", "gewaste", "ongewaste", "waskom", "waskomme", "koperwaskom", "koperwaskomme", "wasbak",
            "wassery", "afwas", "afgewas", "uitwas", "wassers", "wassinge"],
        // here = HERE, lords: geneeshere (as geneesheer under heer); not the names Heres, Héret
        ["here"] = ["geneeshere"],
        // rand = edge: bekerrand, silwerrand; not the brand, verander, strand, skrander words, offerande(s)
        ["rand"] = ["bekerrand", "silwerrand"],
        // bek = beak, mouth (of an animal): bekke; not the be- words, the beker words, Rebekka, bekkens, beke (streams)
        ["bek"] = ["bekke"],
        // erwe = inherit (Neh 8:16 plots): beërwe, geërwe; not sterwe and gesterwe, verwerp, verwek, onderwerp, verwerf,
        // verwelk, bederwe, swerwe, kerwe, skerwe and the rest
        ["erwe"] = ["beerwe", "geerwe"],
        // oom = uncle: ooms; not droom, boom and the tree words, oomblik, stroom, soom, vroom, toom, skroom
        ["oom"] = ["ooms"],
        // rond = round, around: every rond word (rondom, rondloop, ronduit ...), wereldrond; not grond and the grond
        // words, daaronder, hieronder, waaronder, veronderstel
        ["rond"] = ["rond*", "wereldrond"],
        // ryp = ripe, frost: rype, onryp, oorryp; not gryp and the gryp words, begryp, onbegryplik(e), ryperde
        ["ryp"] = ["rype", "onryp", "oorryp"],
        // leeg = empty: the empty words as under lee; not raadpleeg, pleeg and the pleeg words, leeglopers, leeglêers
        ["leeg"] = ["leeggemaak", "leegmaak", "leegskud", "leegheid"],
        // gesel (gésel) = scourge: gegesel, geselslae, geselinge, gesels (4 scourges, 1 talk); not metgesel(le),
        // geselskap, reisgeselskap, vergesel
        ["gesel"] = ["gegesel", "geselslae", "geselinge", "gesels"],
        // nag = night: the night words, nagmaal; not nageslag(te), sinagoge(s), the na- words (nagelaat, nagedink ...),
        // leuenagtig(e), woonagtig, kernagtige, minag and the minag words, names (Nagson, Taänag, Naggai ...)
        ["nag"] = ["vernag", "snags", "nagte", "vannag", "middernag", "nagwaak", "nagwake", "naggesig", "naggesigte",
            "naguil", "nagtelike", "wolkenag", "oornag", "oordeelsnag", "jammernag", "nagdruppels", "nagverblyf",
            "naghut", "naggees", "nagrowers", "nagmaal"],
        // beker = cup: bekers, bekerrand, silwerbeker(s), troosbeker; not bekering
        ["beker"] = ["bekers", "bekerrand", "silwerbeker", "silwerbekers", "troosbeker"],
        // aar = ear (of grain): no exceptions; not maar, daar, haar, aarde, swaard, waar, the -aar(s) nouns ...
        ["aar"] = [],
        // der = of the (in der ewigheid): no exceptions; not kinders, onder, vader, ander, honderd, derde, dertig ...
        ["der"] = [],
        // ere = honour: the ere- compounds; not here, geregtigheid, klere, wereld, diere, berei, erens (somewhere), Ereg
        ["ere"] = ["erekoning", "erenaam", "erediens", "erestoel", "eretroon"],
        // wan = winnow: uitwan, uitgewan, wanners; not want, wandel, wanneer, swanger, wankel, ingewande, dwang, names
        ["wan"] = ["uitwan", "uitgewan", "wanners"],
        // daar = there: the place words and daargestel; not daar + preposition, where daar means dit (daarop = op dit;
        // the preposition's entry keeps those), daardie, daarenteen, sondaar(s), bedaar, kwaadaardig(e), wreedaard
        ["daar"] = ["vandaar", "daarheen", "daarvandaan", "daarnatoe", "daargestel"],
        // waar = where, true: the place words and the true words; not waar + preposition, where waar means wat
        // (waarop = op wat), swaard, bewaar, swaar, waardig and the worth words, waarsku, waarneem, gewaar, the waarsê words
        ["waar"] = ["vanwaar", "waarheen", "waarvandaan", "waarheid", "waarlik", "voorwaar", "waaragtig", "waaragtige",
            "waaragtigheid", "onwaarheid", "bewaarheid", "sowaar", "waargemaak"],
        // hier = here: the place words; not hier + preposition, where hier means dit (hierop = op dit), hierdie,
        // Korinthiërs, Hiërapolis
        ["hier"] = ["hierheen", "hiervandaan", "hiernatoe"],
        // dat = that: the conjunctions (sodat, omdat ...); not soldate, Datan and other names, mitredat
        ["dat"] = ["sodat", "omdat", "nadat", "totdat", "voordat", "deurdat", "vandat", "todat", "hoedat", "noudat"],
        // end = end: no exceptions; not the -end(e) words (bekend, sewende, lewende ...), duisend, vriend, ellende, Endor
        ["end"] = [],
        // gaan = go: every gaan word; not aangaande (concerning), vergaan (perish), begaan (commit), ontgaan
        ["gaan"] = ["*", "!aangaande", "!vergaan", "!begaan", "!ontgaan"],
        // kind = child: every kind word; not skindertaal, beskinder (gossip, slander)
        ["kind"] = ["*", "!skindertaal", "!beskinder"],
        // woord = word: the word words; not antwoord and its forms, teenwoordig and its forms
        ["woord"] = ["woorde", "spreekwoord", "spreekwoorde", "woordestryd", "skeldwoorde", "skrifwoord", "skrifwoorde",
            "woordewisseling", "onheilswoorde", "fluisterwoord", "botterwoorde", "leuenwoord", "smaadwoorde", "woordvoerder"],
        // voor = in front, before, furrow: kept where voor still means that.
        // Clear outs: ivoor, the voort- and voortref- words, the -voorwerp compounds. Out too, though many are close calls:
        // the voor- verbs (voorgeneem, voorgestel ...) and the faded nouns (voorwerp, voorspoed, voordeel, voorbeeld,
        // voorwaar ...).
        ["voor"] = [
            "voorhof", "voorportaal", "voorhangsel", "voorkant", "voorhowe", "vooraan", "voorhoof", "voorpunt", "voorste",
            "voorhoofde", "vooroor", "voorskans", "voorhoofsband", "voorportale", "voorplein", "voorskip", "voorhoede",
            "voorhuis", "voorkante", "voorseil", "voorskote",
            "voordat", "voortyd", "vooruit", "vooraf", "voorgangers", "voorvaders", "voorvaderlike", "voorheen", "voorkennis",
            "vooruitgegaan", "voorafgaande", "voorganger", "voorloper", "voorouers", "voorsabbat", "vooruitgang",
            "voorteken", "voortekens", "voorvy", "voorvye",
            "daarvoor", "waarvoor", "hiervoor",
            "watervoor", "aanvoorsel", "ploegvoor"],
        // trek = pull, move: every trek word (vertrek kept, mostly depart); not the strek words, uitrek (stretch),
        // betrekking, gelaatstrekke, nor vertrek as a room (hoofvertrek, bovertrek ...)
        ["trek"] = ["*", "!strek", "!uitstrek", "!uitgestrek", "!uitgestrekte", "!streke", "!windstreke", "!landstreke",
            "!omstreke", "!verstreke", "!volstrek", "!uitrek", "!betrekking", "!familiebetrekkinge", "!gelaatstrekke",
            "!hoofvertrek", "!slaapvertrek", "!binnevertrekke", "!vertrekke", "!bovertrek", "!bovertrekke"],
        // heil = salvation: heiland, heilsaam, heillose; not the heilig words (holy), nor onheil (disaster) and its words
        ["heil"] = ["heiland", "heilsaam", "heillose"],
        // dien = serve: every dien word (ingedien kept, once served); not verdien (deserve) and its words,
        // bowendien, buitendien (moreover), indien (if)
        ["dien"] = ["*", "!verdien", "!verdienste", "!verdiende", "!onverdienstelike", "!bowendien", "!buitendien", "!indien"],
        // wel = well, indeed: the wel- words for good or well, onwel, vaarwel, weldra, and the conjunctions sowel,
        // hoewel, alhoewel, ewenwel; not geweld, gruwel, duiwel, heuwel, dikwels, kwel, verwelk, swel, swawel,
        // huwelik, sneuwel, wellus and its words, verwelkom
        ["wel"] = ["welbehae", "welgevalle", "welgeluksalig", "welgevallig", "welgevallige", "welgevalliger", "welstand",
            "welbehaaglik", "weldaad", "weldade", "weldadigheid", "welwillendheid", "welvoeglik", "welvoeglike",
            "onwelvoeglik", "welvaart", "welvarende", "welsyn", "welsprekende", "welgemeende", "welgemoed", "welbesproeide",
            "weldoen", "weldoeners", "weldra", "onwel", "vaarwel", "sowel", "hoewel", "alhoewel", "ewenwel"],
        // alle = all: alles, the aller- words, alledaagse; not alleen and its words (alone), the val words (gevalle),
        // walle, stalle, getalle, halleluja
        ["alle"] = ["alles", "aller*", "alledaagse"],
        // are = ears of grain, veins (plural of aar): koringare, waterare; not the -are plurals (skare, jare, pilare),
        // the -bare words, ware, dienares, arend, names (Nasaret, Cesarea)
        ["are"] = ["koringare", "waterare"],
        // rus = rest: the rest words, onrus, verontrus, gerus and its words; not toegerus, uitrusting,
        // wapenrusting (equip), berus (lies with), names (Jerusalem, Petrus, Tirus, Lasarus)
        ["rus"] = ["rustig", "rustige", "rusplek", "rusdag", "sabbatsrus", "rusteloos", "uitrus", "rusbank", "rusbanke",
            "silwerrusbanke", "onrus", "onrustig", "verontrus", "gerus", "geruste", "gerustes", "gerustheid", "gerusstel",
            "gerusstelling", "ongerus"],
        // ent = end, stretch; graft: ente, entjie, koppenent, voetenent, agterent, ingeent; not tent, gemeente,
        // gebeente, fondament, talent, sewentig, omtrent, vorentoe, geleentheid
        ["ent"] = ["ente", "entjie", "koppenent", "voetenent", "agterent", "ingeent"],
        // volg = follow: every volg word, vervolg kept (persecute, but also pursue: "die vyand my dan vervolg");
        // not volgens and its words (according to), vervolgens (then), volgehou, volgemaak, volgroeid (vol-)
        ["volg"] = ["*", "!volgens", "!daarvolgens", "!agtereenvolgens", "!vervolgens", "!volgehou", "!volgemaak", "!volgroeid"],
        // kry = get: verkry, gekry, liefkry, terugkry, klaarkry and their forms; not koninkryk, the skryf words,
        // the kryg- words (war), skrynwerkers
        ["kry"] = ["verkry", "verkryging", "gekry", "liefgekry", "liefkry", "teruggekry", "terugkry", "klaarkry"],
        // weg = away, road: every weg word; not beweging (movement), verreweg (by far)
        ["weg"] = ["*", "!beweging", "!verreweg"],
        // vee = livestock, sweep: the vee words and the sweep verbs; not beveel, veertig, veertien, veel, hoeveel,
        // soveel, graveer, ongeveer, veer
        ["vee"] = ["kleinvee", "grootvee", "slagvee", "veewagter", "veewagters", "veeboere", "veekrale", "veediere",
            "veeopsigters", "wegvee", "afvee", "uitvee", "uitgevee", "uitgeveeg", "weggeveeg"],
        // rig = direct, judge: gerig, rigter, oprig, verrig, aanrig, inrig, uitrig, rigting, onderrig and their forms;
        // not berig (report), the -ig/-erig adjectives (gedurig, vorige, ywerig), the gierig words, Jerigo, gewrig
        ["rig"] = ["gerig", "gerigte", "strafgerig", "strafgerigte", "bloedgerigte", "wraakgerig", "rigter", "rigters",
            "opgerig", "oprig", "oprigter", "wederoprigting", "verrig", "verrigting", "aangerig", "aanrig", "ingerig",
            "inrig", "inrigting", "inrigtinge", "uitrig", "rigting", "riglyn", "onderrig", "onderrigting"],
        // hou = hold, keep; a blow (houe): every hou word, beeldhou- kept; not hout and the wood words, inhoud,
        // houding, verhouding, huishouding
        ["hou"] = ["*", "!hout", "!houte", "!houtjies", "!houtkapper", "!houtkappers", "!houtsnywerk", "!houtstapel",
            "!houtvoorwerp", "!houtjukke", "!houtverhoog", "!houtwerk", "!houtwerke", "!draaghoute", "!akasiahout",
            "!sederhout", "!dwarshout", "!dwarshoute", "!sipreshout", "!brandhout", "!sandelhout", "!kruishout",
            "!oliewenhout", "!oliewenhouttakke", "!dennehout", "!ebbehout", "!goferhout", "!plataanhout",
            "!inhoud", "!inhoudsmaat", "!inhoudsmate", "!houding", "!verhouding", "!huishouding"],
        // oud = old: jaaroud, ouderdom, ouds, oudheid, verouder, oudstes, ouderlinge and their forms; not goud,
        // benoud, eenvoudig and the -voudig words, the hou forms (inhoud, behoud), getroud, vertroud, koud, woud
        ["oud"] = ["jaaroud", "driejaaroud", "ouderdom", "ouds", "oudsher", "oudheid", "oudwyfse", "verouder", "verouderd",
            "verouderde", "oudstes", "oudste", "ouderlinge", "ouderling"],
        // bed = bed: bedde, siekbed, sterfbed, bedlêend, bedding and their words; not gebed, bede, the be- verbs
        // (bedek, bedroef, bedien), bedags, bedel, names (Obed, Abednego, Sebedeus)
        ["bed"] = ["bedde", "beddekamer", "beddegoed", "bedsprei", "siekbed", "sterfbed", "bedleend", "bedding", "beddings",
            "balsembeddings", "terrasbeddings"],
        // mens = person, human: every mens word; not voornemens, namens, Clemens
        ["mens"] = ["*", "!voornemens", "!namens", "!clemens"],
        // Ela = a name: no exceptions; not Egipteland, Sela, melaats, kandelaar, toegelaat, belasting
        ["ela"] = [],
        // mos = new wine; indeed: granaatmos; not Moses, Amos, Kamos and other names, mosterdsaad, mossie
        ["mos"] = ["granaatmos"],
        // ore = ears: no exceptions; not more, gebore, verlore, uitverkore, tevore, spore, the ooreen- words, Kores
        ["ore"] = [],
        // slag = slaughter, blow, battle: the slag words, afslag (flay), geslag (also generation, but often
        // slaughtered); not geslagte, nageslag, geslagsregister, inslag (weft), uitslag (rash), grondslag, verslag
        ["slag"] = ["geslag", "slagoffers", "slagoffer", "slagting", "slagskape", "slagvee", "slagbank", "slaggoed",
            "slaghuis", "slagpaal", "slagplek", "slagdag", "afslag", "doodslag", "manslag", "handslag", "donderslag",
            "slagorde", "slagordes", "slagveld"],
        // hoor = hear: gehoor, verhoor, aanhoor, hoorder, the gehoorsaam (obey = listen to) words; not behoort,
        // toebehoort, hoort (belong, ought), behoorlik (proper)
        ["hoor"] = ["aangehoor", "aanhoor", "gehoor", "gehoorsaal", "gehoorsaam", "gehoorsaamheid", "gehoorsame",
            "geloofsgehoorsaamheid", "hoorder", "hoorders", "hoorse", "ongehoord", "ongehoorsaam", "ongehoorsaamheid",
            "ongehoorsame", "ongehoorsames", "onhoorbaar", "onverhoord", "verhoor"],
        // eed = oath: eedswering; not tweede, breedte, kleed, gereed, reeds, meedeel, wreed, leed
        ["eed"] = ["eedswering", "eedsweringe"],
        // des = of the (des te meer): destyds; not the -des plurals, Herodes and other names, liefdes, vredestyd
        ["des"] = ["destyds"],
        // stel = set, place: the set/place stel words; not herstel (restore), teleurstel, ontstel, stellig, gesteld,
        // ongesteld, apostel, eersteling, the -elik words, distel, worstel, kosteloos
        ["stel"] = ["aangestel", "aanstel", "gestel", "opgestel", "opstel", "vasgestel", "vasgestelde", "vasstel", "bestel",
            "ingestel", "instel", "instelling", "instellinge", "gelykstel", "gelykgestel", "voorgestel", "voorstel",
            "uitgestel", "uitstel", "uitgestelde", "tereggestel", "vrygestel", "vrystel", "vrystelling", "daargestel",
            "stelle", "stelling", "saamgestel", "tentoongestel", "teenoorgestelde", "veronderstel", "gerusstel",
            "gerusstelling"],
        // self = self: the -self and self- words; not selfs (even), dieselfde, selfde (the same)
        ["self"] = ["*", "!selfs", "!dieselfde", "!selfde"],
        // dra = carry: the carry words and the separable verbs (opdra, voorgedra, bygedra: "ek dra die regsaak voor"
        // already holds dra); not verdra, bedra, the nouns opdrag, bydrae, gedrag, verdrag, nor draai, drank, draad, sodra
        ["dra"] = ["gedra", "weggedra", "wegdra", "uitgedra", "uitdra", "rondgedra", "ronddra", "aangedra", "ingedra",
            "draers", "wapendraer", "wapendraers", "lasdraers", "skilddraer", "nuusdraers", "nuusdraery", "draaghoute",
            "draagbaar", "draagbare", "draagstoel", "draagstoele", "draagstok", "drawerk", "drag", "dragtig", "haatdraend",
            "opdra", "opgedra", "voorgedra", "bygedra"],
        // gele = gelê (laid): the -gelê words; not engele, gelei, geleer, gelewer, gelewe, gelees, geleë (situated)
        ["gele"] = ["neergele", "opgele", "uitgele", "weggele", "afgele", "aangele", "voorgele", "ingele", "blootgele",
            "reggele", "toegele", "vasgele"],
        // deel = part, share, divide: all but the oordeel (judge) words, voordeel, nadeel, inteendeel, Abdeel;
        // meedeel kept (separable: "hy deel hulle mee")
        ["deel"] = ["*", "!oordeel", "!veroordeel", "!geoordeel", "!beoordeel", "!oordeelsdag", "!oordeelsnag",
            "!strafoordeel", "!vooroordeel", "!onveroordeeld", "!veroordeeld", "!veroordeelde", "!selfveroordeeld",
            "!voordeel", "!bevoordeel", "!nadeel", "!benadeel", "!inteendeel", "!teendeel", "!abdeel"],
        // sonde = sin: sondes, sondeskuld; not sonder, afsonder, besonder, uitsondering, gesonde, sonsondergang
        ["sonde"] = ["sondes", "sondeskuld"],
        // leen = lend, borrow: geleen, uitleen, ontleen, verleen (grant); not alleen, geleentheid, verleentheid, bedleend
        ["leen"] = ["geleen", "uitleen", "uitgeleen", "ontleen", "verleen"],
        // sit = sit, put: gesit, the separable verbs (neersit, insit, uitsit ...), sitplek, sitting, sittend, hofsittings;
        // not the besit (possess) words, the siter (zither) words, versit (move, resist), names (Sittim, Sitna ...)
        ["sit"] = ["gesit", "neersit", "neergesit", "insit", "ingesit", "uitsit", "uitgesit", "wegsit", "weggesit",
            "agternasit", "agternagesit", "afgesit", "regsit", "reggesit", "vassit", "vasgesit", "aansit", "aangesit",
            "voorsit", "voorgesit", "stilsit", "voortsit", "klaarsit", "opsit", "uiteensit", "uiteengesit",
            "sitplek", "sitplekke", "sitting", "sittend", "hofsittings"],
        // staan = stand: gestaan, the separable verbs (opstaan, toestaan, afstaan, bystaan ...), staande, staanplek, staander;
        // not verstaan and its words, bestaan, ontstaan, weerstaan, deurstaan, nabestaande, aanstaande, buitestaanders
        ["staan"] = ["gestaan", "opstaan", "opgestaan", "vasstaan", "toegestaan", "afstaan", "afgestaan", "bystaan",
            "bygestaan", "oopstaan", "teregstaan", "tereggestaan", "agterstaan", "agtergestaan", "stilgestaan",
            "voorstaan", "gelykstaan", "saamgestaan", "teestaan", "teegestaan", "staande", "opstaande", "regopstaande",
            "staanplek", "staander"],
        // toe = then, to, closed: every toe- word (split rule: "maak die deur toe"), the direction words, waartoe, daartoe,
        // hiertoe; not the faded nouns (toets, toevlug, toekoms, toesig, toegang, toestand ...), vertoef, the stoel words
        ["toe"] = ["toe*", "!toets", "!toetser", "!toevlug", "!toevlugsoord", "!toekoms", "!toekomstige", "!toekomende",
            "!toesig", "!toegang", "!toevallig", "!toeverlaat", "!toebereidsels", "!toestand", "!toestande",
            "!toestemming", "!toesegging", "!toenadering", "!toelating", "!toewyding", "!toenmalige",
            "buitentoe", "boontoe", "vorentoe", "ondertoe", "agtertoe", "binnetoe", "waartoe", "hiertoe", "daartoe",
            "daarnatoe", "hiernatoe"],
        // leer = teach, leather, and (folded) leër = army: the teach words with the scribe and lawyer titles, the army
        // words and beleër (besiege), the leather words; not Galileër, rebelleer, leeglêers, uitleer
        ["leer"] = ["geleer", "leerling", "leerlinge", "leermeester", "leermeesters", "geleerde", "geleerdes", "ongeleerde",
            "geleerdheid", "aangeleerde", "volleerd", "regsleer", "skrifgeleerde", "skrifgeleerdes", "skrifgeleerd",
            "wetgeleerde", "wetgeleerdes", "leerskare", "leers", "leermag", "leermagte", "leerafdeling", "leerafdelings",
            "leerowerste", "leerowerstes", "oorlogsleer", "beleer", "beleering", "beleerde", "beleeraars",
            "beleeringswerke", "leersak", "leersakke", "leergord", "leerwerk", "leervoorwerp", "leerlooier"],
        // hoe = how, and (folded) hoë = high: the how words, the hoëpriester words, hoëveld, hoëres; not hoewel, alhoewel,
        // hoer (folds with hoër, so hoër is lost), the hoek, hoede, behoefte words, hoef
        ["hoe"] = ["hoeveel", "hoeveelheid", "hoeveelhede", "hoedanig", "hoedanige", "hoedat", "hoegenaamd",
            "hoepriester", "hoepriesters", "hoepriesterskap", "hoepriesterlike", "hoeveld", "hoeres"],
        // elke = each: elkeen; not telkens
        ["elke"] = ["elkeen"],
        // ryk = rich, realm: the rich words, the -ryk "rich in" words, koninkryk, doderyk, ryksmag, ryksgebied;
        // not stryk, bestryk, gestryk, vrykom, talryk (numerous)
        ["ryk"] = ["rykdom", "rykdomme", "ryke", "rykes", "ryker", "rykstes", "ryklik", "verryk", "skatryk", "liefderyk",
            "liefderyke", "glansryk", "glansryke", "olieryke", "waterryke", "luisterryke", "skaduryke", "roemryke",
            "koninkryk", "koninkryke", "doderyk", "ryksmag", "ryksgebied"],
        // ben = son (of), in Hebrew names: Benjamin and its words, Ruben and its words, Benhadad, Beninu; the Ben- names
        // are whole already. Not bene (bones; so Bene-Jaakan, Bene-Berak are lost), Benaja, Ebenhaeser, Benob, Benot,
        // Magbena, the Afrikaans words (benoud, bende, benadeel, benede ...)
        ["ben"] = ["benjamin", "benjaminiet", "benjaminiete", "benjaminspoort", "ruben", "rubeniet", "rubeniete",
            "benhadad", "beninu"],
        // ram = the animal (the name Ram is whole already): ramme, bokram, ramshoring, the -ram offerings, stormram;
        // not the "high" names (Abram, Joram, Hiram, Rama, Ramot ...), the Aram words, ramp, kramme, gramskap ...
        ["ram"] = ["ramme", "rammetjie", "bokram", "bokramme", "ramshoring", "ramshorings", "ramsvelle", "wydingsram",
            "skuldofferram", "brandofferram", "versoeningsram", "stormram", "stormramme"],
        // aand = evening: the evening words; not maand and its words, the aan- words (aangaande, aandoen, aandeel ...),
        // the -aande participles (staande, gaande ...), vaandel, afdraand, opdraand, verwaand
        ["aand"] = ["aande", "vanaand", "skemeraand", "aandskemer", "aandskemering", "aandwind", "aandwindjie",
            "aandwolwe", "aandoffer", "aandspysoffer", "aandbrandoffers", "aandskaduwees", "aandete"],
        // nes = nest: neste, voelnes, vasgenestel; not the -enes plurals (gevangenes ...), names (Johannes ...), binneste
        ["nes"] = ["neste", "voelnes", "vasgenestel"],
        // red = rescue: gered, redding, redder, reddende, uitred, uitredding, uitgered; not the vrede words, the rede
        // words, the oortreding words, prediking, prediker, names (Jered, Sered, Mered ...), wrede, erediens, ontredder
        ["red"] = ["gered", "redding", "redder", "reddende", "uitred", "uitredding", "uitgered"],
        // eis = demand: geeis, skuldeiser(s), afeis, afgeeis, opeis, vereis, vereistes; not vleis, paleis, the reis words
        // (ooreis too), the meisie words, keiser, verfoeisel, pleister, bloeisel, teister, the roei words, plaveisel ...
        ["eis"] = ["geeis", "skuldeiser", "skuldeisers", "afeis", "afgeeis", "opeis", "vereis", "vereistes"],
        // wend = turn: afgewend, afwend, gewend, omgewend(e), aanwend (split rule); not aanwending, the lewend words,
        // the -wende adjectives and participles (sewende, bewende, sterwend ...), bowendien, verwende, voorwendsel ...
        ["wend"] = ["afgewend", "afwend", "gewend", "omgewend", "omgewende", "aanwend"],
        // gou = quick: gouer; not the goud words (goud, goue, goudsmid ...)
        ["gou"] = ["gouer"],
        // klei = clay: kleigrond, kleihuise, sierklei, pottebakkersklei; not the klein words, musiekleier
        ["klei"] = ["kleigrond", "kleihuise", "sierklei", "pottebakkersklei"],
        // dof = dull: dofheid; not the -d+offer compounds (brandoffer, sondoffer, skuldoffer ...), Dofka
        ["dof"] = ["dofheid"],
        // baar = give birth (and bier): gebaar; not the -baar adjectives (openbaar, kosbaar ...), the baard words, Baara
        ["baar"] = ["gebaar"],
        // lam = lamb and lame: the lamb words (lammers, ooilam, paaslam, lammervanger ...), the lame words (verlam, lamme,
        // lendelam ...); not the vlam words, the lamp words, names (Elam, Mesullam, Lameg ...), lamed, lama, lamma
        ["lam"] = ["lammers", "skaaplammers", "ooilam", "ooilammers", "lammerooie", "boklam", "skaaplam", "lammervanger",
            "skuldofferlam", "paaslam", "gelam", "dankofferlam", "suiglam", "ooilammetjie", "boklammers",
            "verlamde", "lamme", "lammes", "verlam", "lendelam", "verlamdes"],
        // dan = then, and the tribe Dan (whole already): Daniete; not the dank, dans, onderdaan, -danig words, names
        // (Daniel, Nebusaradan, Paddan ...), adjudant, kommandant, aangedane
        ["dan"] = ["daniete"],
        // wys = show and wise: the wise words (wysheid, wyse, onwyse, alleenwyse, wysgere, wysbegeerte, eiewys), the show
        // words (the separable aanwys, afwys, teregwys and their forms, gewys, aanwysing, wyser, the -wyser nouns); not the
        // bewys words, the onderwys words, teregwysing, the manner words (handelwyse, leefwyse ...), wysie
        ["wys"] = ["wysheid", "wyse", "onwyse", "alleenwyse", "wysgere", "wysbegeerte", "eiewys", "aangewys", "aanwys",
            "afwys", "afgewys", "teregwys", "tereggewys", "gewys", "aanwysing", "wyser", "padwysers", "wegwyser",
            "sonnewyser"],
        // veel = much, many: hoeveel, soveel, the veel- words, hoeveelheid; not beveel, aanbeveel
        ["veel"] = ["hoeveel", "soveel", "veelkleurige", "veelheid", "hoeveelheid", "hoeveelhede", "veelmeer", "veelvuldige",
            "veels", "veelkleuriges", "veelkleurig"],
        // swaar = heavy: swaarder, swaarte, swaarste, the beswaar words; not the swaard words, beklaenswaardig
        ["swaar"] = ["swaarder", "swaarte", "swaarste", "beswaar", "beswaarlik", "beswaard"],
        // werp = throw: gewerp, the separable verbs (neerwerp, wegwerp, opwerp, uitwerp, afwerp, omverwerp ...), werpspies,
        // wegwerpsel; not the verwerp words, the voorwerp words, onderwerp, owerpriester, teewerpinge
        ["werp"] = ["gewerp", "neergewerp", "neerwerp", "weggewerp", "wegwerp", "opwerp", "uitgewerp", "afwerp", "omverwerp",
            "uitwerp", "werpspiese", "werpspies", "toegewerp", "wegwerpsel", "teruggewerp", "opgewerp"],
        // kant = side: every side word; not the vierkant words, ledekant, musikante, gekant
        ["kant"] = ["buitekant", "oorkant", "oostekant", "kante", "binnekant", "westekant", "voorkant", "weerskante",
            "noordekant", "suidekant", "onderkant", "bokant", "anderkant", "linkerkant", "duskant", "agterkant",
            "regterkant", "sykant", "eenkant", "sykante", "alkante", "snykant", "voorkante", "seekant"],
        // hande = hands: the hand words (as hand keeps them, ophande too); not the handel words, allerhande
        ["hande"] = ["mensehande", "handewerk", "ophande", "handevol", "kunstenaarshande", "pottebakkershande"],
        // trou = faithfulness, marry: the faithful words (getrou, ontrou, troueloos, betroubaar ...), the marry words
        // (getroud, ongetroud, troudag ...); not the trust words (vertrou, vertroue, toevertrou, selfvertroue ...)
        ["trou"] = ["getrou", "ontrou", "getroue", "betroubare", "troueloos", "troubreuk", "getrouheid", "betroubaar",
            "ontroue", "ontroues", "getroues", "trouelose", "betroubaarste", "betroubaars", "troueloosheid", "troubreker",
            "onbetroubaar", "getroude", "getroud", "ongetroude", "trougeskenk", "troudag", "ongetroudes", "getroudes",
            "ongetroud"],
        // rein = clean: every rein word except vegterrein, terrein
        ["rein"] = ["*", "!vegterrein", "!terrein"],
        // roer = stir, touch, rudder: verroer, geroer, aanroer, roering, the beroering words, roertoue ...; not the broer
        // words, the ontroer words, the oproer words, Aroer
        ["roer"] = ["verroer", "geroer", "deurmekaargeroer", "deurmekaargeroerde", "aangeroer", "aanroer", "onaangeroerd",
            "roering", "roertoue", "beroering", "beroeringe", "beroerder"],
        // moed = courage, temper: every moed word (by the word, not the sense) except the moeder words, armoede,
        // armoedige, vermoed
        ["moed"] = ["*", "!moeder", "!moederskoot", "!skoonmoeder", "!moeders", "!pleegmoeder", "!moederstad",
            "!moederborste", "!pleegmoeders", "!moederskap", "!moederskote", "!moedermoorders", "!grootmoeder",
            "!armoede", "!armoedige", "!vermoed"],
        // stand: stand's own words only (by form): standhou, standvastig, stande and their forms; every compound
        // with a front word is out (verstand, opstand, teestand, afstand, weerstand, toestand ...), olifantstande
        ["stand"] = ["standhou", "standgehou", "standhoudende", "standvastig", "stande", "standvastige", "standhoudend"],
        // esel = donkey: every esel word (esels, eselin, muilesel ...); not the self words, the gesel words, names
        ["esel"] = ["esels", "eselinne", "eselin", "eselskakebeen", "muilesels", "eselsvul", "eselskop", "muilesel",
            "eselsbegrafnis"],
        // esels: every esel word, as for esel; not the gesels words
        ["esels"] = ["eselskakebeen", "muilesels", "eselsvul", "eselskop", "eselsbegrafnis"],
        // ana: the name only (by form); every containing word out (Manasse, Kanaän, granaat ...)
        ["ana"] = [],
        // niet = nothing: every niet word (vernietig, nietig, verniet, deugniet ...); not geniet (enjoy), the -niet(e) names
        ["niet"] = ["vernietig", "nietige", "nietigheid", "verniet", "deugniet", "vernietiging", "deugniete", "nietig",
            "nietighede", "onvernietigbare"],
        // asa: the name only (by form); every containing name out (Eleasar, Basan, Asarja ...)
        ["asa"] = [],
        // lea: the name only (by form); every containing name out (Gilead, Eleasar, Galilea, Bileam ...)
        ["lea"] = [],
        // ente = ends: ente itself only (by form); every containing word out (gemeente, tente, gebeente ...)
        ["ente"] = [],
        // asie = Asië: the name only (by form); every containing word out (nasie, nasies, Galasië, asiel ...)
        ["asie"] = [],
        // god: every god word (goddelose, afgode, godsdiens ...); not gudgod (a place name)
        ["god"] = ["*", "!gudgod"],
        // dag = day: every day word (vandag, middag, dagvaar ...); not the thought words (gedagte, aandag, bedag), names
        ["dag"] = ["*", "!gedagtes", "!gedagtenis", "!gedagte", "!gedagtig", "!nagedagtenis", "!gedagtenisstene",
            "!onheilsgedagtes", "!gedagtespel", "!aandag", "!aandagtig", "!indagtig", "!bedag", "!bedagsaam",
            "!onbedagsaam", "!onbedagsames", "!onbedags", "!dagon", "!merodag", "!berodag"],
        // val = fall: the words where something falls (geval, aanval, afvallig ...); not welgevalle, beval, bevallig,
        // verval, bouvallig, bevalling, the case and chance words (bygeval, voorval, ongeval), vals, valk
        ["val"] = ["geval", "oorval", "afvallig", "neergeval", "aanval", "afval", "neerval", "inval", "aangeval",
            "afgeval", "ingeval", "afvalliges", "uitval", "wegval", "terugval", "toeval", "agteroorval", "omval",
            "toegeval", "aanvalle", "valstrik", "aanvaller", "valstrikke", "omgevalle", "uitgeval"],
        // werk: every work word, the made things too (reukwerk, erdewerk, vlegwerk ...); not werklik and its family
        // (really, realize), silwer+k..., towerkunste
        ["werk"] = ["*", "!werklik", "!werklikheid", "!verwerklik", "!verwerkliking", "!silwerkom", "!towerkunste",
            "!silwerkomme", "!silwerkandelaars", "!silwerknoppe", "!silwerkettings"],
        // bly = stay and glad: every bly word of either (gebly, oorblyfsel, blydskap, verbly ...); not blyk, geblyk
        ["bly"] = ["*", "!blyk", "!geblyk"],
        // lief = love: every love word (liefde, liefhet, geliefde, lieflik ...); not asseblief (please), geklief (cleft)
        ["lief"] = ["*", "!asseblief", "!geklief"],
        // roep = call: every calling word (geroep, aanroep, roeping, beroep, herroep ...); not the troep and groep words
        ["roep"] = ["*", "!troepe", "!groep", "!groepe", "!troepemag", "!huurtroepe"],
        // lewe = life: every life word (lewende, lewendig, gelewe, lewens- ...); not the lewer words (deliver), klewe,
        // alewee, oorgeblewenes
        ["lewe"] = ["*", "!oorgelewer", "!lewer", "!oorlewer", "!gelewer", "!oorlewering", "!oplewer", "!opgelewer",
            "!uitlewer", "!uitgelewer", "!aflewer", "!afgelewer", "!lewering", "!ingelewer", "!oorleweringe", "!klewe",
            "!aanklewe", "!vasklewe", "!alewee", "!alewees", "!oorgeblewenes"],
        // berg = mountain: every mountain word (berge, gebergte, olyfberg ...); not verberg (hide), the herberg words
        ["berg"] = ["*", "!verberg", "!herberg", "!geherberg", "!herberge"],
        // tyd = time: every time word (altyd, voortyd, tydjie, oestyd ...); not the maaltyd words (meal), tyding (news),
        // partydig, kastyding
        ["tyd"] = ["*", "!maaltyd", "!offermaaltyd", "!wynmaaltyd", "!vrouemaaltyd", "!tyding", "!partydig",
            "!partydigheid", "!onpartydig", "!kastyding", "!kastyders"],
        // bes = best (op sy bes): beste, bestes; not the be- words (besit, besig, beskaamd, besluit ...), names (Jabes,
        // Besaleel ...), the besem words
        ["bes"] = ["beste", "bestes"],
        // beste = best: bestes; not bestem, bestel, bestendig, bestee and their words
        ["beste"] = ["bestes"],
        // lui = lazy: luie, luiheid, luiaard; not the luister, sluit, geluid, sluier, fluit, luiperd words, names
        ["lui"] = ["luie", "luiheid", "luiaard"],
        // les = lesson, quench: no exceptions; not alles, the -les and -self words, lesing (from lees), names
        ["les"] = [],
        // eva = Eve: the name only; not geval, evangelie, gevang, gevaar, gevat and their words, names
        ["eva"] = [],
        // tand = tooth: every tooth word (tande, leeutande, drietand, tandvlees ...); not the stand words (verstand,
        // opstand, teestander, afstand, omstandighede ...)
        ["tand"] = ["tande", "ystertande", "drietand", "drietandvurke", "tandvlees", "olifantstande", "leeutande"],
        // gad = the name Gad: the Gadites; not the vergader words, gader, other names (Asgad, Gadareners, Gaddi ...)
        ["gad"] = ["gadiete", "gadiet"],
        // adam = the name Adam only; not Adami, Adama (places)
        ["adam"] = [],
        // lip: the lip words; not the klip (stone) words, the Filippus names, slip, slippe, glip
        ["lip"] = ["lippe", "lippetaal", "loslippige"],
        // rede = speech, reason: the speech, reasoning, disputing and reasonable words; not the vrede words, the
        // oortreder words, wrede, skrede, optrede, omrede (because), oorredende
        ["rede"] = ["redes", "smaadrede", "drogredes", "geredeneer", "redeneer", "redenering", "redeneringe", "redenasie",
            "redetwis", "geredetwis", "redetwister", "redelose", "redelike", "onredelik"],
        // lees = read: gelees, voorgelees (split rule); not the vlees (flesh) words
        ["lees"] = ["gelees", "voorgelees"],
        // maan = moon: the moon words; not the maand (month) words, vermaan, aanmaan, maanhare
        ["maan"] = ["nuwemaan", "volmaan", "maantjies", "maansiek", "maansiekes"],
        // abel = the name Abel only; not babel, serubbabel, fabels
        ["abel"] = [],
        // dor = withered: the wither words; not the dorp, dors (thirst, thresh), doring, verdorwe words, names (Gedor ...)
        ["dor"] = ["verdor", "verdorde", "uitgedor", "uitgedorde"],
        // arme = poor: armes; not waarmee, daarmee, the karmel and marmer words, warmer, Parmenas
        ["arme"] = ["armes"],
        // egter = however: no exceptions; not the regter, regterhand, vegter words, slegter, opregter
        ["egter"] = [],
        // roede = rod: meetroede, tugroede; not the broeder words
        ["roede"] = ["meetroede", "tugroede"],
        // ruk = snatch, a while: the snatch words (separable verbs by the split rule), ontruk, rukkie, rukwind; not the
        // druk and verdruk words, verrukking (trance), krukke
        ["ruk"] = ["geruk", "uitgeruk", "weggeruk", "afgeruk", "losgeruk", "uitruk", "wegruk", "afruk", "losruk", "opruk",
            "aanruk", "ontruk", "rukkie", "rukwind"],
        // roei = row, root out (the bare word is nearly always uitroei split): the uitroei and rowing words; not the
        // groei, skroei, besproei, broei words
        ["roei"] = ["uitroei", "uitgeroei", "uitroeiing", "geroei", "roeiskepe", "roeispane", "roeispaan", "roeiers"],
        // elfde = eleventh: no exceptions; not dieselfde, selfde
        ["elfde"] = [],
        // meen = think, intend: gemeen (always "thought" in the AOV), welgemeende; not the gemeente, gemeenskap words,
        // algemeen, gemeenheid, meeneem
        ["meen"] = ["gemeen", "welgemeende"],
        // pers = purple, press (and "die Pers", whole already): the press and extort words (afpers by the split rule); not
        // Persië, Perse, the persoon words, the -ers plurals, the koper words, Persis
        ["pers"] = ["wynpers", "gepers", "saamgeperste", "afpers", "afgepers", "afpersing", "afpersinge"],
        // lus = desire: the desire, wellus and verlustig words; not the names (Paulus, Saulus ...), the blus words
        ["lus"] = ["hartelus", "eetlus", "lustig", "strydlustig", "strydlustige", "wellus", "welluste", "wellustelinge",
            "wellustig", "wellustige", "verlustig", "verlustiging", "verlustiginge"],
        // leed = harm, sorrow: leedvermaak; not the kleed words
        ["leed"] = ["leedvermaak"],
        // mis = dung, mist, miss: vermis, gemis, the dung and mist words; not the mis- (wrongly) words, miskien, names
        ["mis"] = ["vermis", "gemis", "mishoop", "mishope", "miskuil", "duiwemis", "beesmis", "mistige", "miswolke"],
        // wes = west: the west words; not the gawes, wese, aanwesig, kwes words, the -wes plurals
        ["wes"] = ["weste", "westekant", "westelike", "westelik", "westewind", "suidweste", "noordweste"],
        // rooi = red: the red words, rooidag; not the strooi and verstrooi words, prooi, lammerooie (lammer + ooie)
        ["rooi"] = ["purperrooi", "bloedrooi", "skarlakenrooi", "vuurrooi", "vuurrooie", "donkerrooi", "rooierige", "rooierig",
            "rooiagtig", "rooier", "rooigeverfde", "rooiminie", "rooigoed", "rooidag"],
        // lof = praise: the praise words, Lofdal (named for praise); not the belofte, gelofte, bruilof words, Selofhad,
        // verlof, voloffer
        ["lof"] = ["lofsang", "lofsange", "loflied", "lofliedere", "lofgesang", "lofgesange", "lofsangers", "lofsing",
            "lofoffer", "lofoffers", "lofdankoffer", "lofwaardig", "loflik", "lofverheffinge", "lofprysinge", "lofdal"],
        // laat = let, leave: gelaat (mostly the participle), the separable verbs (split rule); not verlaat,
        // toeverlaat, the melaats words, plaat, bakplaat, gelaatskleur, gelaatstrekke
        ["laat"] = ["gelaat", "toegelaat", "toelaat", "loslaat", "losgelaat", "agtergelaat", "agterlaat", "oorlaat", "oorgelaat",
            "nalaat", "nagelaat", "vrygelaat", "neergelaat", "oopgelaat", "weglaat", "uitlaat"],
        // ander = other: the other words; not the verander words, teestander, uitlander, landerye, skrander,
        // Alexander ...
        ["ander"] = ["anders", "anderkant", "anderhalf", "anderjaar", "anderdag", "anderdagmore"],
        // saam = together: every saam- word (split rule), altesaam; not the -saam adjectives (gehoorsaam, lydsaamheid,
        // eensaam, werksaam ...)
        ["saam"] = ["saambind", "saambring", "saameet", "saamgaan", "saamgebind", "saamgebring", "saamgee", "saamgegaan",
            "saamgegroei", "saamgekom", "saamgelewe", "saamgeneem", "saamgeperste", "saamgeroep", "saamgeslaan",
            "saamgesleep", "saamgespan", "saamgestaan", "saamgestel", "saamgestem", "saamgestroom", "saamgestry",
            "saamgestuur", "saamgesweer", "saamgetrek", "saamgevat", "saamgevleg", "saamgevoeg", "saamgewerk",
            "saamgewoon", "saamhang", "saamkom", "saamlewe", "saamneem", "saamraap", "saamskraap", "saamsmul",
            "saamspan", "saamstem", "saamstroom", "saamstuur", "saamtrek", "saamverbind", "saamverbonde", "saamvloei",
            "saamvoeg", "saamvoel", "saamwerk", "saamwoon", "altesaam"],
        // raak = touch, hit, become: geraak and the separable verbs (split rule); not the wraak, spraak, draak, braak, kraak words
        ["raak"] = ["geraak", "aanraak", "aangeraak", "vryraak", "vrygeraak", "vrygeraakte", "vrygeraaktes", "losraak",
            "losgeraak", "opraak", "opgeraak", "wegraak", "weggeraak", "vasgeraak", "agterraak", "kwytgeraak", "toeraak"],
        // glo = believe: geglo; not the gloed, gloei words, glorie, names (Eglon, Maglon), dagloner, wegloop, weglok ...
        ["glo"] = ["geglo"],
        // lang = long, along: langs and the along words, langer, langsamerhand; not verlang, the slang words,
        // belang, onlangs
        ["lang"] = ["langs", "langsaan", "daarlangs", "waarlangs", "padlangs", "langer", "langsamerhand", "lewenslange",
            "langsaam"],
        // ween = weep: geween, beween; not weens (because of)
        ["ween"] = ["geween", "beween"],
        // vier = four, celebrate: the four words, the celebrate words (gevier, vierdag, feesvier ...); not the rivier words
        ["vier"] = ["vierde", "vierhonderd", "vierduisend", "vierhonderdduisend", "vierkantig", "vierkant", "viervoetige",
            "viervors", "vierdae", "viervoudig", "vierdubbel", "vierdag", "gevier", "viering", "feesvier", "feesvierende"],
        // eg = harrow, genuine, marriage: the marriage words (egbreuk, egbreek ...), egskeiding, egte, egtheid;
        // not weg, reg, kneg, the Egipte words, egter, the weeg and pleeg words, names ...
        ["eg"] = ["egbreuk", "egbreek", "egbrekers", "egbreekster", "egbreeksters", "egskeiding", "egte", "egtheid"],
        // egte = genuine: onegte; not the regter, slegte, knegte, regte, opregte words, egter, the vegter words ...
        ["egte"] = ["onegte"],
        // ewe = equally: the equal words; not ewenwel, the lewe, sewe, gewees, vergewe, bewe words ...
        ["ewe"] = ["ewewydig", "ewebeeld", "ewemin", "eweredigheid"],
        // vul = fill, foal: the fill words, the vervul words (the sense is still filling), eselsvul; not the
        // -vuldig words (vermenigvuldig, sorgvuldig, veelvuldige ...)
        ["vul"] = ["gevul", "opgevul", "aanvul", "ingevul", "invulling", "steeninvulling", "eselsvul", "vervul", "vervulling",
            "onvervuld", "vervulde", "vervuld"],
        // tref = hit, meet with: getref, aantref (split rule); not betref, oortref, the voortreflik words, Diotrefes
        ["tref"] = ["getref", "aantref", "aangetref"],
        // sluit = close, lock, join: gesluit, the separable verbs (split rule), the sluit- nouns, ontsluit; not the besluit words
        ["sluit"] = ["gesluit", "toesluit", "toegesluit", "toegesluite", "opsluit", "opgesluit", "aansluit", "aangesluit",
            "aaneengesluit", "aanmekaargesluit", "insluit", "ingesluit", "uitgesluit", "buitesluit", "afgesluit", "afsluiting",
            "oopsluit", "wegsluit", "sluitplate", "sluitbalke", "sluitsteen", "ontsluit"],
        // volk = people: every people word (volke, the volks- words, bevolking, ontvolk ...); not the volkome words (vol+kome)
        ["volk"] = ["volke", "volks", "volkie", "volksgenote", "volkstamme", "volksvergadering", "volkstelling", "volkstaal",
            "volkshoofde", "volksoploop", "eiendomsvolk", "diensvolk", "erfvolk", "bevolking", "bevolk", "ontvolk",
            "ontvolking"],
        // soek = seek: gesoek, the separable verbs (split rule), deursoek, ondersoek (sense still searching), the soeker words;
        // not the versoek (tempt, request) and besoek (visit) words
        ["soek"] = ["gesoek", "uitsoek", "uitgesoek", "uitgesoekte", "uitgesoektes", "opsoek", "opgesoek", "nasoek", "nagesoek",
            "rondgesoek", "deursoek", "soekers", "vuilgewinsoeker", "vuilgewinsoekers", "aansoek", "ondersoek"],
        // teen = against: teenoor, daarteen, daarenteen, inteendeel ...; not the teenwoordig and teenswoordig words, meteens,
        // the steen words, the uiteen words
        ["teen"] = ["teenoor", "teenoorgestelde", "daarteen", "daarenteen", "daarteenoor", "hierteen", "waarteen", "inteendeel",
            "teendeel", "onteenseglik"],
        // vers = heifer: no exceptions; every containing word is a ver-s word (verslaan, versamel, verstand ...)
        ["vers"] = [],
        // erg = badly, vex: erger, ergers, ergernis, vererg, geerg (geërg); not the berg, ver-g, neerge-, terg words ...
        ["erg"] = ["erger", "ergers", "ergernis", "vererg", "geerg"],
        // aard = nature, kind: the nature words, ontaard, the -aard person words; not aarde and the earth words,
        // the swaard, daardie, waard, vaardig, baard, bewaarder words ...
        ["aard"] = ["kwaadaardige", "kwaadaardigste", "kwaadaardigheid", "boosaardig", "sagaardige", "ontaard", "ontaarde",
            "luiaard", "grysaard", "grysaards", "trotsaard", "trotsaards", "dronkaard", "dronkaards", "gierigaard", "gierigaards",
            "wreedaard", "snoodaard"],
        // verse: not in the AOV as a word; no exceptions, so it no longer finds verseël, verseker, verset
        ["verse"] = [],
        // uie = onions: no exceptions; not the getuie, sluier, buie, werktuie, kruie words ...
        ["uie"] = [],
        // uile = owls: no exceptions; not the kuile, muile, vuile words
        ["uile"] = [],
        // langs = along: the along words; not langsaam, langsamerhand (under lang), onlangs
        ["langs"] = ["langsaan", "daarlangs", "waarlangs", "padlangs"],
        // tee = teë, against: the teë- words; not teen and its words, steek, verteer, profeteer, steen, steel, onteer ...
        ["tee"] = ["teestanders", "teestander", "teestand", "teegekom", "teekom", "teehou", "teegehou", "teespraak", "teespoed",
            "teespoede", "teegestaan", "teestaan", "teebeeld", "teegegaan", "teespreek", "teesprekende", "teewerpinge",
            "teesprekers", "teepraat", "teeparty"],
        // gek = fool: gekskeer; not the ge-k participles
        ["gek"] = ["gekskeer"],
        // rek = stretch: the rek words; not the trek, strek, gebrek, reken, drek, sprek words
        ["rek"] = ["uitrek", "uitgerek", "uitgerekte", "oopgerek", "gerek"],
        // ink: inkpot; not drink, koninkryk, dink, inkom, links, inkeer ...
        ["ink"] = ["inkpot"],
        // els = awl: no exceptions; not the -els plurals, names
        ["els"] = [],
        // ooi = ewe: the ewe words; not gebooie, gooi, nooit, verstrooi, mooi, dooie, rooi, ooievaar ...
        ["ooi"] = ["ooilam", "ooilammers", "ooilammetjie", "bokooi", "bokooie", "lammerooie", "skaapooie", "takbokooie"],
        // lek = lick, leak: gelek, oplek, opgelek, lekplekke (a ship's leaks); not the plek, vlek, lekker words, Amalek ...
        ["lek"] = ["gelek", "oplek", "opgelek", "lekplekke"],
        // mal = mad: no exceptions; not almal, heeltemal, Amalek, smal, malers, names
        ["mal"] = [],
        // per = by: no exceptions; not perde, koper, boodskappers, dapper, pers, persoon ...
        ["per"] = [],
        // tin: no exceptions; not the -ting nouns, kettings, stink, vorstin
        ["tin"] = [],
        // lem = blade: no exceptions; not Jerusalem, Salem, names, waterlemoene, belemmer
        ["lem"] = [],
        // dom = stupid: domme, domheid; not rondom, the -dom nouns (heiligdom, rykdom, eiendom ...), Edom, Sodom
        ["dom"] = ["domme", "domheid"],
        // tem = tame: getem; not stem, tempel, heeltemal, bestem, Teman ...
        ["tem"] = ["getem"],
        // bree = breë, wide: the wide words; not the breek words, Hebreërs
        ["bree"] = ["breed", "breedte", "breedtes", "breer", "handbreed", "handbreedte", "handbreedtes", "voetbreed"],
        // had: the participles of hê (gehad, liefgehad ...); not the names (Benhadad, Hadad, Selofhad ...)
        ["had"] = ["gehad", "liefgehad", "aangehad", "oorgehad", "omgehad"],
        // uwe = yours: no exceptions; not nuwe, gruwel, skaduwee, weduwee, heuwel, huwelik ...
        ["uwe"] = [],
        // erf = inherit: the erf- words, beërf, geërf; not sterf, verderf, verwerf, verf, gerf, swerf, herfs ...
        ["erf"] = ["erfdeel", "erfdele", "erfenis", "erfenisse", "erfgenaam", "erfgename", "erflike", "erflik", "erfvolk",
            "erfbesit", "erfbesitter", "beerf", "geerf"],
        // pen = peg, pen: the peg and pen words, penwortel; not open, openbaar, wapen, penning, terpentyn ...
        ["pen"] = ["penne", "tentpen", "ysterpenne", "weefpen", "muurpen", "leuenpen", "penwortel"],
        // lis = cunning: the list words; not Filistyne, the Eli- names, beslis, lissies, basilisk ...
        ["lis"] = ["liste", "listig", "listige", "listiges", "listiger", "listigheid", "listiglik", "arglistig",
            "arglistigheid"],
        // ban = the ban: banvloek, bangoed, banoffer, geban, uitgeban, verban and verbanning; not bang,
        // Libanon, Laban, the band and bank words ...
        ["ban"] = ["banvloek", "bangoed", "banoffer", "geban", "uitgeban", "verban", "verbanning"],
        // lae = low, layers: laeveld; not laer (camp), the slae, plae, klae words, hinderlae
        ["lae"] = ["laeveld"],
        // ami: a name; no exceptions (Benjamin, the familie words, names out)
        ["ami"] = [],
        // dek = cover: the cover words, bedek and oordek (sense still covering); not ontdek, Sedekia,
        // noordekant, Melgisedek, ledekant ...
        ["dek"] = ["bedek", "bedekking", "bedekte", "onbedekte", "hutbedekking", "oordek", "oordekte", "toedek", "toegedek",
            "gedek", "dekkleed", "deksel", "versoendeksel", "looddeksel", "dekmantel", "dekens"],
        // verte = distance: no exceptions; not vertel, verteer
        ["verte"] = [],
        // ham: a place; no exceptions (Abraham, Haman, Hamat, hamer, inham out)
        ["ham"] = [],
        // gam = Ham, Noah's son: no exceptions; not liggame, Gamliël, Gamaliël, Pergamus
        ["gam"] = [],
        // luit = lute: no exceptions; not the sluit words, fluit, kluite
        ["luit"] = [],
        // raai = advise: aanraai, aangeraai, geraai; not draai, verraai, kraai, braai, raaisel
        ["raai"] = ["aanraai", "aangeraai", "geraai"],
        // teer = tender, waste away: wegteer, uitgeteer, teerhartige; not verteer, profeteer, onteer, hanteer
        ["teer"] = ["wegteer", "uitgeteer", "teerhartige"],
        // aap: not in the AOV; no exceptions, so not slaap, skaap, wegraap ...
        ["aap"] = [],
        // ape = apes: no exceptions; not skape, geskape, the wapen words, stapel, regskape
        ["ape"] = [],
        // dis = it's ("dis waar"): no exceptions; not dissipels, distels, distrik, medisyne, names
        ["dis"] = [],
        // set = Seth: no exceptions; not insettinge, besete, Mefiboset, opset, verset, setel ...
        ["set"] = [],
        // rys = rise: oprys, opgerys, verrys; not prys, verbrysel, grys, the vry-s words, patrys
        ["rys"] = ["oprys", "opgerys", "verrys"],
        // bal = ball: no exceptions; not ballingskap, Balak, Nabal, simbale, balk, balsem, Gebal ...
        ["bal"] = [],
        // ure = hours: no exceptions; not mure, deure, gedurende, bure, murmureer ...
        ["ure"] = [],
        // klee = kleë, clothe: the clothe verbs; not the kleed (garment) words, the kleef words
        ["klee"] = ["geklee", "gekleed", "beklee", "bekleed", "oorklee", "ontklee", "uitgeklee"],
        // aande = evenings: no exceptions; not aangaande, maande, aandeel, vaandel, staande, aandete ...
        ["aande"] = [],
        // gelee = geleë, situated: verafgeleë; not geleer, gelees, geleentheid, geleef, geleen
        ["gelee"] = ["verafgelee"],
        // laag = low, layer: the low and layer words; not plaag, hinderlaag, the klaag words, beraadslaag, neerlaag, slaag
        ["laag"] = ["laagte", "laagtes", "laagste", "laagheid", "koningslaagte", "voetlaag", "doulaag", "vetlaag"],
        // nat = wet: natgemaak, natmaak, natgelei; not the names (Jonatan, Natan ...), the natuur words, natrossies ...
        ["nat"] = ["natgemaak", "natmaak", "natgelei"],
        // ammi (Ben-Ammi, Lo-Ammi): the names with ammi in the Hebrew (someone may ask where else ammi occurs);
        // not afstamming, Dammim, Sulammitiese
        ["ammi"] = ["amminadab", "ammihud", "ammiel", "ammisaddai", "ammihur", "ammisabad"],
        // bere = bêre, store away: wegbêre, gebêre; not berei, bereik, bereken, Berea, Bered
        ["bere"] = ["wegbere", "weggebere", "gebere"],
        // kou = cold and chew; not aanskou, skouer, beskou
        ["kou"] = ["koue", "koud", "herkou", "gekou", "herkouer"],
        // mes = knife; not armes, Mesullam, sameswering, Mesopotamië, dragmes
        ["mes"] = ["messe", "skeermes", "snoeimesse", "klipmesse"],
        // not struikel, gebruik, kruik
        ["ruik"] = ["geruik"],
        // not veertig, veertien, graveer, ongeveer
        ["veer"] = [],
        // not span, pand, pantser, uitspansel
        ["pan"] = ["panne", "rookpan", "rookpanne", "vuurpan", "vuurpanne", "kopervuurpanne", "wierookpan", "ysterpan", "soutpanne"],
        // the pot words and the pottebakker words; not spot, bespot, pote, Potifar, Mesopotamië, potlood
        ["pot"] = ["potte", "erdepot", "inkpot", "koperpot", "kookpot", "salfpot", "vleispotte", "potskerf", "erdepotskerwe",
                   "potskerwepoort", "pottebakker", "pottebakkers", "pottebakkerskruik", "pottebakkershande", "pottebakkersklei"],
        // not geleentheid, geleenthede (geleen is also under leen)
        ["geleen"] = ["uitgeleen"],
        // not wakker, bakker, pottebakker, swakker
        ["akker"] = ["akkers", "akkerland"],
        // mak = tame; not maker, maklik, gemak, the -making words
        ["mak"] = [],
        // ruis = rustle: geruis; not the kruis words, gedruis (from druis), bruis, volstruis, vergruis
        ["ruis"] = ["geruis"],
        // not in the AOV as a word; not hisop, Mesopotamië, Sopater
        ["sop"] = [],
        // not swakker, swakkerig, swakkere
        ["wakker"] = ["aangewakker"],
        // not in the AOV as a word; not the -kers plurals (werkers, bekers ...)
        ["kers"] = [],
        // not fondament, testament, namens, perkamente
        ["amen"] = [],
        // laas = last (not in the AOV alone): laaste, oplaas; not the blaas and plaas words
        ["laas"] = ["laaste", "laastes", "oplaas"],
        // vere = feathers; not vereer, verenig, vererg, vereis
        ["vere"] = [],
        // enig = one, only, any (not in the AOV alone): enige, enigste, eniggebore, onenigheid, verenig ...;
        // not menigte, vermenigvuldig, the stenig words, groenigheid
        ["enig"] = ["enige", "enigiets", "enigste", "enigeen", "enigsins", "eniggebore", "eniggeborene", "eniger",
                    "onenig", "onenigheid", "allenig", "verenig", "vereniging"],
        // perk = bound, limit (not in the AOV alone): tydperke, beperking; not the koper, purper words, kapperkruid, perkamente
        ["perk"] = ["tydperke", "beperking", "onbeperkte"],
        // eerlik = honest: eerlike, oneerlike; not the heerlik and begeerlik words, onbekeerlike
        ["eerlik"] = ["eerlike", "oneerlike"],
        // not in the AOV; not heerlikheid, begeerlikheid
        ["eerlikheid"] = [],
        // the white words; not Awit
        ["wit"] = ["witter", "gewitte", "witbrood", "witmarmer", "wittes"],
        // ys = ice: no exceptions; not wys, spys, prys, yster, grys, rys, paradys ...
        ["ys"] = [],
        // reuk = scent: the reuk- words; not spreuk, spreuke, the breuk words
        ["reuk"] = ["reukwerk", "reukaltaar", "reukofferaltaar", "reukoffer", "reukoffers", "reukflessies"],
        // not in the AOV; not broer, voer, hoer, vloer, roer, jaloers ...
        ["oer"] = [],
        // not dikwels, the prediking words, Dikla
        ["dik"] = ["dikte", "diktes", "dikker", "dikke", "dikmelk"],
        // not oniks, sardoniks, feniks
        ["niks"] = [],
        // pas = fit, watch, step: oppas, toepas by the split rule, passe; not the names (Pasga, Pashur, Kleopas ...), passer
        ["pas"] = ["oppas", "opgepas", "oppasser", "oppassers", "gepas", "toepas", "toegepas", "passe", "pasgebore"],
        // mas = mast, and Mas (a name); not Damaskus, Amasia, Thomas, massa ...
        ["mas"] = [],
        // not vlieg and its words
        ["lieg"] = ["belieg", "gelieg"],
        // os = ox; not soos, Moses, goddelose, verlos, los, oos, bos, troos ...
        ["os"] = ["osse", "osdrywerstok", "osprikkels"],
        // not verskaf, afgeskaf, aanskaf, Kaftor
        ["kaf"] = [],
        // not the skat words, Helkat, katoen, names
        ["kat"] = ["boskat", "boskatte"],
        // not kassie (cassia), akasia, kasty, Lukas, kastrol, karkas
        ["kas"] = ["borskas"],
        // bas = bark, bass; not basuin, Basan, Barnabas, verbasing, baster, base
        ["bas"] = ["basstem"],
        // kus = coast, kiss, Cush: the Cushite words; not Damaskus, Markus, Tichikus, Kusan, Kusi, Kusaja, kussing
        ["kus"] = ["kusland", "kuslande", "kusstreek", "seekus", "kusbewoners", "kusse", "kusiet", "kusiete", "kusitiese"],
        // not in the AOV alone; not woordestryd, deurboordes
        ["ordes"] = ["priesterordes", "slagordes"],
        // not verwagting, slagting, afwagting, versagting
        ["agting"] = ["minagting", "veragting"],
        // lyk = corpse, seem; not the gelyk words, blyk, slyk
        ["lyk"] = ["lyke"],
        // not in the AOV; not maar, maak ...
        ["ma"] = [],
        // not in the AOV; not pad, Paulus ...
        ["pa"] = [],
        // mat = weary: afgemat, afmat; not mate, uitermate, matrose, aanmatigend, onmatigheid, names
        ["mat"] = ["afgemat", "afmat"],
        // sat = weary of; not Satan, names
        ["sat"] = [],
        // not the duiwel words
        ["duiwe"] = ["tortelduiwe", "duiweverkopers", "duiwemis"],
        // the roll words; not the vrolik words, kastrol
        ["rol"] = ["boekrol", "skrifrol", "gerol", "aanrol", "opgerol", "afrol", "weggerol", "rollende", "wegrol", "afgerol",
                   "toerol", "toegerol"],
        // not in the AOV; not menigte, enige, reinig ...
        ["nig"] = [],
        // regtig = really; not the geregtigheid words, geregtig (entitled)
        ["regtig"] = [],
        // not bepaal, bepaalde, Elpaäl
        ["paal"] = ["slagpaal", "vlagpaal"],
        // not in the AOV alone; not woord, moord, noord, the oordeel words, oordek, oordag ...
        ["oord"] = ["toevlugsoord"],
        // not in the AOV; not woorde, noorde, oordeel ...
        ["oorde"] = [],
        // als = wormwood: wildeals, alsem (the same plant); not the vals words, jakkals, hals, Baals, the balsem words
        ["als"] = ["wildeals", "alsem"],
        // tuig = gear: rytuig, werktuig; not getuig, oortuig, betuig
        ["tuig"] = ["rytuig", "werktuig"],
        // not the skerpioen words
        ["skerp"] = ["skerpte", "skerper", "skerpgemaak", "inskerp", "swaardskerptes"],
        // not the stand words (teestander, verstandeloos, opstande, toestande)
        ["tande"] = ["ystertande", "leeutande", "olifantstande"],
        // vy = fig (not in the AOV alone): the fig words; not the vyand, vyf words, vywer, vysel
        ["vy"] = ["vye", "vyeboom", "vyebome", "wildevyebome", "wildevyeboom", "voorvye", "voorvy", "navye", "vyekoek",
                  "vyekoeke", "vyeblare"],
        // not heuwel, sneuwel and their words
        ["euwel"] = ["euwels"],
        // not vark, mark, Markus, park, bymekaarkom, Arkiet, names
        ["ark"] = ["verbondsark"],
        // mores = mornings; not morester, moreskemering, morestond, Moreset
        ["mores"] = [],
        // not the stop words, stoppel, topaas
        ["top"] = ["toppe", "bergtop", "bergtoppe"],
        // not in the AOV; not the -naar nouns (dienaar, eienaar ...), snaar, Naara
        ["naar"] = [],
        // not the wolk words, wolf, wolwe
        ["wol"] = ["wolvlies"],
        // not steel, gesteel, kapiteel, Jokteël
        ["teel"] = ["aanteel", "aangeteel"],
        // not gestalte, kristal, diefstal
        ["stal"] = ["stalle", "skaapstal"],
        // tal = number (not in the AOV alone): the number words; not talente, tale, talm, betaling, vertaling, names
        ["tal"] = ["getal", "getalle", "aantal", "talryk", "talryke", "talryker", "sieletal", "sewetalle", "tienduisendtalle",
                   "voltallig"],
        // wig = wedge, not in the AOV; not the ewig words, gewig, gelowig, hewig ...
        ["wig"] = [],
        // not the verderf and verderwe words
        ["verder"] = ["verdere"],
        ["rut"] = ["gerut"],
        // not gronde, daaronder, the ondeurgrondelik words
        ["ronde"] = [],
        // laste = burdens; not the laster words
        ["laste"] = [],
        // tale = languages; not talent, talente, voorportale
        ["tale"] = [],
        // not kloue, klou, the louter words, blou, verflou
        ["lou"] = [],
        // gaar = cooked, and opgaar (store up); not gierigaard, sagaardige
        ["gaar"] = ["opgaar", "opgegaar"],
        // not medelyde, skedel, besoedel, bedel, moedeloos, vredeliewend, redelose ...
        ["edel"] = ["edele", "edeles", "edelste", "edelgesteente", "edelgesteentes", "hoogedele", "onedel", "onedels",
                    "edelmoediger"],
        // not doedelsak, edelste
        ["edels"] = ["onedels"],
        // not jammerklagte
        ["merk"] = ["bemerk", "gemerk", "opmerk", "opgemerk", "aanmerking", "brandmerk", "gebrandmerk", "merkteken",
                    "skeepsmerk", "opmerksaam", "opmerksaamheid", "opmerksame"],
        // not the vrugbaar words
        ["rugbaar"] = [],
        // wed = wager, and the contest words; not the weduwee and weder- words, sewe-
        ["wed"] = ["wedloop", "wedstryd", "wedywer", "gewedywer"],
        // bel = the idol Bel only; not babel, the be- words, jubel, dubbel, names
        ["bel"] = [],
        // kool = coal; not skool
        ["kool"] = ["koolvuur", "vuurkool"],
        // poel = pool; not the spoel words
        ["poel"] = ["vuurpoel"],
        // innig; not the -sinnig words, vinnig
        ["innig"] = ["innige"],
        ["innige"] = [],
        // sig = reflexive; not the sight words (gesig, insig, sigbaar), besig, kruisig, aanwesig, names
        ["sig"] = ["sigself"],
        // pad = path; not paddas, names, kappadocie
        ["pad"] = ["grootpad", "padkos", "padlangs", "padgee", "padgegee", "leuenpad", "kruispad", "padwysers"],
        // skep = create and scoop; not skepe (ships)
        ["skep"] = ["geskep", "skepping", "skepper", "skepsel", "skepsele", "skepding", "opgeskep", "uitskep"],
        // gereed = ready; not the gereedskap (tools) words
        ["gereed"] = ["gereedmaak", "gereedgemaak", "gereedheid"],
        // baard: not openbaarder, kosbaarder
        ["baard"] = ["baarde"],
        // kos: food and cost; not kosbaar, kostelik (precious), names
        ["kos"] = ["gekos", "koste", "onkoste", "kosteloos", "bekostig", "padkos", "meelkos"],
        // reens: not ooreenstemming
        ["reens"] = ["stortreens", "lentereens"],
        // voorwaar: not voorwaarde
        ["voorwaar"] = [],
        // seen: seën; not waarseënde
        ["seen"] = ["geseen", "geseend", "geseende", "geseendes", "seeninge", "seening", "seenwens", "afskeidseen"],
        // sou: not the sout words
        ["sou"] = [],
        // dal: valley; not dalk, dalet, names, neerdaling
        ["dal"] = ["dale", "soutdal", "dalpoort", "handwerkersdal", "moorddal", "koningsdal", "lofdal"],
        // prys: praise and price; not oprys
        ["prys"] = ["geprys", "losprys", "koopprys", "prysgee", "prysgegee", "huweliksprys", "hondeprys", "bruidsprys", "lofprysinge", "saligprysing"],
        // met: not meteens, vermetel, metgesel, metaal, besmet, names
        ["met"] = [],
        // sake: not Rabsake, insake
        ["sake"] = ["regsake"],
        // telling: not the stel words
        ["telling"] = ["volkstelling", "optelling"],
        // wet: law; not the know words (gewete, wete, onwetend), names
        ["wet"] = ["wette", "wetboek", "wetgeleerde", "wetgeleerdes", "wetsinsetting", "wetlike", "wettige", "onwettige", "wetteloosheid", "wettelose", "wetteloos", "wetgewing", "wettiglik", "wettelik", "wetgewer"],
        // wek: verwek; not weke, kweker
        ["wek"] = ["gewek", "opgewek", "opwek", "opwekking", "skrikwekkend", "skrikwekkende", "ontsagwekkende", "verwek", "verwekker"],
        // weke: not kweker
        ["weke"] = ["jaarweke"],
        // sag: not gesag, ontsag, the -agtig words, names
        ["sag"] = ["sagte", "sagter", "sagtheid", "saggies", "versag", "versagting", "sagmoedig", "sagmoedige", "sagmoediges", "sagmoedigheid", "sagaardige"],
        // sap: not liefdesappels
        ["sap"] = ["lewensap", "druiwesap"],
        // stap: not the stapel words
        ["stap"] = ["voetstap", "voetstappe", "wegstap", "voortgestap", "misstap", "oorstap", "aanstap"],
        // staf: not vleestafels
        ["staf"] = ["veldheerstaf", "ysterstaf", "herderstaf", "werwerstaf"],
        // stam: tribe and trunk; not stamp, testament
        ["stam"] = ["stamme", "boomstam", "boomstamme", "stamhoof", "stamhoofde", "stamboom", "stamgenote", "stamgenoot", "stamvader", "volkstamme", "stamowerstes", "stamgedeeltes", "afstam", "afstamming", "afstammeling", "afstammelinge"],
        // stad: not stadig
        ["stad"] = ["vrystad", "stadspoort", "stadsplein", "palmstad", "bloedstad", "stadsmuur", "stadsbesitting", "vaderstad", "dawidstad", "stadsowerhede", "soutstad", "stadsbestuurder", "priesterstad", "moederstad", "koningstad", "waterstad", "stadsdeel", "godstad", "stadshoogtes", "stadjie", "stadsgewoel", "stadsklerk"],
        // skat: not boskat
        ["skat"] = ["skatte", "skatkamer", "skatkamers", "skathuis", "skatkis", "skatmeester", "skatmeesters", "skatryk", "skatting", "geskat", "geskatte"],
        // aarde: not daardeur, waarde, swaarde, bejaarde ...
        ["aarde"] = [],
        // vaar: go, sail; not gevaar, aanvaar, ervaar, wedervaar, dagvaar, the vaardig words
        ["vaar"] = ["opgevaar", "afgevaar", "uitgevaar", "oorgevaar", "weggevaar", "ingevaar", "deurgevaar", "opvaar", "afvaar", "uitvaar", "oorvaar", "wegvaar", "seevaarders", "bedevaartslied", "vaart", "skeepvaart", "vaarwel", "bevaar"],
        // weeg: not beweeg, oorweeg, teweeg
        ["weeg"] = ["geweeg", "afgeweeg", "afweeg", "opgeweeg", "opweeg", "ongeweeg", "onweegbaar", "weegskaal", "weegstene", "weegtoestel"],
        // volle: not the -volle adjectives, volledig
        ["volle"] = ["voller", "handvolle"],
        // rye: rows; not speserye, hoererye, landerye, vrye ...
        ["rye"] = [],
        // stry: not the stryd words, the stryk words
        ["stry"] = ["gestry", "bestry", "saamgestry", "stryery", "bestryding", "bestryder"],
        // want: not the verwant words
        ["want"] = [],
        // vore: not tevore, vorentoe
        ["vore"] = ["watervore"],
        // erger: not the -gereedskap words
        ["erger"] = ["ergers", "ergernis"],
        // iets: not rietstok
        ["iets"] = ["enigiets"],
        // ook: not the rook, kook, stook, spook words
        ["ook"] = ["asook"],
        // asof: not paasoffer
        ["asof"] = [],
        // meer: not Arameërs, formeer, smeer
        ["meer"] = ["veelmeer", "nimmermeer", "meerdere", "meerderheid", "vermeerder", "vermeerdering"],
        // bou: bouval; not the boud words
        ["bou"] = ["gebou", "opgebou", "opbou", "opbouing", "bouers", "herbou", "herbouer", "bouwerk", "aanbousel", "boustof", "ingebou", "boustyl", "onderbou", "boumeester", "boumeesters", "bousteen", "bebou", "bouplan", "boufonds", "ombou", "toebou", "geboue", "landbou", "landbouer", "landbouers", "landboustede", "geboude", "bouvalle", "bouvallige"],
        // boud: not geboude (under bou)
        ["boud"] = ["regterboud", "hefboud"],
        // week: soft and soak too; not the weeklaag words, opgekweek
        ["week"] = ["bruilofsweek", "weekheid", "deurweek"],
        // gerig: judgment and directed; opgerig, aangerig, ingerig; not the honger words, begerig
        ["gerig"] = ["strafgerig", "strafgerigte", "wraakgerig", "gerigte", "bloedgerigte", "opgerig", "aangerig", "ingerig"],
        // angel: not the evangelie words, aangele-, triangels
        ["angel"] = ["angels"],
        // liste: not Filistea, evangeliste
        ["liste"] = [],
        // wens: verwens; not the lewens- words, tewens, benewens, hawens
        ["wens"] = ["gewens", "wense", "wenslik", "toewens", "seenwens", "verwens", "verwensing"],
        // kere: not sekere, swakkere, names
        ["kere"] = [],
        // tye: as tyd: not the maaltye words, partye
        ["tye"] = ["feestye", "geregstye"],
        // melk: not hemelkoningin
        ["melk"] = ["dikmelk", "melkkoeie", "soetmelk", "melksak", "beesmelk", "bokmelk"],
        // pars: not names
        ["pars"] = ["parskuip", "parskuipe"],
        // rant: not verantwoording, rantsoen, trant
        ["rant"] = ["bosrante"],
        // jaar: bejaard; not names
        ["jaar"] = ["jaaroud", "jubeljaar", "verjaarsdag", "jaarlikse", "jaarliks", "anderjaar", "sterfjaar", "driejaaroud", "jaarweke", "bejaarde", "bejaardes", "hoogbejaard"],
        // jare: not Jareb
        ["jare"] = ["lewensjare"],
        // mane: not Salmaneser, Getsemane
        ["mane"] = ["nuwemane"],
        // ligte: not the -pligte words
        ["ligte"] = ["ligter", "ligtende", "weerligte", "hemelligte", "helderligte", "verligte"],
        // bene: not benewens, beneweld, benede
        ["bene"] = ["kakebene", "mensbene", "ribbebene", "springbene", "doodsbene"],
        // rib: not names, ribbok
        ["rib"] = ["ribbes", "ribbebene"],
        // wie: not the wiel and wierook words, names
        ["wie"] = [],
        // wei: graze; not the weier words, weinig
        ["wei"] = ["weiveld", "weivelde", "weide", "weiding", "afwei", "gewei", "afgewei", "weiplek", "weiplekke"],
        // wye: not stilswyend, swye
        ["wye"] = [],
        // ringe: not geringe, dringend, the -ering plurals
        ["ringe"] = ["silwerringe", "vingerringe", "oorringe", "voetringe", "koperringe", "neusringe"],
        // skoon: in-laws; not verskoon, ofskoon
        ["skoon"] = ["skoonheid", "skoonheidsmiddele", "skoonste", "skoonmaak", "skoongemaak", "skoonkap", "skoonvader", "skoondogter", "skoondogters", "skoonmoeder", "skoonseun", "skoonseuns", "skoonsuster"],
        // by: not the verby words, byt, byl, robyne, bydrae, bywoner, byvrou, bynaam, byna, verbyster
        ["by"] = ["naby", "nabyheid", "naderby", "daarby", "waarby", "byderhand", "bymekaar", "bymekaargekom", "bymekaarmaak", "bymekaarkom", "bymekaargemaak", "bymekaargeroep", "bymekaarbring", "bymekaargeskraap", "bymekaargebring", "bymekaarskraap", "bymekaarvat", "byeen", "byeengekom", "byeenkom", "byeengeroep", "byeengebring", "byeenkoms", "byvoeg", "bygevoeg", "bygestaan", "bystaan", "bygekom", "bykom", "bygeval", "byval", "bygedra", "bygegee", "byneem", "byslaap", "bybehorende", "bye", "perdebye"],
        // byt: not verbytrek
        ["byt"] = ["gebyt", "vasgebyt"],
        // tent: not buitentoe, tentoongestel
        ["tent"] = ["tente", "tentdoeke", "tentdoek", "tentwoning", "tentseil", "tentbewoners", "tentpen", "tentwaens", "tentdorpe", "tentdeur", "tenthuis", "tentlyn", "woontent", "herderstent", "tentwonings", "paleistente", "tentmakers"],
        // skei: not the onderskei words, verskeie, beskeiden, jukskeie
        ["skei"] = ["geskei", "afskei", "afgeskei", "geskeie", "afgeskeie", "skeiding", "skeibrief", "afskeid", "afskeidseen", "afskeidsgeskenk", "egskeiding", "skeidsmuur", "onafskeibaar", "skeidsregter", "skeidsregters"],
        // tang: not stange
        ["tang"] = [],
        // tange: not in the AOV
        ["tange"] = [],
        // bak: bake and bowl, pottebakker as under pot; not names, baken
        ["bak"] = ["gebak", "gebakte", "bakkers", "bakker", "baksters", "bakoond", "bakoonde", "bakoondtoring", "bakplaat", "bakwerk", "bakstene", "baksteen", "bakkerstraat", "bakke", "bakkies", "bakskottel", "bakskottels", "drinkbak", "drinkbakke", "reenbak", "reenbakke", "klipbakke", "graanbakke", "wierookbak", "wasbak", "pottebakker", "pottebakkers", "pottebakkerskruik", "pottebakkershande", "pottebakkersklei"],
        // bakke: not the bakker words
        ["bakke"] = ["drinkbakke", "reenbakke", "klipbakke", "graanbakke"],
        // agter: the behind words, agtervolg; not the wagter words, jagter, veragter, sagter, agterdog, agtereenvolgens
        ["agter"] = ["agtervolg", "agternagejaag", "agterbly", "agterna", "agterkant", "agterhoede", "agtergelaat", "agteruitwyk", "agteruit", "agtergebly", "agtergehou", "agterhoeke", "agternagesit", "agteruitgewyk", "agtertoe", "agterste", "agternajaag", "agternagehoereer", "agternageloop", "agterdele", "agteruitgegaan", "agternaloop", "agterlaat", "daaragter", "agteroor", "agtervolging", "agterskip", "agterstaan", "agteruitloop", "agteroorval", "agterraak", "agternahoereer", "agterop", "agtermiddag", "agternasit", "agterent", "agteraan", "agteraf", "agterhou", "agterkom", "agtergestaan"],
        // wyk: not beswyk
        ["wyk"] = ["afgewyk", "afwyk", "gewyk", "agteruitwyk", "agteruitgewyk", "ontwyk", "afwyking", "wegwyk", "weggewyk", "buitewyke"],
        // ophou: not oophou
        ["ophou"] = ["onophoudelike", "onophoudelik"],
        // hof: not Hofni, Hofra
        ["hof"] = ["voorhof", "hofdienaars", "hofdienaar", "hofsittings"],
        // smaak: not losmaak, vasmaak, mismaak
        ["smaak"] = ["gesmaak", "smaaklik", "smaaklike"],
        // gespe: not gespeel, gespeen, the speur words
        ["gespe"] = ["vasgespe", "gespes"],
        // ur: Ur only
        ["ur"] = [],
        // sara: Sarai; not Nebusaradan
        ["sara"] = ["sarai"],
        // lot: not lote, the slot words, blote, plotseling, komplot, names
        ["lot"] = ["noodlot", "lotgeval", "lotgevalle", "lotsverandering", "verloting"],
        // sem: Shem only
        ["sem"] = [],
        // laan: not in the AOV; not the slaan words
        ["laan"] = [],
        // leers: leërs; leërskare; not leersak, Galileërs
        ["leers"] = ["leerskare"],
        // moet: not the tegemoet and ontmoet words (meet)
        ["moet"] = [],
        // veraf: not verafsku
        ["veraf"] = ["verafgelee"],
        // wen: win; not verwen, the lewend, sewende, wend, wens words
        ["wen"] = ["gewen"],
        // hare: not Hareset, Haref
        ["hare"] = ["kameelhare", "hoofhare", "maanhare", "kuifhare", "vrouehare"],
        // hom: not Thomas, homer, Homam
        ["hom"] = ["homself"],
        // sy: his, her and side; not syd (wyd en syd), tensy, hetsy, asyn, rosyne, welsyn
        ["sy"] = ["syne", "sykamers", "sykamer", "sykant", "sykante", "opsy", "symuur", "symure", "regtersymuur", "sye", "sypaaie", "syposte", "wedersydse"],
        // myn: not komyn
        ["myn"] = ["myne", "mynskag"],
        // selfs: not selfsug
        ["selfs"] = [],
        // siek: not the musiek words, meisiekinders
        ["siek"] = ["siekte", "siektes", "siekes", "sieke", "siekbed", "pessiektes", "maansiek", "maansiekes", "sieklike", "sieklikes", "bemoeisiek", "praatsiek"],
        // pes: not speserye, gespes
        ["pes"] = ["pessiektes", "pestilensies"],
        // ope: not the open words, openbaar, koper, hope, lope
        ["ope"] = [],
        // dae: not Pedael
        ["dae"] = ["lewensdae", "vierdae", "aldae", "feesdae", "geregsdae", "herfsdae", "werkdae", "kindsdae"],
        // verniet: not the vernietig words
        ["verniet"] = [],
        // anders: not teestanders, uitlanders, branders
        ["anders"] = [],
        // boor: not the geboorte and boord words, inboorlinge
        ["boor"] = ["deurboor", "deurboordes"],
        // lyf: not the olyf words, verblyf, oorblyfsel
        ["lyf"] = ["lyfwag", "onderlyf", "lyfdoek", "lyfrok"],
        // lyke: not the gelykenis words, gelyke, tegelykertyd
        ["lyke"] = [],
        // lende: not ellende, the -lende participles
        ["lende"] = ["lendene", "lendelam"],
        // bele: belê; not the beleër words, beledig, belet, dubbele
        ["bele"] = [],
        // emmer: not belemmer, swemmer
        ["emmer"] = ["emmers", "maatemmer"],
        // kinder: not skindertaal, beskinder
        ["kinder"] = ["kinders", "mensekinders", "kindertjies", "kinderloos", "kinderloosheid", "kinderlose", "kindskinders", "hoerkinders", "kindergeboorte", "troetelkinders", "kleinkinders", "meisiekinders"],
        // stem: voice and agree; not the bestem words, the ontstem words, gestemd, Estemoa
        ["stem"] = ["stemme", "sopraanstemme", "basstem", "donderstem", "mensestem", "eenstemmig", "toegestem", "saamgestem", "toestemming", "saamstem", "instem", "ooreenstemming"],
        // talm: not Talmai, Talmon
        ["talm"] = ["getalm"],
        // waai: not the swaai words, kwaai
        ["waai"] = ["gewaai", "verwaai", "verwaaide", "deurwaai", "wegwaai"],
        // wonde: wounds; not the wonder words, opgewonde
        ["wonde"] = ["brandwonde", "gewonde", "gewondes"],
        // sondes: not gesondes
        ["sondes"] = ["sondeskuld"],
        // dier: not dierbaar
        ["dier"] = ["diere", "gedierte", "gediertes", "ongedierte", "ongediertes", "seediere", "seedier", "lasdier", "lasdiere", "roofdiere", "woestyndiere", "veediere", "pakdier", "pakdiere", "diereriem", "dierehart", "offerdiere"],
        // naam: not sogenaamde, naamlik, erfgenaam, aangenaam, vernaam, hoegenaamd, names
        ["naam"] = ["gedenknaam", "erenaam", "bynaam"],
        // name: not erfgename, vername, aangename, namens, names
        ["name"] = [],
        // etter: not verpletter, letter, ketterye
        ["etter"] = [],
        // skim: not the skimp words
        ["skim"] = ["skimme"],
        // het: not the Hetiete words, names, hetsy
        ["het"] = ["liefhet"],
        // bare: billows; not the barende words, the -bare adjectives, barbare
        ["bare"] = [],
        // bye: not the byeen words
        ["bye"] = ["perdebye"],
        // vliee: vlieë; not vlieënde, opvlieënde
        ["vliee"] = ["steekvliee"],
        // besig = busy; not besigtig (inspect)
        ["besig"] = [],
        // dale = valleys; not Magdalena, dalet
        ["dale"] = [],
        // lap = patch; not slap, klap, verslap, Lappidot
        ["lap"] = ["lappe", "aangelapte", "gelapte", "oorlappie"],
        // wis = knew: gewis; not twis and the twis words, the wissel words, uitwis (wipe out), verkwis
        ["wis"] = ["gewis"],
        // ding = thing: dinge, skepding; not the -ding nouns (oortreding, opstanding, tyding, leiding, redding ...)
        ["ding"] = ["dinge", "skepding"],
        // los = loose, release, redeem: every los word, the verlos words, gelos, aflossing; not the -lose adjectives
        // (goddelose, sorgelose ...), Apollos, kolosse, klossies, verwaarlosing
        ["los"] = ["los*", "verlos", "verlosser", "verlossers", "verlossing", "verlossinge", "verlostes", "gelos",
            "aflossing"],
        // maak = make: every maak word except the taste/pleasure words and volmaak (perfect)
        ["maak"] = ["*", "!smaak", "!smaaklik", "!smaaklike", "!gesmaak", "!vermaak", "!leedvermaak", "!volmaak",
            "!volmaakte", "!volmaakter", "!volmaaktheid"],
        // hand: every hand word except the handel (trade, deal) words, allerhande, langsamerhand, naderhand, handhaaf
        ["hand"] = ["*", "!handel", "!handelaar", "!handelaars", "!handeling", "!handelinge", "!handelsaak",
            "!handelsartikel", "!handelsartikels", "!handelsgebied", "!handelshuis", "!handelsware", "!handelswins",
            "!handelwyse", "!gehandel", "!afgehandel", "!behandel", "!mishandel", "!mishandeling", "!mishandelinge",
            "!onderhandel", "!verhandel", "!koophandel", "!allerhande", "!langsamerhand", "!naderhand", "!handhaaf"],
        // kom = come, bowl: every kom word except kommer, kombers, komkommer, kommando, komplot, komyn, Milkom,
        // volkome, ooreenkoms, bekom, voorkoms, inkomste, uitkoms
        ["kom"] = ["*", "!kommer", "!bekommer", "!bekommerd", "!bekommernis", "!kombers", "!komberse", "!komkommers",
            "!komkommertuin", "!kommando", "!kommandant", "!laerkommandant", "!roofkommando", "!komplot", "!komplotte",
            "!komyn", "!milkom", "!volkome", "!volkomelik", "!volkomenheid", "!ooreenkoms", "!ooreenkomstig",
            "!ooreengekom", "!ooreenkomende", "!bekom", "!voorkoms", "!inkomste", "!uitkoms", "!uitkomste"],
    };

    // The bracketed entries (WholeWordOnly key -> word -> where in it the hit is), taken out of the lists as plain words.
    public static readonly Dictionary<string, Dictionary<string, int>> MarkAt = Marks();

    static Dictionary<string, Dictionary<string, int>> Marks()
    {
        var marks = new Dictionary<string, Dictionary<string, int>>();
        foreach (var (key, only) in WholeWordOnly)
            foreach (var x in only.Where(x => x.Contains('[')).ToList())
            {
                string word = x.Replace("[", "").Replace("]", "");
                only.Remove(x);
                only.Add(word);
                if (!marks.TryGetValue(key, out var m)) marks[key] = m = [];
                m[word] = x.IndexOf('[');
            }
        return marks;
    }

    public static bool Listed(HashSet<string> only, string word) =>
        !only.Contains("!" + word) &&
        (only.Contains(word) || only.Any(x => x[^1] == '*' && word.StartsWith(x[..^1], StringComparison.Ordinal)));
}
