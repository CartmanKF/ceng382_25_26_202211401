using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RazorPagesProject.Models;

namespace RazorPagesProject.Utils
{
    public class JsonExportUtility
    {
        private static JsonExportUtility _instance;
        public static JsonExportUtility Instance => _instance ??= new JsonExportUtility();

        private JsonExportUtility() { }

        public void ExportToJson<T>(List<T> data, string filePath)
        {
            var jsonString = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(filePath, jsonString);
        }

        public string ExportToJsonString<T>(List<T> data)
        {
            return JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }

        public List<T> ImportFromJson<T>(string filePath)
        {
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(jsonString);
        }
    }
} 