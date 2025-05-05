# ***Emne 9 - Prosjekt oppgave***

##  Prosjekt - <ins>BLJ Games</ins>

Prosjektet er utviklet I en Blazor Web app og baserer seg på at brukere skal kunne spille kjente spill rett i nettleser.\
Brukere skal kunne ha mulighet til å logge seg inn/registrere seg som medlem\
og med det få muligheten til å ta få brukernavnet og score knyttet på et visuelt LeaderBoard, samt brukernavn i spill chat.\
Brukere kan også velge å delta i spill og chat som gjest.
***
### <ins>Problemstilling:</ins>
Hvordan utvikle spill i browser som inneholder tilkobling mellom to spillere samt en chat en chat\
og har bruker-innlogging, validering og score som lagres i database.

Prosjektet benytter .NET rammeverket med C# som programmeringsspråk, MySQL og entity framework core for databaseoppsett,\
med et tilhørende controller-basert API-design, samt SignalR for sanntidskommunikasjon. JWT for authentication og authorization.\
Hovedutfordringene for prosjektet har vært balansen mellom å ha tid nok til å tilegne seg ny kunnskap\
og samtidig levere til satt tid, samt holde seg innenfor de rammene til opprinnelig prosjektplan.\
Det har i de fleste tilfeller vært god kommunikasjon og samkjøring av hva som skulle bli prioritert\
og hvilke valg som har blitt tatt.

Sluttproduktet er en Blazor web app der brukere kan velge å registrere seg/logge inn,\
spille BattleShip eller Connect4 mot hverandre, få opp sin score på et leaderboard,\
snakke med andre spillere mens spillet er i gang, eller flere sammen i en større chat.

Eventuelle forbedringer ville eksempelvis være <ins>følgende:</ins>
- Legge til rette for at brukere kan velge motspiller/ spille mot venner.
- Se hvor mange brukere som er pålogget.
***
## Innledning
Ved oppstart vil bruker bli introdusert til prosjektets forside,\
her vil bruker få informasjon om hvem utviklerne er, hva siden inneholder og hva som kommer.\
Derfra kan man videre navigere til registrering, innlogging, spill, chat, leaderboard og forum.

Et av spillene bruker kan velge mellom er en versjon av BattleShip, der to brukere kan spille mot hverandre i klassisk stil.\
Hver bruker vil ha muligheten til å sette opp sitt eget brett før spillet starter.\
Når spillet er i gang vil hver bruker ha oversikt over sitt eget brett,\
samt status på hvilke skip hos en selv som er sunket, og hvilke skip bruker har klart å synke på motstanderens brett.\
For hvert utløste skudd vil bruker kunne se på egen oversikt over motstanders brett – om det var treff eller bom,\
øvrig informasjon om motstanders skipsplassering er skjult.

Når to spillere kobler seg til, pares de av GameService basert på rekkefølgen de ankommer.\
Hver spiller identifiseres med sin connectionId, som brukes til å sende meldinger og oppdatere spill tilstand.\
I praksis vil dette også tillate at man kan spille mot seg selv på hver sin nettleserfane dersom man ønsker dette,\
å ta i bruk spillet på denne måten kan gi rom for hukommelsestrim.

Når spillet er i gang, vil det også lastes inn en spill-chat, her kan spillerne kommunisere med hverandre mens de spiller.\
Begge spillere starter med tap, spilleren som vinner vil få tapet annullert og et poeng for vunnet spill.\
Taperen vil holde den scoren den startet med.

Dersom en bruker ønsker logge inn/registrere seg kan dette gjøres ved å navigere til login/register siden.\
Her vil bruker få valget om å benytte google innlogging, eller registrere seg som bruker og deretter logge inn utenom google-involvering.\
Hver bruker som registrerer seg, vil føres gjennom en valideringssjekk slik at det ikke havner uferdig/uheldig data på feil plass.\
Ved innlogging vil hver bruker bli gitt en JWT Access Token som oppdateres via gitt refresh Token,\
slik vil brukeren bli holdt innlogget gjennom ulike navigerings-prosesser og sessions (spill, leaderboards, chat, forum).

***
**Målsettingen med utviklingen var å kunne produsere et enkelt spill der to spillere kunne spille og snakke sammen**\
**samt ha mulighet til å opprette sin egen bruker på siden og se spillerscores.**\
**Prosjektets målgruppe er foreløpig alle og enhver som er brettspill-inspirerte.**
***

## Arbeidsmetodikk og verktøy
Som utviklingsmetodikk har vi gått for noe Agile lignende,\
vi har hatt en form for daily-standups over nett og ukentlige møter på skolens-campus.\
Under møtene og underveis har vi tatt vurderinger på hver vår kode-modul,\
og forsøkt å tilpasse hverandres moduler underveis.\
I flere tilfeller har det forekommet at kode-moduler har blitt refaktorert.

For versjonskontroll og versjonshåndtering har vi benyttet Git og GitHub.

Prosjektet inneholder 2 integrasjonstester og 4 unit-tester.

Discord og Microsoft Teams er brukt som kommunikasjonsverktøy under prosjektet,
det ble også satt opp et Trelloboard for oversikt.
***
## Rammeverk, Teknologistakk og implementasjon

### <ins>Språk</ins>
- C#
- JavaScript
- CSS

### <ins>Rammeverk</ins>
- ASP.NET Core med Blazor Web App (server).
- .NET 8.0

### <ins>Frontend/UI</ins>
- JavaScript
- Blazor WebApp (Server)
  - .razor
  - C#
  - css)

### <ins>Backend</ins>
- ASP.NET Core (C#)
- Blazor WebApp (Server)

### <ins>Sanntidskommunikasjon</ins>
- SignalR (websockets)

### <ins>Autentisering & Autorisering</ins>
- JWT

### <ins>API-Sikkerhet/Robust</ins>
- Global exception handler
- JWT,
- EF Core
- Fluent Validation
- API-Key
- IP-adresse

### <ins>Datatilgang</ins>
- REST API
- EF Core
- MySQL

#### Grafiske elementer og lyd effekter:
Det er brukt lyd og grafikk for å supplementere til design av spill, og overall side design.\
Graffik er selvprodusert mens lydfilene er hentet fra et offentlig bibliotek.\
Disse elementene ar plassert i wwwroot og består av:
- .png (2D)
- .gld (3D)
- .mp4 (Video/Bakgrunn)
- .wav (Audio)
- .js (Script for kontroll)
***
## Arkitektur og komponenter

### Oversikt spillmoduler:
Spill-modulene er bygget som sanntids flerspillerapplikasjoner i Blazor,\
kommunikasjon mellom klient og server håndteres via SignalR.\
Spillerne blir paret basert på deres connectionId\
og det opprettes en delt GameSession hvor turer,\
skudd/trekk og sluttstatus synkroniseres i sanntid.

### Hver spillmodul består av følgende komponenter:

### **<ins>Klientside</ins>** (Blazor):
- **BattleShip.razor / Connect4.razor:**
  - Design,UI -brukerinteraksjon.\
    Her er det også benyttet CSS for å gi elementene en animert og interaktiv effekt.
  - JavaScript brukes for å trigge ulike lydeffekter når ConnectionHandlers trigges,\
    samt kontroll over bakgrunns animasjon.
  
### **<ins>Server-side logikk</ins>** (undernevnte serivce og hubconnections har tilhørende interfaces):
  - BattleShipComponents.cs / Connect4Components.cs:\
    - Spillregler og validering.
  - GameService.cs / ConnectFourGameService.cs:\
    - Håndtering av venteliste, GameSession, turstyring og spillerlogikk.
  - GameHub.cs / ConnectFourGameHub.cs:\
    - SignalR-hub som eksponerer metoder og håndterer klientkoblinger. Kommunikasjonsnode på serveren.
  - GameHubConnection.cs / ConnectFourGameHubConnection.cs:\
    - Klient-side wrapper for SignalR – håndterer sending og mottak av meldinger
***
## Eksempel på dataflyt
### <ins>Oppkobling:</ins>
> Når en spiller åpner BattleShip.razor eller Connect4.razor,\
opprettes en tilkobling mot riktig SignalR-hub.

🠋
> Spilleren tildeles automatisk en unik ConnectionId av SignalR,\
som brukes til å identifisere brukeren under hele spilløkten.

🠋
### <ins>Matchmaking og spillstart:</ins>
> Dersom ingen andre venter, plasseres spilleren i en venteliste som administreres av GameService.

🠋
> Hvis en annen spiller allerede venter, kobles spillerne sammen i en ny GameSession.
GameSession inneholder begge spillernes ConnectionId, brettdata og turstatus etc.

🠋
> Begge klienter varsles via StartGameHandler om at spillet kan begynne.

🠋
###  <ins>Handlinger under spillet:</ins>
> Når en spiller utfører et skudd (Battleship) eller gjør et trekk (Connect4), sendes dette til huben.

🠋
> Server håndterer skuddet/trekket, oppdaterer tilstanden og sender tilbake resultatet via egne handler-funksjoner

🠋
> Begge klienters UI oppdateres i sanntid med det nyeste skuddet/trekket.

🠋
### <ins>Avslutning:</ins>
> Når spillet er ferdig (enten ved seier eller at en spiller kobler fra),\
> varsles begge klienter via relevante SignalR-handlere (GameOverHandler, OpponentDisconnectedHandler).\
Resultatet logges, og leaderboardet kan oppdateres via REST-kall til backend.

## API oversikt:
### API’ets struktur lar deg logge inn med google, eller en lokal bruker
- Man kan registrere seg via google eller lokalt
- Som innlogget bruker kan man endre på egen data
- Ved Logout så revokes refresh tokenet som er lagret i databasen.
  - Oppretting av JWT Access Token og JWT Refresh Token, via refresh token får man ny access token.
  - Dersom man er logget inn og nærmer seg slutten av tiden for Refresh Token så får man en ny en.
  - Validering av Access og Refresh Token.
  LeaderBoard:
- Her opprettes eller legges det til score til innlogget bruker i et leaderboard.
  - Dette er en paginert score side, her vil man også få ut egen score i tillegg dersom man er logget inn.
  Custom AuthenticationStateProvider i Blazor:
  - Her blir refresh token kryptert via AES, og denne lagres i localstorage.
  - Refresh token blir dekryptert og sendt for å få ny refresh token dersom\
    det er mindre enn 1 time igjen av  tokenet’s varighet, og en access token.
***
## Eksempel på Dataflyt til innlogging
> En person logger inn

🠋
> Møter JWT Middleware og ExceptionHandler

🠋
> Møter controlleren, derfra så sender den ned til service laget med brukernavnet og passordet fra inlogget bruker

🠋
> Derfra så finner sender den brukernavnet ned til repositoriet som derifra finner og returnerer Member som er logget inn utifra brukernavnet.\
Service laget sjekker om member er null, om hashedpassword er null/empty så verifier passordet mot hashedpassord i databasen via BCrypt.\
Dersom alt går gjennom så returnerer den en mappet member til controlleren.

🠋
> I controlleren så ber den Servicelaget om å opprette en refreshtoken og access token.\
Controlleren tar deretter og sender refreshtokenen sammen med memberId til innlogget bruker til service\
som lager tilleggs info for refreshtokenen (created, expires og revoked) så videre til repoet som lagrer refreshtokenen\med all infoen ned i databasen.

🠋
> Etter alt dette så returnerer controlleren access tokenen og refresh tokenen sånn at blazor kan ta dette og lagre det.

🠋
> Dersom responsen er success ved endpoint callet så kaller blazor componenten på CustomAuthenticationStateProvider som lagrer all infoen.\
Denne Encrypterer refreshtokenen via AES og en egen JWT key (atm lagret i appsettings da det er under development)\
Derfra så lagrer den den encrypterte refreshtokenen inn i local storage.

🠋
> Videre så setter den access tokenen i Header for fremtidige kall, Videre så kaller den på 2 controllere for å hente ut brukernavnet og memberid.\
Da SignalR fungerer på den måten som den gjør og HttpContext ikke er tilgjengelig i blazor components\
så blir brukernavnet og memberid lagret i båte context.Items og Claims etter validert accesstoken i middlewaren.

🠋
> Controllerene henter ut disse og returnerer de som en string. Derfra så lagres disse inn i en egen Claimstype.\
Den setter også tidspunktet når dette skjer inn i et eget field for videre bruk senere.\
Samtidig så setter den også brukernavnet for AuthStateService som Navmenu bruker.

🠋
> Til slutt når dette er i orden så sendes man til hjemmesiden.
***
## Resultater og evaluering
Arkitekturen på de fleste modulene har komponenter med tilhørende interfaces, samt flere komponenter for separasjon av ansvar.\
Dette med hensikt for fremtidig skalering, bedre testbarhet og funksjonalitet.\
Noen komponenter viste tekniske utfordringer ved denne tilnærmingen og er derfor bygget for funksjonalitet i første utkast,\
med viten om at dette kan påvirke ytelse.\
Noen av komponentene har også viket fra øvrig standard grunnet tidsbesparelse.

Det er blitt gjort to integrasjonstester som omhandler bruker-registrering, denne sjekker på utfall av gyldig og ugyldig data som blir sendt.\
To unit tester er blitt gjort for en spillkomponent til Connect4,\
den ene sjekker om utfallet av verdier basert på tur-basert brikkeplassering er korrekt og konsistent,\
mens andre sjekker for riktig verdiretur ved en plassert brikke.\
To unit tester er gjort for CheckGameOver når enten alle spillerens/ eller motstanderens skip er sunket.\
Her settes det opp en testpage med gjeldende funksjon og hubconnection mockes.

Prosjektets arkitektur følger en monolitisk modell, hvor både REST API, SignalR-hub, og frontend (UI/klient) er integrert i én og samme Blazor Web App.\
Databasen kjøres separat i en Docker-instans, men er koblet til applikasjonen via en standard tilkoblingsstreng.
***
### Videre prosjektforbedring vil innebære:
- Logging gjennom flere deler/komponenter i prosjektet.
- Øke robusthet med exceptionhandling rundt de Hub-relaterte delene av prosjektet.
- Undersøke videre connection-baserte events og oppdatering av states ift Blazor sin integrerte signalR.
- Ta bedre høyde for eventuelle/potensielle overbelastnings trusler.
- Integrere mindre deler oftere, contra større deler sjeldnere.
- Kunne brukt ASP.NET roles og user
- Forbedre prosjektets mappestruktur
***
### Erfaringer fra utviklingsprosessen:
- Å tilegne seg ny kunnskap kan ta lengre tid en ønsket/forventet.
- Man kan fort bli ivrig, revet med og vike fra opprinnelig plan.
- Over/under-estimasjon av tidsbruk på forskjellige deler.
- Utfordringer ved å lete seg frem til god dokumentasjon.
- Benytte google sitt innloggings-system
- Bruke JWT tokens for autorisasjon og autentisering
- SignalR tilkobling
- Integrere og bruke informasjon fra API
- Hvordan integrere spillgrafikk og funksjoner rett i blazor
- Hvordan håndtere sanntidskommunikasjon via SignalR, samt få de forskjellige tilkoblingene til å leve parallelt
- Hvordan skape og håndtere interaktivt design/Ui utenfor "blazor - standard"(Lyd, animasjon etc)
- Hvordan kombinere JavaScript for bruk av lyd sammen med Blazor, der blazofunksjoner ikke helt strekker til.
- Hvordan CSS kan brukes til å få animasjon og interaktivitet i Ui
- HOW
  - TO
    - CENTER A DIV?!
***
## Konklusjon og videre arbeid
Under prosjektets utvikling har det forekommet flere utfordringer,\
disse omhandler alt fra å sette sammen alle modulene slik at det ferdige produktet oppleves sømløst fra start til slutt – til små irritasjonsmomenter\
som hvorfor riktig data ikke blir sendt/vist eller lagret riktig.

Hoved-modulene til prosjektet har i utgangspunktet blitt utviklet hver for seg\
(API, SIgnalR-Hub utforming, spill-regel komponenter med følgende razorpages).\
Noe som har ført til at vi til tider har jobbet litt rundt og ikke med hverandre,\
hvilket igjen da har ført til at det ikke alltid har gjort det like lett å samkjøre kode.

Det har også forekommet nye oppdagelser ved valget av Blazor i sin helhet, og deretter også Server delen.\
Hub modulene(med servicer og alt) som et eksempel, har gått igjennom flere re faktoriseringer for å oppnå ønsket resultat,\
likevel er gjenstår det forbedringspotensialer som nok kunne ført til mer effektiv kode.\
Det ble også forsøkt å lagre JWT i cookie, men da blazor benytter SignalR  websockets sendes ikke nødvendigvis cookies på samme måte som ved vanlige http-forespørsler\
(hvilket vi fikk bekreftet da cookien fungerte som forventet ved bruk av postman).\
Det ble gjort flere forsøk for å oppnå ønsket resultat, uten hell,\
dermed ble løsningen heller en kryptert refresh session token i localstorage, hvilket gav ønsket resultat.
***
#### Vi leverer som gruppe et prosjekt som på tross av mange utfordringer inneholder:
- To fungerende spill
- Et sikkert system for brukere
- Et leaderboard som viser oversikt over spillernes poeng til hvert spill
- En chat som kan brukes for alle
- Et messageboard der brukere kan poste i forum stil

Vi har valgt å sette oss inn i teknologier som er helt nye for oss da dette var nødvendig for prosjektets funksjonalitet.\
Når det kom til JWT/Google, ble dette valget for autentisering og autorisasjon da dette er en moderne tilnærming for bruker-innlogging og sikkerhet.\
SignalR for websockets med en hubløsning ble valget for sanntidskommunikasjon,\
både fordi det er en del av ASP.NET og har fall-back teknologier dersom websockets ikke skulle være tilgjengelige.
***
#### Anbefalinger for videre utvikling og forbedringer vil også inkludere:
- At brukeren kan velge å slette sin bruker
- Et messageboard som bruker en type redis-basert / eller annen from for lagring slik at data blir lagret for en periode
- Benytte servicer typ AWS/Azure for secrets som i dag ligger i appsettings.json
- Tilrettelegge for deployment, benytte cloud-plattformer
- Implementering av mindre manglende grafiske elementer i spillene
- Språk filter for chat og brukernavn
***
### Utviklere
- [Line Bratli Veddegjerde](https://github.com/Ariyelz)
- [Jonas Lærke Hellum](https://github.com/JonasHellum)
- [Brian Henriksen](https://github.com/BrianHenriksen)

### Lisens
[MIT License](./LICENCE)