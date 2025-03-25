using ModestTree;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace MultiplayerChat.Models
{
    internal class QuickChatOptions : IInitializable
    {
        const string QuickChatOptionsPath = "UserData/MultiplayerChat/quickchat.json";

        public IReadOnlyDictionary<string, string[]> Options => _options;
        private Dictionary<string, string[]> _options = new();

        public void Initialize()
        {
            if (!File.Exists(QuickChatOptionsPath)) WriteOutDefault();

            using (var fstream = new FileStream(QuickChatOptionsPath, FileMode.Open))
            {
                using (var reader = new StreamReader(fstream))
                    _options = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(reader.ReadToEnd());
            }
        }

        private void WriteOutDefault()
        {
            var dir = Path.GetDirectoryName(QuickChatOptionsPath);
            if (!Directory.Exists(Path.GetDirectoryName(QuickChatOptionsPath)))
                Directory.CreateDirectory(dir);

            using (var fstream = new FileStream(QuickChatOptionsPath, FileMode.OpenOrCreate))
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "MultiplayerChat.Assets.quickchat.json";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    stream.CopyTo(fstream);
            }
        }
    }
}
