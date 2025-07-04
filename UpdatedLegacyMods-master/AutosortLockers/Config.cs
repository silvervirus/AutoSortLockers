﻿using Nautilus.Options;
using BepInEx.Configuration;
using Nautilus.Handlers;
using System.Collections.Generic;
using Nautilus.Utility;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;
using ConfigFile = BepInEx.Configuration.ConfigFile;

namespace AutosortLockers
{

    public class AutosortConfig
    {

        public static ConfigEntry<bool> EasyBuild;
        public static ConfigEntry<bool> ShowLogs;
        public static ConfigEntry<bool> UnlockedAtStart;
        public static ConfigEntry<bool> TransferToSorter;
        public static ConfigEntry<bool> TransferOtherLockers;
        public static ConfigEntry<float> SortInterval;
       
        public static ConfigEntry<bool> ShowAllItems;
        public static ConfigEntry<float> updatePresentsIntervals;
        public static ConfigEntry<float> RangeLimit;
        public static ConfigEntry<int> AutosorterWidth;
        public static ConfigEntry<int> AutosorterHeight;

        public static ConfigEntry<int> ReceptacleWidth;
        public static ConfigEntry<int> ReceptacleHeight;

        public static ConfigEntry<int> StandingReceptacleWidth;
        public static ConfigEntry<int> StandingReceptacleHeight;

        public static void LoadConfig(ConfigFile config)
        {
            // Recipe config regsitration

            EasyBuild = config.Bind("Autosorter Recipe",
                "Easy Build Recipe",
                false,
                "Toggle whether to use the easy recipe or not"
                );
            ShowLogs = config.Bind("ShowLog",
               "Show Debug Logs",
               false,
               "Toggle whether to show the debug logs"
               );
            UnlockedAtStart = config.Bind("LasyPlayer",
                "All Lockers Unlocked At Start",
                false,
                "Toggle whether to have All The Lockers Unloacked at start or to use A techtype to unlock them."
                );
            TransferOtherLockers = config.Bind("TOLS",
               "Allow Transfer From Other lockers",
               false,
               "Toggle Allows Transfer from default lockers and Docked Vehicle Storage Locker"
               );
            TransferToSorter = config.Bind("TOS",
             "Allow Transfer To Sorter",
             false,
             "Toggle Allows Transfer from From Unloader To Sorter"
             );

            SortInterval = config.Bind("Autosorter Recipe",
                "Sort Interval",
                1.0f,
                "How long to wait in-between sorting each item"
                );


            ShowAllItems = config.Bind("Autosorter Recipe",
                "Show All Items",
                false,
                "Whether to show all items or not"
                );
            updatePresentsIntervals = config.Bind("Update Intervals Config",
               "Wait time for Update Presents",
               10f,
               "The time set to wait until update presents of docked vehicle"
               );
            RangeLimit = config.Bind("Range Limit Config",
               "The Limit to the Range for the Unsorter",
               50f,
               "change the number to increase or decrease the range limit"
               );

            // Autosorter config options

            AutosorterWidth = config.Bind("Autosorter Config",
                "Autosorter width size",
                5,
                "The width of the inventory for the autosorter"
                );

            AutosorterHeight = config.Bind("Autosorter Config",
                "Autosorter height size",
                6,
                "The height of the inventory for the autosorter"
                );

            // Autosort receptacle config options

            ReceptacleWidth = config.Bind("Receptacle Config",
                "Receptacle width size",
                6,
                "The width of the inventory for the autosort receptacle"
                );
            ReceptacleHeight = config.Bind("Receptacle Config",
                "Receptacle height size",
                8,
                "The height of the inventory for the autosort receptacle"
                );
            // Autosort standing receptacle config options

            StandingReceptacleWidth = config.Bind("Standing Receptacle Config",
                "Standing receptacle width size",
                6,
                "The width of the inventory for the standing autosort receptacle"
                );
            StandingReceptacleHeight = config.Bind("Standing Receptacle Config",
                "Standing receptacle height size",
                8,
                "The height of the inventory for the standing autosort receptacle"
                );
        
        
            OptionsPanelHandler.RegisterModOptions(new AutoSortModOptions());
        }

        public static void WriteDefaultFilters()
        {
            if (FileUtils.FileExists(IOUtilities.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "filters.json"))) return;

            List<FilterEntry> filterFileData = new List<FilterEntry>();
            foreach (KeyValuePair<string, TechType[]> category in AutosorterCategoryData.defaultCategories)
            {
                List<string> value = new List<string>();
                foreach (var v in category.Value)
                {
                    value.Add(v.ToString());
                }

                filterFileData.Add(new FilterEntry(category.Key, value));
            }

            var json = JsonConvert.SerializeObject(filterFileData, Formatting.Indented);
            File.WriteAllText(IOUtilities.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets", "filters.json"), json);
        }
        public class AutoSortModOptions : ModOptions
        {
            public AutoSortModOptions() : base("Autosort Lockers")
            {
                AddItem(ShowLogs.ToModToggleOption());
                AddItem(EasyBuild.ToModToggleOption());
                AddItem(UnlockedAtStart.ToModToggleOption());
                AddItem(TransferToSorter.ToModToggleOption());
                AddItem(TransferOtherLockers.ToModToggleOption());
                AddItem(SortInterval.ToModSliderOption(0.1f, 5.0f, 0.1f, "{0:F1}x"));
                AddItem(RangeLimit.ToModSliderOption(500f, 1000f, 50f, "{0:F1}x"));
                AddItem(ShowAllItems.ToModToggleOption());
                AddItem(updatePresentsIntervals.ToModSliderOption(10f, 30f, 10f));

                AddItem(AutosorterWidth.ToModSliderOption(1, 40, 1));
                AddItem(AutosorterHeight.ToModSliderOption(1, 40, 1));

                AddItem(ReceptacleWidth.ToModSliderOption(1, 40, 1));
                AddItem(ReceptacleHeight.ToModSliderOption(1, 40, 1));

                AddItem(StandingReceptacleWidth.ToModSliderOption(1, 40, 1));
                AddItem(StandingReceptacleHeight.ToModSliderOption(1, 40, 1));
            }
        }
    }
}