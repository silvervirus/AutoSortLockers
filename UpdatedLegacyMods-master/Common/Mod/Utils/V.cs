using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Common.Mod.Utils
{
    public static class Variables
    {
        public static Harmony harmony { get; set; }

        public static ManualLogSource logger { get; set; }

        public static class Paths
        {
            public static string PluginFolder => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            public static string AssetsFolder => Path.Combine(PluginFolder, "Assets");

            public static string RecipeFolder => Path.Combine(PluginFolder, "Recipes");
        }
    }
}
