

using Common.Mod.Utils;
using System.IO;

namespace Common.Utils
{
    public static class JsonUtils
    {
        public static string GetJsonRecipe(string filename) => Path.Combine(Variables.Paths.RecipeFolder, filename + ".json");
    }
}