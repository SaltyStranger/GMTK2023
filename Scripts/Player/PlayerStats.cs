using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    bool firstSetup;

    public void loadFrom(STMPacket packet)
    {
        // Load values
        firstSetup = packet.firstSetup;
        //maxCards = packet.maxCards;

        // Send leftover level effects to the new Level Manager.
        //foreach (CardEffect ce in levelEffects)
        //{
        //    lm.addEffect(ce);
        //}

        // Sync with UI
        // if (pHPEC != null) pHPEC.updateBarRendering(currHealth, maxHealth, currEnergy, maxEnergy);
    }

    public STMPacket packet()
    {
        STMPacket packet = new STMPacket();
        //packet.firstSetup = firstSetup;
        //packet.maxCards = maxCards;
        return packet;
    }
}
