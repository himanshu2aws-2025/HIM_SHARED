using EdiFileProcessor.Models;
using System;
using System.IO;
using Newtonsoft.Json;

namespace EdiFileProcessor.Helpers
{
    public static class ConfigLoader
    {
        public static EdiConfig Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Config file not found: {path}");

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<EdiConfig>(json);
        }
    }
}
