namespace SessionDashboard;

public sealed record StoryChoice(string Text, string Next);
public sealed record StoryScene(string Chapter, string Title, string Text, StoryChoice[] Choices)
{
    public bool IsEnding => Choices.Length == 0;
}

public static class Story
{
    public static readonly IReadOnlyDictionary<string, StoryScene> Scenes = new Dictionary<string, StoryScene>
    {
        ["arrival"] = new("01 / THE SIGNAL", "The Last Greenhouse",
            "At 11:47, every station clock stops. Beyond the glass roof, the city is dark except for a greenhouse on the hill. Your pocket radio crackles: ‘We have one battery left. Tell us what to save.’\n\nA stairway leads to the signal tower. The station archive glows beneath the concourse. On platform nine, a train waits with its doors open.",
            [new("Climb the tower. Find out who is calling.", "roof"), new("Search the archive. Learn what happened.", "archive"), new("Board the train. Someone may need help.", "platform")]),
        ["roof"] = new("02 / ABOVE THE CITY", "A voice in the rain",
            "The caller is Iona, the greenhouse keeper. The storm has severed the irrigation network. The tower battery could power a warning beacon or restart the greenhouse pumps, but not both.\n\nOn the maintenance desk you find a hand-drawn weather chart and a sealed relay box. Thunder is getting closer.",
            [new("Study the weather chart before committing power.", "weather"), new("Open the relay. Try to reconnect the city.", "relay")]),
        ["weather"] = new("03 / A NARROW WINDOW", "Seven minutes of clear sky",
            "The chart shows a brief gap in the storm. A warning now could guide the supply boats into shelter. But the greenhouse's oldest seedlings will dry out before the boats arrive.\n\nIona doesn't choose for you. ‘Food can be replanted,’ she says. ‘So can hope. Neither grows on its own.’",
            [new("Light the beacon. Bring the supply boats home.", "beacon"), new("Send the battery to the pumps. Save the seedlings.", "garden")]),
        ["relay"] = new("03 / OPEN CIRCUIT", "A hundred small signals",
            "The central relay is burned through. There is no repair you can make alone. But the low-power neighborhood receivers still work. You could ask people to make a chain of lamps, or spend the entire battery broadcasting the evacuation route.\n\nThe radio fills with voices waiting for instructions.",
            [new("Ask each neighborhood to relay a light to the next.", "chorus"), new("Broadcast a clear route to the waiting train.", "departure")]),
        ["archive"] = new("02 / UNDER THE CONCOURSE", "The city's memory",
            "In the archive, you find the outage was deliberate. The station diverted its power to keep a century of seed records from being erased. An old service map shows a water channel beneath the greenhouse.\n\nThe records are still transferring. Taking the archive battery will stop the copy.",
            [new("Leave the records running and follow the water map.", "map"), new("Read the unfinished recording beside the console.", "echo")]),
        ["map"] = new("03 / BELOW THE ROOTS", "The forgotten channel",
            "The service tunnel reaches a rusted floodgate. You can release the reservoir into the greenhouse, but the water will flood the archive below. Or you can close the tunnel and carry the map back, leaving the thirsty plants for another day.\n\nA chalk mark on the wall reads: ‘A map is a promise that someone else can follow.’",
            [new("Open the floodgate. Save living seeds, lose the archive.", "garden"), new("Seal the gate. Preserve the records for rebuilding.", "memory")]),
        ["echo"] = new("03 / A MESSAGE LEFT BEHIND", "No perfect rescue",
            "The recording is from the city's first gardener: ‘These records belong to everyone. If you must choose between preserving them and sharing them, share them.’\n\nThe station transmitter can send the unfinished archive to neighborhood radios, or you can keep the only complete copy safe here. A broadcast will drain the battery.",
            [new("Broadcast the records. Let the city carry its own memory.", "chorus"), new("Finish the copy and protect the archive.", "memory")]),
        ["platform"] = new("02 / PLATFORM NINE", "The last departure",
            "The train is full of people carrying empty pots. They came to help at the greenhouse. The driver can take everyone out of the storm, but the station battery is only strong enough for one trip.\n\nA porter has a cart of tools. A child asks whether a train can run without an engine.",
            [new("Talk to the passengers. Ask what they can do together.", "passengers"), new("Inspect the engine with the driver.", "engine")]),
        ["passengers"] = new("03 / MANY HANDS", "A different kind of power",
            "The passengers include electricians, gardeners, and one very determined baker. They can form a bucket line from the station cistern to the hill, if you stay to organize it.\n\nOr you can send them away on the train, taking the last battery to a safer place where a new greenhouse could be built.",
            [new("Stay. Turn the empty pots into a bucket line.", "chorus"), new("Leave together. Carry the seeds to a new home.", "departure")]),
        ["engine"] = new("03 / THE STILL ENGINE", "Permission to stop",
            "The engine is sound. The driver is exhausted. ‘Everyone keeps asking for one more trip,’ she says. A small maintenance shelter has blankets and enough food for tonight.\n\nYou can take over the controls and use the battery to leave, or shut the station down and let everyone rest while the storm passes.",
            [new("Take the controls. Make the final trip.", "departure"), new("Open the shelter. Tomorrow can have tomorrow's problems.", "quiet")]),
        ["beacon"] = new("ENDING / THE LIGHTKEEPER", "A light across the water",
            "You spend the battery on the beacon. One by one, the supply boats answer. By dawn, medicine and food reach the station. The oldest seedlings are gone, and Iona sits beside their empty trays.\n\nThen a boat captain places a packet of seeds in her hand. You couldn't save everything. You made it possible to begin again.", []),
        ["garden"] = new("ENDING / THE GARDENER", "Something living",
            "Water reaches the roots just before dawn. The greenhouse fills with the smell of wet soil. Some of the city's history, and some of its machinery, will never come back.\n\nIona gives you the first new leaf to appear. Outside, people begin clearing the paths by hand. What you saved will need tending. That's what makes it alive.", []),
        ["memory"] = new("ENDING / THE ARCHIVIST", "Instructions for a future",
            "The final record settles into place. You seal the archive as the greenhouse lights go out. It is a hard, quiet choice, and you do not pretend otherwise.\n\nWeeks later, the station opens as a school. A child follows an old planting diagram and grows the first green shoot of spring. Memory has become an instruction, and an instruction has become a beginning.", []),
        ["departure"] = new("ENDING / THE TRAVELER", "Beyond the last station",
            "The train crosses the bridge with every seat taken. The city's last light disappears behind you. In the next valley, the passengers gather around their pots, tools, and packets of seeds.\n\nNo one calls this a rescue of the old city. You saved its people, and they will decide what to build next. The driver finally sleeps.", []),
        ["chorus"] = new("ENDING / THE NEIGHBOR", "No single point of light",
            "Your call becomes a conversation. Lamps appear in windows. Hands pass buckets. Names and planting instructions travel from radio to radio. The battery dies, but the work continues.\n\nBy morning, no one can point to the person who saved the city. There are too many. You put down what you were carrying. Someone else picks it up.", []),
        ["quiet"] = new("ENDING / THE CARETAKER", "Enough for tonight",
            "You turn off the station and open the shelter. The greenhouse must wait. So must the train. Under borrowed blankets, the passengers share the last warm tea.\n\nAt dawn the storm passes on its own. There is damage to repair, and no grand victory to announce. But everyone has slept. For the first time all night, the next step looks possible.", [])
    };

    public static StoryScene Current(AppState state) => Scenes.TryGetValue(state.StoryNode ?? "", out var scene) ? scene : Scenes["arrival"];
    public static void Choose(AppState state, int choice)
    {
        var scene = Current(state);
        if (choice < 0 || choice >= scene.Choices.Length) throw new ArgumentOutOfRangeException(nameof(choice));
        var selected = scene.Choices[choice];
        state.StoryHistory.Add(selected.Text);
        state.StoryNode = selected.Next;
        if (Current(state).IsEnding && !state.StoryEndings.Contains(selected.Next)) state.StoryEndings.Add(selected.Next);
    }
    public static void Restart(AppState state) { state.StoryNode = "arrival"; state.StoryHistory.Clear(); }
}
