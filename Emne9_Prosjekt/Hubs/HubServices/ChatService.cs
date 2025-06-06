﻿using System.Collections.Concurrent;
using Emne9_Prosjekt.Hubs.HubServices.Interfaces;

namespace Emne9_Prosjekt.Hubs.HubServices;

public class ChatService : IChatService
{
    private readonly ILogger<ChatService> _logger;

    // En tråd-sikker kø som lagrer brukere som venter på en chat-partner
    private static readonly ConcurrentQueue<string> WaitingQueue = new();
    // En tråd-sikker dictionary som knytter hver tilkobling til en gruppe
    private static readonly ConcurrentDictionary<string, string> UserGroups = new();
   private static readonly ConcurrentDictionary<string, string> UserPartners = new(); // Kobling mellom bruker og partner
   
   public ChatService(ILogger<ChatService> logger)
   {
       _logger = logger;
   }

    /// <summary>
    /// Tildeler en bruker til en chat-gruppe.
    /// Hvis det finnes en annen bruker som venter, pares de sammen i en ny gruppe.
    /// Hvis ikke, legges brukeren i køen.
    /// </summary>
    /// <param name="connectionId">Tilkoblings-ID for brukeren</param>
    /// <returns>Gruppe-navnet og partnerens tilkoblings-ID hvis en match finnes, ellers null</returns>
    public (string? groupName, string? partnerConnectionId) AssignUserToGroup(string connectionId)
    {
        lock (WaitingQueue) // Sikrer at køoperasjoner er tråd-sikre
        {
            if (WaitingQueue.TryDequeue(out var partnerConnectionId)) // Sjekker om noen venter i køen
            {
                var groupName = $"chat_{Guid.NewGuid()}"; //Lager et unikt gruppenavn
                
                // Lagrer begge brukerne i gruppen
                UserGroups[connectionId] = groupName;
                UserGroups[partnerConnectionId] = groupName;
                
                UserPartners[connectionId] = partnerConnectionId;
                UserPartners[partnerConnectionId] = connectionId;
                _logger.LogInformation($"User {connectionId} matched with {partnerConnectionId} in group {groupName}");

                return (groupName, partnerConnectionId);  // Returnerer gruppenavn og partnerens ID
            }
            // Ingen partner tilgjengelig, brukeren legges i ventekøen
            WaitingQueue.Enqueue(connectionId);
            return (null, null);
        }
    }

    /// <summary>
    /// Fjerner en bruker fra chat-gruppen ved frakobling.
    /// </summary>
    /// <param name="connectionId">Tilkoblings-ID for brukeren</param>
    /// <param name="groupName">Ut-param som returnerer gruppen brukeren var i</param>
    /// <returns>True hvis brukeren ble funnet og fjernet, ellers false</returns>
    public bool TryRemoveUser(string connectionId, out string? groupName)
    {
        if (UserGroups.TryRemove(connectionId, out groupName))
        {
            // Finn partneren
            if (UserPartners.TryGetValue(connectionId, out var partnerId))
            {
                // Fjern partnerens kobling også
                UserPartners.TryRemove(partnerId, out _);
                UserGroups.TryRemove(partnerId, out _);
            }

            // Fjern brukeren fra partnerlisten
            UserPartners.TryRemove(connectionId, out _);
            _logger.LogInformation($"User {connectionId} disconnected from {groupName}");

            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Henter hvilken gruppe en bruker tilhører.
    /// </summary>
    /// <param name="connectionId">Tilkoblings-ID</param>
    /// <returns>Gruppe-navnet eller null hvis brukeren ikke er i en gruppe</returns>
    public string? GetUserGroup(string connectionId)
    {
        _logger.LogInformation($"Getting group for {connectionId}");
        UserGroups.TryGetValue(connectionId, out var groupName);
        return groupName;
    }

    //Henter brukerens chat-partner
    public string? GetPartner(string connectionId)
    {
        _logger.LogInformation($"Getting partner for {connectionId}");
        UserPartners.TryGetValue(connectionId, out var partnerConnectionId);
        return partnerConnectionId;
    }
    
    //Sjekker om en bruker har reconnectet
    public bool IsUserReconnected(string connectionId)
    {
        _logger.LogInformation($"Checking if user {connectionId} is reconnected");
        return UserGroups.ContainsKey(connectionId);
    }
    
}