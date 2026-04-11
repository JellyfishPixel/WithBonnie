using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public TimeSaveData time;
    public EconomySaveData economy;
    public CameraSaveData camera;
    public InventorySaveData inventory;
    public List<DeliverySaveData> activeDeliveries;
    public SceneSaveData scene;

}
