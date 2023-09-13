using System.Text.Json;
using Serilog;
using SlopCrew.Common;
using SlopCrew.Common.Encounters;
using SlopCrew.Common.Network.Clientbound;
using SlopCrew.Server.Race;

namespace SlopCrew.Server;

public class StatefulEncounterManager {
    public List<StatefulEncounter> Encounters = new();
    public Dictionary<int, Dictionary<EncounterType, List<ConnectionState>>> QueuedPlayers = new();
    private int queueTicks;

    public List<RaceConfig> RaceConfigs = new();

    private const int TicksPerQueue = Constants.TicksPerSecond * 20;

    public StatefulEncounterManager() {
        Task.Run(this.DownloadRaceConfigs).Wait();
    }

    private async Task DownloadRaceConfigs() {
        Log.Information("Downloading race configs from GitHub...");
        var gh = new GitHubDownloader();
        var races = await gh.DownloadFilesFromDirectory("SlopCrew", "race-config");

        foreach (var file in races.Values) {
            var raceConfig = JsonSerializer.Deserialize<RaceConfig>(file);
            if (raceConfig != null) this.RaceConfigs.Add(raceConfig);
        }

        Log.Information("Loaded {Count} race configs", this.RaceConfigs.Count);
    }

    public void Update() {
        // Regular update
        foreach (var encounter in this.Encounters) {
            encounter.Update();
        }

        // Check to remove empty races
        this.Encounters.Where(e => e.IsEmpty())
            .ToList()
            .ForEach(e => { e.State = EncounterState.Finished; });

        var finishedEncounters = this.Encounters
            .Where(e => e.State == EncounterState.Finished);
        foreach (var encounter in finishedEncounters) {
            this.Encounters.Remove(encounter);
        }

        var queueIsEmpty = this.QueuedPlayers.Count == 0;
        if (!queueIsEmpty) {
            this.queueTicks++;
            if (this.queueTicks >= TicksPerQueue) {
                this.QueuePlayers();
                this.queueTicks = 0;
                this.QueuedPlayers.Clear();
            }
        }
    }

    public void HandleEncounterRequest(ConnectionState conn, EncounterType type) {
        if (conn.Player is null) return;
        var stage = conn.Player.Stage;

        if (!this.QueuedPlayers.ContainsKey(stage)) this.QueuedPlayers[stage] = new();
        if (!this.QueuedPlayers[stage].ContainsKey(type)) this.QueuedPlayers[stage][type] = new();
        if (this.QueuedPlayers[stage][type].Contains(conn)) return;

        this.QueuedPlayers[stage][type].Add(conn);
    }

    private void QueuePlayers() {
        foreach (var (stage, queue) in this.QueuedPlayers) {
            foreach (var (type, players) in queue) {
                var encounter = type switch {
                    EncounterType.RaceEncounter => new RaceStatefulEncounter(stage),
                    _ => throw new ArgumentOutOfRangeException()
                };

                encounter.Players.AddRange(players);
                this.Encounters.Add(encounter);

                Server.Instance.Module.SendToTheConcerned(
                    players.Select(x => x.Player!.ID),
                    new ClientboundEncounterStart {
                        EncounterType = type,
                        EncounterConfigData = new RaceEncounterConfigData {
                            EncounterLength = RaceStatefulEncounter.MaxRaceTime,
                            Guid = encounter.EncounterId,
                            RaceConfig = encounter.ConfigData
                        }
                    }
                );
            }
        }
    }
}
