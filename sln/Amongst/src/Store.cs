using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Amongst
{
    public class Persistence
    {
        public Guid Id;
        public DateTime LastRun;
    }

    public class Store
    {
        public string BinaryPath;
        public Persistence Persistence = new Persistence();
        private readonly string _storeFilePath;

        public Store()
        {
            _storeFilePath = Path.Combine(Directory.GetCurrentDirectory(), "instances", "store.json");
        }

        public void Load()
        {
            if (!File.Exists(_storeFilePath)) return;

            using (var file = File.OpenText(_storeFilePath)) {
                var store = (Store) JsonSerializer.Create().Deserialize(file, typeof(Store));

                Persistence = store.Persistence;
                BinaryPath = store.BinaryPath;
            }
        }

        public void Save()
        {
            using (var file = File.CreateText(_storeFilePath)) {
                JsonSerializer.Create(new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }).Serialize(file, this);
            }
        }
    }
}