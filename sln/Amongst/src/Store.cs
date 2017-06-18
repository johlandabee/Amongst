using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Amongst
{
    public class Persistence
    {
        public Guid Id { get; set; }
        public DateTime LastRun { get; set; }
    }

    //------------------------------------------------------------------------------------------------------------->

    public class Store
    {
        private const string STORE_FILE = "store.json";

        public string BinaryPath { get; set; }
        public Persistence Persistence { get; set; }

        //------------------------------------------------------------------------------------------------------------->

        public Store()
        {
            Persistence = new Persistence();
        }

        public void Load(string instancesPath)
        {
            var storeFilePath = Path.Combine(instancesPath, STORE_FILE);

            if (!File.Exists(storeFilePath)) {
                return;
            }

            using (var file = File.OpenText(storeFilePath)) {
                var store = (Store) JsonSerializer.Create().Deserialize(file, typeof(Store));

                Persistence = store.Persistence;
                BinaryPath = store.BinaryPath;
            }
        }

        public void Save(string instancesPath)
        {
            var storeFilePath = Path.Combine(instancesPath, STORE_FILE);

            using (var file = File.CreateText(storeFilePath)) {
                JsonSerializer.Create(new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }).Serialize(file, this);
            }
        }
    }
}