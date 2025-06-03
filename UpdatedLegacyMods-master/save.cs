using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Nautilus.Json;


namespace DockedVehicleStorageAccess
{
    [Serializable]
    public class SaveDataEntry
    {
        public string id;
    }

    [Serializable]
    public class SaveData 
    {
        public List<SaveDataEntry> Entries = new List<SaveDataEntry>();
    }
}