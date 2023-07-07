using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A packet used by SceneTransferManager to send player data across scenes.
public class STMPacket
{
    public int currHealth, maxHealth, currEnergy, maxEnergy, maxCards;

    //public List<CardEffect> activeEffects;

    //public List<CardEffect> levelEffects;

    //public List<Card> cards;

    public bool firstSetup;
}
