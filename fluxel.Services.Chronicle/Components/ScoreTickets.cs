using System;
using System.Collections.Generic;

namespace fluxel.Services.Chronicle.Components;

public class ScoreTickets
{
    public Dictionary<string, Ticket> Tickets { get; } = new();

    public class Ticket
    {
        public long MapID { get; }
        public DateTimeOffset PredictedCompletion { get; }

        public Ticket(long mapID, DateTimeOffset predictedCompletion)
        {
            MapID = mapID;
            PredictedCompletion = predictedCompletion;
        }
    }
}
