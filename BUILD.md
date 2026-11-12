# Build

Run `dotnet build -c Release`. Set the `ElinGamePath` MSBuild property if Elin is installed elsewhere. Run `dotnet run --project tests/MailPranksChecks.csproj -c Release` for offline policy checks.

The build does not install anything. The package in `dist/MailPranks` needs its DLL, manifest, and `assets/ledger.png` together.

Release version: 0.2.3. Package compatibility: 0.23.355.

Self-delivery gathers living, unattached party members already in the destination onto nearby visible free tiles after player placement. Native host synchronization carries mounts/symbiotes. New deliveries use ThingGen.CreateCardboardBox, the native empty mail container, with a scoped TraitContainer.Open hook. Legacy parcel opening remains supported.

Self-delivery offers towns, native online random moongates, and native saved-map destinations. Moongate travel awaits UseMoongate/LoadMap resolution, records the actual destination, then waits for the destination map and progress layer to finish before choosing a resident/landing tile. Failure or interrupted unresolved requests cancel without spawning or changing hostility. Moongate arrivals make all nonparty residents hostile; ordinary town arrivals retain their isolated duel. UniTask is referenced from the game and is not bundled.

The ledger uses native book_roster with a private marker, localized name and custom inventory sprite. Pending creatures use native SlaverData on the player card; history uses versioned JSON on that same card. No separate global save file is used. Missing parcels are retained in history and never automatically duplicated.

Each trader has a separate save-integrated SlaverData for pranks, using native species generation and a four-day restock timer. Native localized nextRestock formatting shows the remaining time. Normal slave/pet stock is never consumed or refreshed by this list.

Validation: offline policy/localization checks and native startup/serializer checks pass. Moongate gameplay was user-tested; the companion gathering and installed-container corrections still need a complete arrival playthrough.
